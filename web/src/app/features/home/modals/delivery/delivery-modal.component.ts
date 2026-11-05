import { Component, computed, inject, signal } from "@angular/core";
import { ServiceOrderModalComponent } from "../shared/service-order-modal.component";
import { CurrencyPipe, DatePipe } from "@angular/common";
import { HttpErrorResponse, HttpResponse } from "@angular/common/http";
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

    /** Mesma regra do backend: só OS consertada gera o termo de entrega em PDF. */
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

        // A aba precisa ser aberta no próprio clique; depois da requisição o navegador a bloquearia.
        const pdfWindow = this.isRepaired() ? window.open('', '_blank') : null;

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
                if (this.isPdf(response)) {
                    this.showPdf(response.body!, pdfWindow);
                } else {
                    pdfWindow?.close();
                }

                this._snackbar.open("Ordem de serviço entregue.");
                this._dialogRef.close(true);
            },
            error: async (error: HttpErrorResponse) => {
                pdfWindow?.close();
                this._snackbar.open(await this.readErrorMessage(error));
            }
        });
    }

    private isPdf(response: HttpResponse<Blob>): boolean {
        return !!response.body && !!response.headers.get('content-type')?.includes('application/pdf');
    }

    /** Abre o termo na aba já aberta e chama a impressão; se a aba foi bloqueada, baixa o arquivo. */
    private showPdf(pdf: Blob, pdfWindow: Window | null) {
        const url = URL.createObjectURL(pdf);

        if (pdfWindow) {
            this.printInWindow(pdfWindow, url);
        } else {
            const link = document.createElement('a');
            link.href = url;
            link.download = `ordem_servico_${this.id}.pdf`;
            link.click();
        }

        // Tempo de sobra para a impressão e para reabrir o PDF na aba, se a pessoa precisar.
        setTimeout(() => URL.revokeObjectURL(url), 10 * 60 * 1000);
    }

    /**
     * Uma página com o PDF em tela cheia dentro de um iframe: o PDF aberto direto na aba não
     * deixa o app chamar print(), mas o iframe (mesma origem do app) deixa.
     */
    private printInWindow(pdfWindow: Window, url: string) {
        const doc = pdfWindow.document;
        doc.open();
        doc.write(`<!DOCTYPE html>
<html lang="pt-BR">
<head>
  <meta charset="utf-8">
  <title>Termo de entrega - OS ${this.id}</title>
  <style>html, body { margin: 0; height: 100%; } iframe { border: 0; width: 100%; height: 100%; display: block; }</style>
</head>
<body><iframe title="Termo de entrega - OS ${this.id}"></iframe></body>
</html>`);
        doc.close();

        const frame = doc.querySelector('iframe')!;
        frame.addEventListener('load', () => {
            // Pequena espera para o leitor de PDF do navegador terminar de desenhar a página.
            setTimeout(() => {
                try {
                    frame.contentWindow?.focus();
                    frame.contentWindow?.print();
                } catch {
                    // Se o navegador não deixar, o PDF continua aberto na aba para imprimir pelo botão dele.
                }
            }, 300);
        }, { once: true });
        frame.src = url;
    }

    /** Com responseType 'blob', o corpo do erro também chega como Blob. */
    private async readErrorMessage(error: HttpErrorResponse): Promise<string> {
        try {
            if (error.error instanceof Blob) {
                const body = JSON.parse(await error.error.text());
                return body?.message ?? DEFAULT_ERROR;
            }

            return error.error?.message ?? (typeof error.error === 'string' ? error.error : DEFAULT_ERROR);
        } catch {
            return DEFAULT_ERROR;
        }
    }
}
