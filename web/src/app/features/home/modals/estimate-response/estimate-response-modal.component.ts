import { Component, inject, signal } from "@angular/core";
import { ServiceOrderModalComponent } from "../shared/service-order-modal.component";
import { CurrencyPipe } from "@angular/common";
import { HttpErrorResponse } from "@angular/common/http";
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog";
import { MatButtonModule } from "@angular/material/button";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { finalize, Observable } from "rxjs";
import { ServiceOrder } from "../../../../shared/models/service-order.interface";
import { BaseResponse } from "../../../../shared/models/base-response.interface";
import { SnackbarService } from "../../../../shared/snackbar/snackbar.service";
import { EnumLabelPipe } from "../../../../shared/enums";
import { EstimateResponseModalService } from "./estimate-response-modal.service";

type EstimateResponseAction = 'approve' | 'reject';

@Component({
    selector: 'app-estimate-response-modal',
    styleUrl: './estimate-response-modal.component.scss',
    host: { class: 'service-order-modal-host' },
    templateUrl: './estimate-response-modal.component.html',
    imports: [
        ServiceOrderModalComponent,
        CurrencyPipe,
        EnumLabelPipe,
        MatDialogModule,
        MatButtonModule,
        MatProgressSpinnerModule,
    ]
})
export class EstimateResponseModalComponent {
    private _dialogRef = inject(MatDialogRef<EstimateResponseModalComponent>);
    private _service = inject(EstimateResponseModalService);
    private _snackbar = inject(SnackbarService);

    protected serviceOrder = signal<ServiceOrder | null>(null);
    protected sendingAction = signal<EstimateResponseAction | null>(null);
    /** A OS foi respondida em outra tela: a lista precisa recarregar quando o modal fechar. */
    protected answeredElsewhere = signal<boolean>(false);
    id = inject<number>(MAT_DIALOG_DATA);


    protected isSending(): boolean {
        return this.sendingAction() !== null;
    }

    protected approve() {
        this.respond('approve', this._service.approveEstimate(this.id), 'Orçamento aprovado.');
    }

    protected reject() {
        this.respond('reject', this._service.rejectEstimate(this.id), 'Orçamento reprovado.');
    }

    private respond(
        action: EstimateResponseAction,
        request$: Observable<BaseResponse<ServiceOrder>>,
        successMessage: string) {
        if (this.isSending()) {
            return;
        }

        this.sendingAction.set(action);
        this._dialogRef.disableClose = true;

        request$
        .pipe(
            finalize(() => {
                this.sendingAction.set(null);
                this._dialogRef.disableClose = false;
            })
        )
        .subscribe({
            next: () => {
                this._snackbar.open(successMessage);
                this._dialogRef.close(true);
            },
            error: (error: HttpErrorResponse) => {
                if (error.status === 409) {
                    this.answeredElsewhere.set(true);
                }

                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Não foi possível registrar a resposta. Tente de novo.');

                this._snackbar.open(message);
            }
        });
    }
}
