import { Component, inject, signal, viewChild } from "@angular/core";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { HttpErrorResponse } from "@angular/common/http";
import { MatButtonModule } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatSelectModule } from "@angular/material/select";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { finalize } from "rxjs";
import { BaseLayoutComponent } from "../../shared/layout/base-layout/base-layout.component";
import { SnackbarService } from "../../shared/snackbar/snackbar.service";
import { EEnterprise, EProduct, enumOptions } from "../../shared/enums";
import { isPdfResponse, openPdfWindow, readBlobErrorMessage, showPdf } from "../../shared/pdf/pdf-print";
import { AddServiceOrderService } from "./add-service-order.service";
import { CustomerPickerComponent } from "../customers/customer-picker/customer-picker.component";
import { Customer } from "../customers/models/customer.model";

/**
 * Tela "Adicionar OS", em duas colunas iguais: cliente (app-customer-picker) e ordem de serviço.
 * Estilos comuns em shared/styles/_two-column-page.scss.
 */
@Component({
    selector: 'app-add-service-order',
    templateUrl: './add-service-order.component.html',
    host: { class: 'two-column-page' },
    imports: [
        BaseLayoutComponent,
        ReactiveFormsModule,
        MatButtonModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatProgressSpinnerModule,
        CustomerPickerComponent,
    ],
})
export class AddServiceOrderComponent {
    private _service = inject(AddServiceOrderService);
    private _snackbar = inject(SnackbarService);
    private _fb = inject(FormBuilder);

    private _picker = viewChild(CustomerPickerComponent);

    protected selectedCustomer = signal<Customer | null>(null);
    protected isSending = signal(false);

    protected readonly enterpriseOptions = enumOptions('enterprise');
    protected readonly productOptions = enumOptions('product');

    protected device = this._fb.nonNullable.group({
        enterprise: [EEnterprise.Particular as number],
        productType: [EProduct.Tv as number],
        productBrand: ['', Validators.required],
        productModel: ['', Validators.required],
        productSerialNumber: ['', Validators.required],
        productDefect: [''],
        accessories: [''],
    });

    protected createServiceOrder() {
        const customer = this.selectedCustomer();
        if (!customer || this.device.invalid || this.isSending()) {
            this.device.markAllAsTouched();
            return;
        }

        // A aba do termo precisa ser aberta no próprio clique (senão o navegador bloqueia).
        const pdfWindow = openPdfWindow();
        this.isSending.set(true);

        const raw = this.device.getRawValue();
        this._service.createServiceOrder({
            customerId: customer.id,
            productBrand: raw.productBrand,
            productModel: raw.productModel,
            productSerialNumber: raw.productSerialNumber,
            productDefect: raw.productDefect,
            accessories: raw.accessories,
            productType: raw.productType,
            enterprise: raw.enterprise,
        })
        .pipe(finalize(() => this.isSending.set(false)))
        .subscribe({
            next: response => {
                const id = response.headers.get('X-Service-Order-Id') ?? '';

                if (isPdfResponse(response)) {
                    showPdf(response.body!, pdfWindow, `ordem_servico_${id}.pdf`, `Termo de entrada - OS ${id}`);
                } else {
                    pdfWindow?.close();
                }

                this._snackbar.open(id ? `OS ${id} criada.` : 'OS criada.');
                this.reset();
            },
            error: async (error: HttpErrorResponse) => {
                pdfWindow?.close();
                this._snackbar.open(await readBlobErrorMessage(error, 'Não foi possível criar a OS. Tente de novo.'));
            },
        });
    }

    /** Depois de criar a OS: sem cliente, busca vazia e OS com os padrões. */
    private reset() {
        this._picker()?.reset();
        this.device.reset();
    }
}
