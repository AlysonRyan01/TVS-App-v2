import { Component, inject, model, signal, viewChild } from "@angular/core";
import { HttpErrorResponse } from "@angular/common/http";
import { MatDialog } from "@angular/material/dialog";
import { MatButtonModule } from "@angular/material/button";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { finalize } from "rxjs";
import { SnackbarService } from "../../../shared/snackbar/snackbar.service";
import { maskPhone } from "../../../shared/masks/masks";
import { CustomersService } from "../customers.service";
import { CustomerSearchComponent } from "../customer-search/customer-search.component";
import { CustomerDialogComponent } from "../customer-dialog/customer-dialog.component";
import { CustomerFormComponent } from "../customer-form/customer-form.component";
import { Customer } from "../models/customer.model";
import { formatDocument } from "../models/document";

const DIALOG_OPTIONS = { width: '44rem', maxWidth: '95vw', autoFocus: 'first-tabbable' as const };

/**
 * Coluna "Cliente" das telas Adicionar OS e Vender: mostra o cliente escolhido ou a busca com o
 * formulário "Criar cliente" embaixo. O cliente escolhido sai pelo `[(customer)]`.
 */
@Component({
    selector: 'app-customer-picker',
    templateUrl: './customer-picker.component.html',
    host: { style: 'display: block' },
    imports: [MatButtonModule, MatProgressSpinnerModule, CustomerSearchComponent, CustomerFormComponent],
})
export class CustomerPickerComponent {
    private _service = inject(CustomersService);
    private _dialog = inject(MatDialog);
    private _snackbar = inject(SnackbarService);

    private _search = viewChild(CustomerSearchComponent);
    private _customerForm = viewChild(CustomerFormComponent);

    readonly customer = model<Customer | null>(null);

    protected isSavingCustomer = signal(false);
    protected readonly formatDocument = formatDocument;
    protected readonly maskPhone = maskPhone;

    /** Volta ao início: sem cliente e com a busca vazia. */
    reset() {
        this.customer.set(null);
        this._search()?.clear();
    }

    /** Cliente achado na busca: confirmar ou atualizar no modal antes de usar. */
    protected openExisting(customer: Customer) {
        this._dialog.open(CustomerDialogComponent, { ...DIALOG_OPTIONS, data: { customer } })
            .afterClosed()
            .subscribe((confirmed?: Customer) => {
                if (confirmed) {
                    this.customer.set(confirmed);
                }
            });
    }

    /** Busca sem resultado: o texto digitado vira o nome no cadastro. */
    protected useSearchTermAsName(name: string) {
        this._customerForm()?.setName(name);
    }

    /** Cadastra o cliente do formulário e já o usa. */
    protected createCustomer() {
        const customerForm = this._customerForm();
        if (!customerForm || this.isSavingCustomer()) {
            return;
        }

        if (customerForm.form.invalid) {
            customerForm.form.markAllAsTouched();
            return;
        }

        this.isSavingCustomer.set(true);
        this._service.createCustomer(customerForm.value())
            .pipe(finalize(() => this.isSavingCustomer.set(false)))
            .subscribe({
                next: result => {
                    if (result.data) {
                        this.customer.set(result.data);
                    }
                    this._snackbar.open('Cliente cadastrado.');
                },
                error: (error: HttpErrorResponse) => {
                    const message = error.error?.message ??
                        (typeof error.error === 'string' ? error.error : 'Não foi possível cadastrar o cliente. Tente de novo.');
                    this._snackbar.open(message);
                },
            });
    }

    /** Desfaz só a escolha do cliente; o formulário ao lado continua preenchido. */
    protected changeCustomer() {
        this.customer.set(null);
    }

    protected addressSummary(customer: Customer): string {
        const street = [customer.street, customer.number].filter(Boolean).join(', ');
        return [street, customer.neighborhood, customer.city].filter(Boolean).join(' – ');
    }
}
