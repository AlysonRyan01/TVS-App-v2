import { Component, inject, input, OnInit, signal } from "@angular/core";
import { FormBuilder, FormGroup } from "@angular/forms";
import { ServiceOrder } from "../../../../shared/models/service-order.interface";
import { AddServiceOrderQuoteModalService } from "./add-service-order-quote-modal.service";
import { finalize } from "rxjs";
import { HttpErrorResponse } from "@angular/common/http";
import { SnackbarService } from "../../../../shared/snackbar/snackbar.service";

@Component({
    selector: 'app-add-service-order-quote-modal',
    styleUrl: './add-service-order-quote-modal.component.scss',
    templateUrl: './add-service-order-quote-modal.component.html',
})
export class AddServiceOrderQuoteModalComponent implements OnInit {
    private _fb = inject(FormBuilder);
    private _service = inject(AddServiceOrderQuoteModalService);
    private _snackbar = inject(SnackbarService);


    protected isLoading = signal<boolean>(false);
    protected serviceOrder = signal<ServiceOrder | null>(null);
    id = input.required<number>();

    form = this._fb.nonNullable.group({

    })

    ngOnInit(): void {
        this.loadServiceOrder();
    }

    private loadServiceOrder() {
        this.isLoading.set(true);

        this._service.getServiceOrderById(this.id()).
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
}