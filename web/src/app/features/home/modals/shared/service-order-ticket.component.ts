import { Component, effect, inject, input, signal, ViewEncapsulation } from "@angular/core";
import { HttpErrorResponse } from "@angular/common/http";
import { MatButtonModule } from "@angular/material/button";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { finalize } from "rxjs";
import { ServiceOrder } from "../../../../shared/models/service-order.interface";
import { SnackbarService } from "../../../../shared/snackbar/snackbar.service";
import { EnumLabelPipe } from "../../../../shared/enums";
import { ServiceOrderLookupService } from "./service-order-lookup.service";

/**
 * Etiqueta lateral com o que já se sabe da OS, mais o campo para gravar a localização do produto.
 * Os estilos ficam na casca (_service-order-modal.scss).
 */
@Component({
    selector: 'app-service-order-ticket',
    templateUrl: './service-order-ticket.component.html',
    encapsulation: ViewEncapsulation.None,
    imports: [EnumLabelPipe, MatButtonModule, MatProgressSpinnerModule],
})
export class ServiceOrderTicketComponent {
    private _lookup = inject(ServiceOrderLookupService);
    private _snackbar = inject(SnackbarService);

    order = input.required<ServiceOrder>();
    serviceOrderId = input.required<number>();

    /** Localização exibida: começa com a da OS e é trocada pela gravada. */
    protected location = signal<string>('');
    protected newLocation = signal<string>('');
    protected isSavingLocation = signal<boolean>(false);

    constructor() {
        effect(() => this.location.set(this.order().location ?? ''));
    }

    protected saveLocation() {
        const value = this.newLocation().trim();
        if (!value || this.isSavingLocation()) {
            return;
        }

        this.isSavingLocation.set(true);

        this._lookup.addProductLocation(this.serviceOrderId(), value)
        .pipe(
            finalize(() => this.isSavingLocation.set(false))
        )
        .subscribe({
            next: result => {
                this.location.set(result.data ?? value.toUpperCase());
                this.newLocation.set('');
                this._snackbar.open('Localização salva.');
            },
            error: (error: HttpErrorResponse) => {
                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Não foi possível salvar a localização. Tente de novo.');

                this._snackbar.open(message);
            }
        });
    }
}
