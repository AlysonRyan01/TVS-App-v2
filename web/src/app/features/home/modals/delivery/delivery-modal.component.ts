import { Component, computed, inject, signal } from "@angular/core";
import { ServiceOrderModalComponent } from "../shared/service-order-modal.component";
import { CurrencyPipe, DatePipe } from "@angular/common";
import { HttpErrorResponse } from "@angular/common/http";
import { isPdfResponse, openPdfWindow, readBlobErrorMessage, showPdf } from "../../../../shared/pdf/pdf-print";
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog";
import { MatButtonModule } from "@angular/material/button";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { finalize } from "rxjs";
import { ServiceOrder } from "../../../../shared/models/service-order.interface";
import { SnackbarService } from "../../../../shared/snackbar/snackbar.service";
import { EnumLabelPipe, ERepairResult, ERepairStatus } from "../../../../shared/enums";
import { DeliveryModalService } from "./delivery-modal.service";

const DEFAULT_ERROR = 'Não foi possível registrar a entrega. Tente de novo.';

@Component({
    selector: 'app-delivery-modal',
    styleUrl: './delivery-modal.component.scss',
    host: { class: 'service-order-modal-host' },
    templateUrl: './delivery-modal.component.html',
    imports: [
        ServiceOrderModalComponent,
        CurrencyPipe,
        DatePipe,
        EnumLabelPipe,
        MatDialogModule,
        MatButtonModule,
        MatProgressSpinnerModule,
    ]
})
export class DeliveryModalComponent {
    private _dialogRef = inject(MatDialogRef<DeliveryModalComponent>);
    private _service = inject(DeliveryModalService);
    private _snackbar = inject(SnackbarService);

    protected isSending = signal<boolean>(false);
    protected serviceOrder = signal<ServiceOrder | null>(null);
    id = inject<number>(MAT_DIALOG_DATA);

    protected isRepaired = computed(() => {
        const order = this.serviceOrder();
        return !!order
            && order.repairStatus === ERepairStatus.Approved
            && order.repairResult === ERepairResult.Repair;
    });

    protected registerDelivery() {
        if (this.isSending()) {
            return;
        }

        const pdfWindow = this.isRepaired() ? openPdfWindow() : null;

        this.isSending.set(true);
        this._dialogRef.disableClose = true;

        this._service.registerDelivery(this.id)
        .pipe(
            finalize(() => {
                this.isSending.set(false);
                this._dialogRef.disableClose = false;
            })
        )
        .subscribe({
            next: response => {
                if (isPdfResponse(response)) {
                    showPdf(response.body!, pdfWindow, `ordem_servico_${this.id}.pdf`, `Termo de entrega - OS ${this.id}`);
                } else {
                    pdfWindow?.close();
                }

                this._snackbar.success("Ordem de serviço entregue.");
                this._dialogRef.close(true);
            },
            error: async (error: HttpErrorResponse) => {
                pdfWindow?.close();
                this._snackbar.error(await readBlobErrorMessage(error, DEFAULT_ERROR));
            }
        });
    }
}
