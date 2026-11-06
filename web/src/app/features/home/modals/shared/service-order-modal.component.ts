import { Component, inject, input, OnInit, output, signal, ViewEncapsulation } from "@angular/core";
import { HttpErrorResponse } from "@angular/common/http";
import { MatDialogModule } from "@angular/material/dialog";
import { MatButtonModule } from "@angular/material/button";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { finalize } from "rxjs";
import { ServiceOrder } from "../../../../shared/models/service-order.interface";
import { SnackbarService } from "../../../../shared/snackbar/snackbar.service";
import { ServiceOrderLookupService } from "./service-order-lookup.service";
import { ServiceOrderTicketComponent } from "./service-order-ticket.component";

@Component({
    selector: 'app-service-order-modal',
    templateUrl: './service-order-modal.component.html',
    encapsulation: ViewEncapsulation.None,
    imports: [MatDialogModule, MatButtonModule, MatProgressSpinnerModule, ServiceOrderTicketComponent],
})
export class ServiceOrderModalComponent implements OnInit {
    private _lookup = inject(ServiceOrderLookupService);
    private _snackbar = inject(SnackbarService);

    serviceOrderId = input.required<number>();
    title = input.required<string>();
    closeLabel = input.required<string>();
    sending = input(false);
    closeResult = input<unknown>(undefined);

    loaded = output<ServiceOrder>();

    protected isLoading = signal<boolean>(false);
    protected serviceOrder = signal<ServiceOrder | null>(null);

    ngOnInit(): void {
        this.loadServiceOrder();
    }

    protected loadServiceOrder() {
        this.isLoading.set(true);

        this._lookup.getById(this.serviceOrderId())
        .pipe(
            finalize(() => this.isLoading.set(false))
        ).subscribe({
            next: result => {
                if (!result.data) {
                    this._snackbar.error(result.message ?? 'Ordem de serviço não encontrada.');
                    return;
                }

                this.serviceOrder.set(result.data);
                this.loaded.emit(result.data);
            },
            error: (error: HttpErrorResponse) => {
                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Erro ao carregar a ordem de serviço.');

                this._snackbar.error(message);
            }
        });
    }
}
