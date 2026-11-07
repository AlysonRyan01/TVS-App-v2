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
import { CustomerSearchItem } from "../models/customer-search-item.model";
import { formatDocument } from "../models/document";

const DIALOG_OPTIONS = { width: '44rem', maxWidth: '95vw', autoFocus: 'first-tabbable' as const };

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
    protected isOpeningCustomer = signal(false);
    protected readonly formatDocument = formatDocument;
    protected readonly maskPhone = maskPhone;

    reset() {
        this.customer.set(null);
        this._search()?.clear();
    }

    /** A busca traz só id/nome/telefone; a ficha completa vem da API antes de abrir o diálogo. */
    protected openFromSearch(item: CustomerSearchItem) {
        if (this.isOpeningCustomer()) {
            return;
        }

        this.isOpeningCustomer.set(true);
        this._service.getCustomer(item.id)
            .pipe(finalize(() => this.isOpeningCustomer.set(false)))
            .subscribe({
                next: result => {
                    if (result.data) {
                        this.openExisting(result.data);
                    }
                },
                error: () => this._snackbar.error('Não foi possível abrir o cliente. Tente de novo.'),
            });
    }

    protected openExisting(customer: Customer) {
        this._dialog.open(CustomerDialogComponent, { ...DIALOG_OPTIONS, data: { customer } })
            .afterClosed()
            .subscribe((confirmed?: Customer) => {
                if (confirmed) {
                    this.customer.set(confirmed);
                }
            });
    }

    protected useSearchTermAsName(name: string) {
        this._customerForm()?.setName(name);
    }

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
                    this._snackbar.success('Cliente cadastrado.');
                },
                error: (error: HttpErrorResponse) => {
                    const message = error.error?.message ??
                        (typeof error.error === 'string' ? error.error : 'Não foi possível cadastrar o cliente. Tente de novo.');
                    this._snackbar.error(message);
                },
            });
    }

    protected changeCustomer() {
        this.customer.set(null);
    }

    protected addressSummary(customer: Customer): string {
        const street = [customer.street, customer.number].filter(Boolean).join(', ');
        return [street, customer.neighborhood, customer.city].filter(Boolean).join(' – ');
    }
}
