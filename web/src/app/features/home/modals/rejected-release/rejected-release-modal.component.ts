import { Component, inject, signal } from "@angular/core";
import { ServiceOrderModalComponent } from "../shared/service-order-modal.component";
import { CurrencyPipe, DatePipe } from "@angular/common";
import { HttpErrorResponse } from "@angular/common/http";
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog";
import { MatButtonModule } from "@angular/material/button";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { finalize } from "rxjs";
import { ServiceOrder } from "../../../../shared/models/service-order.interface";
import { SnackbarService } from "../../../../shared/snackbar/snackbar.service";
import { RejectedReleaseModalService } from "./rejected-release-modal.service";

@Component({
    selector: 'app-rejected-release-modal',
    host: { class: 'service-order-modal-host' },
    templateUrl: './rejected-release-modal.component.html',
    imports: [
        ServiceOrderModalComponent,
        CurrencyPipe,
        DatePipe,
        MatDialogModule,
        MatButtonModule,
        MatProgressSpinnerModule,
    ]
})
export class RejectedReleaseModalComponent {
    private _dialogRef = inject(MatDialogRef<RejectedReleaseModalComponent>);
    private _service = inject(RejectedReleaseModalService);
    private _snackbar = inject(SnackbarService);

    protected isSending = signal<boolean>(false);
    protected serviceOrder = signal<ServiceOrder | null>(null);
    id = inject<number>(MAT_DIALOG_DATA);


    protected release() {
        if (this.isSending()) {
            return;
        }

        this.isSending.set(true);
        this._dialogRef.disableClose = true;

        this._service.markAsReadyForPickup(this.id)
        .pipe(
            finalize(() => {
                this.isSending.set(false);
                this._dialogRef.disableClose = false;
            })
        )
        .subscribe({
            next: () => {
                this._snackbar.success("Ordem de serviço liberada para retirada.");
                this._dialogRef.close(true);
            },
            error: (error: HttpErrorResponse) => {
                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Não foi possível liberar a ordem de serviço. Tente de novo.');

                this._snackbar.error(message);
            }
        });
    }
}
