import { Component, inject, OnInit, signal } from "@angular/core";
import { FormBuilder, Validators, ReactiveFormsModule } from "@angular/forms";
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog";
import { ServiceOrder } from "../../../../shared/models/service-order.interface";
import { AddServiceOrderQuoteModalService } from "./add-service-order-quote-modal.service";
import { finalize } from "rxjs";
import { HttpErrorResponse } from "@angular/common/http";
import { SnackbarService } from "../../../../shared/snackbar/snackbar.service";
import { enumOptions, ERepairResult } from "../../../../shared/enums";
import { MatButtonModule } from "@angular/material/button";
import { MatInputModule } from "@angular/material/input";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatSelectModule } from "@angular/material/select";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";

@Component({
    selector: 'app-add-service-order-quote-modal',
    styleUrl: './add-service-order-quote-modal.component.scss',
    templateUrl: './add-service-order-quote-modal.component.html',
    imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatProgressSpinnerModule]
})
export class AddServiceOrderQuoteModalComponent implements OnInit {
    private _dialogRef = inject(MatDialogRef<AddServiceOrderQuoteModalComponent>);
    private _fb = inject(FormBuilder);
    private _service = inject(AddServiceOrderQuoteModalService);
    private _snackbar = inject(SnackbarService);

    protected isLoading = signal<boolean>(false);
    protected isSending = signal<boolean>(false);
    protected serviceOrder = signal<ServiceOrder | null>(null);
    id = inject<number>(MAT_DIALOG_DATA)

    repairResultOptions = enumOptions('repairResult');

    form = this._fb.nonNullable.group({
        solution: ['', Validators.required],
        estimateMessage: ['', Validators.required],
        totalCost: [0, [Validators.required, Validators.min(0)]],
        guarantee: [''],
        partPurchaseCost: [0, [Validators.required, Validators.min(0)]],
        repairResult: [ERepairResult.Repair]
    })

    ngOnInit(): void {
        this.loadServiceOrder();
    }

    private loadServiceOrder() {
        this.isLoading.set(true);

        this._service.getServiceOrderById(this.id).
        pipe(
            finalize(() => this.isLoading.set(false))
        ).subscribe({
            next: result => {
                if (!result.data) {
                    this._snackbar.open(result.message ?? 'Ordem de serviço não encontrada.');
                    return;
                }

                const serviceOrder = result.data;
                this.serviceOrder.set(serviceOrder);
            },
            error: (error: HttpErrorResponse) => {
                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Erro ao carregar a ordem de serviço.');

                this._snackbar.open(message);
            }
        })
    }

    protected sendQuote() {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        this.isSending.set(true);

        const raw = this.form.getRawValue();
        const payload = {
            serviceOrderId: this.id,
            ...raw,
            totalCost: raw.totalCost ?? 0,
            partPurchaseCost: raw.partPurchaseCost ?? 0,
        }

        this._service.addServiceOrderQuote(payload)
        .pipe(
            finalize(() => this.isSending.set(false))
        )
        .subscribe({
            next: () => {
                this._snackbar.open("Orçamento enviado com sucesso");
                this._dialogRef.close(true);
            },
            error: (error: HttpErrorResponse) => {
                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Erro ao tentar enviar orçamento.');

                this._snackbar.open(message);
            }
        })
    }
}