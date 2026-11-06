import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, finalize, Observable, shareReplay, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

let refreshing$: Observable<void> | null = null; 

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const isApi = req.url.startsWith(environment.apiUrl); 
  // Rotas de sessão: usam só o cookie HttpOnly (withCredentials), nunca o Bearer.
  const isAuthCall = ['/auth/login', '/auth/refresh', '/auth/logout'].some(path => req.url.includes(path));

  const withToken = (r: HttpRequest<unknown>) => {
    if (isApi && isAuthCall) return r.clone({ withCredentials: true });
    const token = auth.accessToken();
    return token && isApi
      ? r.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : r;
  };

  return next(withToken(req)).pipe(
    catchError((e: unknown) => {
      if (!(e instanceof HttpErrorResponse) || e.status !== 401 || !isApi || isAuthCall) {
        return throwError(() => e);
      }

      refreshing$ ??= auth.refresh().pipe(
        shareReplay(1),
        finalize(() => (refreshing$ = null)),
      );

      return refreshing$.pipe(
        catchError(err => { 
          router.navigate(['/login'], { replaceUrl: true });
          return throwError(() => err);
        }),
        switchMap(() => next(withToken(req))),
      );
    }),
  );
};