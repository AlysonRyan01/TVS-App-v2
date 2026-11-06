import { computed, inject, Service, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BaseResponse } from '../../shared/models/base-response.interface';
import { AuthTokens } from '../models/AuthTokens.interface';
import { environment } from '../../../environments/environment';
import { catchError, map, Observable, tap, throwError } from 'rxjs';

@Service()
export class AuthService {
    private _http = inject(HttpClient);

    private _accessToken = signal<string | null>(sessionStorage.getItem('accessToken'));
    private _refreshToken = signal<string | null>(sessionStorage.getItem('refreshToken'));

    accessToken = this._accessToken.asReadonly();
    isLoggedIn = computed(() => !!this._accessToken())

    login(email: string, password: string): Observable<BaseResponse<AuthTokens>> {
        return this._http.post<BaseResponse<AuthTokens>>(`${environment.apiUrl}/auth/login`, { email, password })
        .pipe(tap( res => {
            this._accessToken.set(res.data!.accessToken);
            this._refreshToken.set(res.data!.refreshToken);
            sessionStorage.setItem('accessToken', res.data!.accessToken);
            sessionStorage.setItem('refreshToken', res.data!.refreshToken);
            }
        ))
    };

    refresh(): Observable<void> {
        const refreshToken = this._refreshToken();
        if (!refreshToken) {
            this.logout();
            return throwError(() => new Error('Sessão expirada'));
        }

        return this._http
            .post<BaseResponse<string>>(`${environment.apiUrl}/auth/refresh`, { refreshToken })
            .pipe(
            map(res => {
                if (res.code < 200 || res.code > 299 || !res.data) {
                throw new Error(res.message ?? 'Sessão expirada');
                }
                this._accessToken.set(res.data);
                sessionStorage.setItem('accessToken', res.data);
                return;
            }),
            catchError(e => {
                this.logout();
                return throwError(() => e);
            })
            );
    }

    logout() {
        const token = this._refreshToken();
        if (token) this._http.post(`${environment.apiUrl}/auth/logout`, { refreshToken: token }).subscribe();
        this.limparSessao();
    }

    private limparSessao() {
        this._accessToken.set(null);
        this._refreshToken.set(null);
        sessionStorage.removeItem('accessToken');
        sessionStorage.removeItem('refreshToken');
    }
}
