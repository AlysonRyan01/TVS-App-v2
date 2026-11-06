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
import { EProduct, enumOptions } from "../../shared/enums";
import { isPdfResponse, openPdfWindow, readBlobErrorMessage, showPdf } from "../../shared/pdf/pdf-print";
import { CustomerPickerComponent } from "../customers/customer-picker/customer-picker.component";
import { Customer } from "../customers/models/customer.model";
import { SellService } from "./sell.service";

/**
 * Tela "Vender": mesmo layout da Adicionar OS (cliente | venda). Registra a venda como OS de venda
 * (defeito "VENDA", como o handler antigo), abre o comprovante e o cliente recebe o WhatsApp da venda.
 * Estilos comuns em shared/styles/_two-column-page.scss.
 */
@Component({
    selector: 'app-sell',
    templateUrl: './sell.component.html',
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
export class SellComponent {
    private _service = inject(SellService);
    private _snackbar = inject(SnackbarService);
    private _fb = inject(FormBuilder);

    private _picker = viewChild(CustomerPickerComponent);

    protected selectedCustomer = signal<Customer | null>(null);
    protected isSending = signal(false);

    protected readonly productOptions = enumOptions('product');

    protected sale = this._fb.group({
        productType: this._fb.nonNullable.control(EProduct.Tv as number),
        productBrand: this._fb.nonNullable.control(''),
        productModel: this._fb.nonNullable.control('', Validators.required),
        productSerialNumber: this._fb.nonNullable.control('', Validators.required),
        guarantee: this._fb.nonNullable.control('', Validators.required),
        amount: this._fb.control<number | null>(null, [Validators.required, Validators.min(0.01)]),
    });

    protected createSale() {
        const customer = this.selectedCustomer();
        if (!customer || this.sale.invalid || this.isSending()) {
            this.sale.markAllAsTouched();
            return;
        }

        // A aba do comprovante precisa ser aberta no próprio clique (senão o navegador bloqueia).
        const pdfWindow = openPdfWindow();
        this.isSending.set(true);

        const raw = this.sale.getRawValue();
        this._service.createSale({
            customerId: customer.id,
            productType: raw.productType,
            productBrand: raw.productBrand,
            productModel: raw.productModel,
            productSerialNumber: raw.productSerialNumber,
            guarantee: raw.guarantee,
            amount: raw.amount!,
        })
        .pipe(finalize(() => this.isSending.set(false)))
        .subscribe({
            next: response => {
                const id = response.headers.get('X-Service-Order-Id') ?? '';

                if (isPdfResponse(response)) {
                    showPdf(response.body!, pdfWindow, `venda_${id}.pdf`, `Comprovante de venda - OS ${id}`);
                } else {
                    pdfWindow?.close();
                }

                this._snackbar.open(id ? `Venda registrada (OS ${id}).` : 'Venda registrada.');
                this.reset();
            },
            // Em caso de erro nada é limpo: a pessoa corrige e tenta de novo.
            error: async (error: HttpErrorResponse) => {
                pdfWindow?.close();
                this._snackbar.open(await readBlobErrorMessage(error, 'Não foi possível registrar a venda. Tente de novo.'));
            },
        });
    }

    /** Depois de registrar: sem cliente, busca vazia e venda com os padrões. */
    private reset() {
        this._picker()?.reset();
        this.sale.reset();
    }
}
