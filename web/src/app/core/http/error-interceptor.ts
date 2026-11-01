import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, map, throwError } from 'rxjs';

const temCodigoDeErro = (body: unknown): body is { code: number; message?: string } => {
  const code = (body as { code?: unknown } | null)?.code;
  return typeof code === 'number' && (code < 200 || code > 299);
};

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const snack = inject(MatSnackBar);

  return next(req).pipe(
    map((event) => {
      if (event instanceof HttpResponse && temCodigoDeErro(event.body)) {
        throw new HttpErrorResponse({
          error: event.body,
          headers: event.headers,
          status: event.body.code,
          statusText: event.body.message ?? 'Erro',
          url: event.url ?? undefined,
        });
      }
      return event;
    }),
    catchError((e: HttpErrorResponse) => {
      const mensagem =
        e.error?.message
        ?? (e.status === 0 ? 'Sem conexão com o servidor'
          : e.status >= 500 ? 'Erro no servidor. Tente novamente.'
          : 'Erro na requisição');

      snack.open(mensagem, 'Fechar', {
        duration: 4000,
        horizontalPosition: 'right',
        verticalPosition: 'top',
      });

      return throwError(() => e);
    })
  );
};
