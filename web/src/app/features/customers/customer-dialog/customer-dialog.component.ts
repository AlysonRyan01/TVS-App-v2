import { Component, inject, signal, viewChild } from "@angular/core";
import { HttpErrorResponse } from "@angular/common/http";
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog";
import { MatButtonModule } from "@angular/material/button";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { finalize } from "rxjs";
import { SnackbarService } from "../../../shared/snackbar/snackbar.service";
import { CustomersService } from "../customers.service";
import { Customer } from "../models/customer.model";
import { CustomerFormComponent } from "../customer-form/customer-form.component";

/**
 * Cliente achado na busca.
 * - `confirm` (padrão, telas Adicionar OS/Vender): confirmar como está ou salvar alterações e usar.
 * - `edit` (tela Clientes): só editar; fechar sem alterações não devolve cliente.
 */
export interface CustomerDialogData {
    customer: Customer;
    mode?: 'confirm' | 'edit';
}

@Component({
    selector: 'app-customer-dialog',
    templateUrl: './customer-dialog.component.html',
    styleUrl: './customer-dialog.component.scss',
    imports: [MatDialogModule, MatButtonModule, MatProgressSpinnerModule, CustomerFormComponent],
})
export class CustomerDialogComponent {
    private _dialogRef = inject(MatDialogRef<CustomerDialogComponent, Customer>);
    private _service = inject(CustomersService);
    private _snackbar = inject(SnackbarService);

    protected data = inject<CustomerDialogData>(MAT_DIALOG_DATA);
    protected isSaving = signal(false);
    protected customerForm = viewChild.required(CustomerFormComponent);
    protected readonly isEdit = this.data.mode === 'edit';

    /** Usa o cliente como está, sem salvar. */
    private confirm() {
        this._dialogRef.close(this.isEdit ? undefined : this.data.customer);
    }

    /** Botão Confirmar: sem alterações, só usa o cliente; com alterações, salva e usa o cliente atualizado. */
    protected save() {
        const form = this.customerForm().form;
        if (form.invalid || this.isSaving()) {
            form.markAllAsTouched();
            return;
        }

        // Sem alterações: usa o cliente como está, sem chamar a API.
        if (!form.dirty) {
            this.confirm();
            return;
        }

        this.isSaving.set(true);
        this._dialogRef.disableClose = true;

        this._service.updateCustomer(this.data.customer.id, this.customerForm().value())
            .pipe(finalize(() => {
                this.isSaving.set(false);
                this._dialogRef.disableClose = false;
            }))
            .subscribe({
                next: result => {
                    this._snackbar.open('Cliente atualizado.');
                    this._dialogRef.close(result.data ?? undefined);
                },
                error: (error: HttpErrorResponse) => {
                    const message = error.error?.message ??
                        (typeof error.error === 'string' ? error.error : 'Não foi possível salvar o cliente. Tente de novo.');
                    this._snackbar.open(message);
                },
            });
    }
}
