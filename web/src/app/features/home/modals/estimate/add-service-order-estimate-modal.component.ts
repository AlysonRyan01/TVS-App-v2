import { Component, computed, inject, OnInit, signal } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { CurrencyPipe } from "@angular/common";
import { FormBuilder, Validators, ReactiveFormsModule } from "@angular/forms";
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog";
import { TextFieldModule } from "@angular/cdk/text-field";
import { ServiceOrder } from "../../../../shared/models/service-order.interface";
import { AddServiceOrderEstimateModalService } from "./add-service-order-estimate-modal.service";
import { finalize } from "rxjs";
import { HttpErrorResponse } from "@angular/common/http";
import { SnackbarService } from "../../../../shared/snackbar/snackbar.service";
import { EnumLabelPipe, enumOptions, ERepairResult } from "../../../../shared/enums";
import { MatButtonModule } from "@angular/material/button";
import { MatButtonToggleModule } from "@angular/material/button-toggle";
import { MatInputModule } from "@angular/material/input";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";

@Component({
    selector: 'app-add-service-order-estimate-modal',
    styleUrl: './add-service-order-estimate-modal.component.scss',
    templateUrl: './add-service-order-estimate-modal.component.html',
    imports: [
        ReactiveFormsModule,
        CurrencyPipe,
        EnumLabelPipe,
        TextFieldModule,
        MatDialogModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        MatButtonToggleModule,
        MatProgressSpinnerModule,
    ]
})
export class AddServiceOrderEstimateModalComponent implements OnInit {
    private _dialogRef = inject(MatDialogRef<AddServiceOrderEstimateModalComponent>);
    private _fb = inject(FormBuilder);
    private _service = inject(AddServiceOrderEstimateModalService);
    private _snackbar = inject(SnackbarService);

    protected isLoading = signal<boolean>(false);
    protected isSending = signal<boolean>(false);
    protected serviceOrder = signal<ServiceOrder | null>(null);
    id = inject<number>(MAT_DIALOG_DATA)

    repairResultOptions = enumOptions('repairResult');
    warrantyPresets = ['90 dias', '6 meses', '1 ano'];

    form = this._fb.group({
        repairResult: this._fb.nonNullable.control(ERepairResult.Repair),
        solution: this._fb.nonNullable.control('', Validators.required),
        estimateMessage: this._fb.nonNullable.control('', Validators.required),
        partPurchaseCost: this._fb.control<number | null>(null, [Validators.required, Validators.min(0)]),
        totalCost: this._fb.control<number | null>(null, [Validators.required, Validators.min(0)]),
        warranty: this._fb.nonNullable.control(''),
    })

    protected formValue = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

    protected margin = computed(() => {
        const { totalCost, partPurchaseCost } = this.formValue();
        if (totalCost == null || partPurchaseCost == null) {
            return null;
        }
        return totalCost - partPurchaseCost;
    });

    ngOnInit(): void {
        this.loadServiceOrder();
    }

    protected loadServiceOrder() {
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
                this.prefill(serviceOrder);
            },
            error: (error: HttpErrorResponse) => {
                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Erro ao carregar a ordem de serviço.');

                this._snackbar.open(message);
            }
        })
    }

    /** Se a OS já tiver um orçamento salvo, ele aparece para ser corrigido. */
    private prefill(order: ServiceOrder) {
        this.form.patchValue({
            ...(typeof order.repairResult === 'number' ? { repairResult: order.repairResult } : {}),
            ...(order.solution ? { solution: order.solution } : {}),
            ...(order.estimateMessage ? { estimateMessage: order.estimateMessage } : {}),
            ...(order.guarantee ? { warranty: order.guarantee } : {}),
            ...(order.partPurchaseCost ? { partPurchaseCost: order.partPurchaseCost } : {}),
            ...(order.totalAmount ? { totalCost: order.totalAmount } : {}),
        });
    }

    protected setWarranty(value: string) {
        const control = this.form.controls.warranty;
        control.setValue(control.value === value ? '' : value);
        control.markAsDirty();
    }

    protected sendEstimate() {
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

        this._service.addServiceOrderEstimate(payload)
        .pipe(
            finalize(() => this.isSending.set(false))
        )
        .subscribe({
            next: () => {
                this._snackbar.open("Orçamento enviado.");
                this._dialogRef.close(true);
            },
            error: (error: HttpErrorResponse) => {
                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Não foi possível enviar o orçamento. Tente de novo.');

                this._snackbar.open(message);
            }
        })
    }
}
