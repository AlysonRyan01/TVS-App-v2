import { HttpErrorResponse, HttpResponse } from "@angular/common/http";

/**
 * PDFs devolvidos pela API (termo de entrada e termo de entrega): abrir numa nova aba com a
 * impressão automática ou, se o navegador bloquear a aba, baixar o arquivo.
 */

/** Abre a aba **no próprio clique**: depois da requisição o navegador a trataria como pop-up. */
export function openPdfWindow(): Window | null {
    return window.open('', '_blank');
}

/** A resposta (com responseType 'blob') trouxe um PDF? */
export function isPdfResponse(response: HttpResponse<Blob>): boolean {
    return !!response.body && !!response.headers.get('content-type')?.includes('application/pdf');
}

/** Mostra o PDF na aba aberta e chama a impressão; sem aba, baixa como `fileName`. */
export function showPdf(pdf: Blob, pdfWindow: Window | null, fileName: string, title: string) {
    const url = URL.createObjectURL(pdf);

    if (pdfWindow) {
        printInWindow(pdfWindow, url, title);
    } else {
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.click();
    }

    // Tempo de sobra para a impressão e para reabrir o PDF na aba, se a pessoa precisar.
    setTimeout(() => URL.revokeObjectURL(url), 10 * 60 * 1000);
}

/** Com responseType 'blob', o corpo do erro também chega como Blob. */
export async function readBlobErrorMessage(error: HttpErrorResponse, fallback: string): Promise<string> {
    try {
        if (error.error instanceof Blob) {
            const body = JSON.parse(await error.error.text());
            return body?.message ?? fallback;
        }

        return error.error?.message ?? (typeof error.error === 'string' ? error.error : fallback);
    } catch {
        return fallback;
    }
}

/**
 * Uma página com o PDF em tela cheia dentro de um iframe: o PDF aberto direto na aba não deixa o
 * app chamar print(), mas o iframe (mesma origem do app) deixa.
 */
function printInWindow(pdfWindow: Window, url: string, title: string) {
    const doc = pdfWindow.document;
    doc.open();
    doc.write(`<!DOCTYPE html>
<html lang="pt-BR">
<head>
  <meta charset="utf-8">
  <title>${title}</title>
  <style>html, body { margin: 0; height: 100%; } iframe { border: 0; width: 100%; height: 100%; display: block; }</style>
</head>
<body><iframe title="${title}"></iframe></body>
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
