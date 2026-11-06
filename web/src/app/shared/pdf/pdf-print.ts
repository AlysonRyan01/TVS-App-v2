import { HttpErrorResponse, HttpResponse } from "@angular/common/http";

export function openPdfWindow(): Window | null {
    return window.open('', '_blank');
}

export function isPdfResponse(response: HttpResponse<Blob>): boolean {
    return !!response.body && !!response.headers.get('content-type')?.includes('application/pdf');
}

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

    setTimeout(() => URL.revokeObjectURL(url), 10 * 60 * 1000);
}

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
        setTimeout(() => {
            try {
                frame.contentWindow?.focus();
                frame.contentWindow?.print();
            } catch {
            }
        }, 300);
    }, { once: true });
    frame.src = url;
}
