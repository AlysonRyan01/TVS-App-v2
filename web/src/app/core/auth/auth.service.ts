import { computed, inject, Service, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BaseResponse } from '../../shared/models/base-response.interface';
import { AuthTokens } from '../models/AuthTokens.interface';
import { environment } from '../../../environments/environment';
import { catchError, firstValueFrom, map, Observable, tap, throwError } from 'rxjs';

const LOGOUT_CHANNEL = 'tvs-auth';
const LEGACY_KEYS = ['accessToken', 'refreshToken'];

/**
 * Sessão do usuário.
 * - Token de acesso (1 h): só em memória; some ao fechar a aba.
 * - Sessão de 30 dias: cookie HttpOnly "tvs_refresh" gravado pela API — o Angular nunca o lê.
 * Ao abrir o sistema, restore() chama /auth/refresh (o navegador envia o cookie) e recupera o acesso.
 */
@Service()
export class AuthService {
    private _http = inject(HttpClient);
    private _router = inject(Router);

    private _accessToken = signal<string | null>(null);
    private _channel = typeof BroadcastChannel !== 'undefined' ? new BroadcastChannel(LOGOUT_CHANNEL) : null;

    accessToken = this._accessToken.asReadonly();
    isLoggedIn = computed(() => !!this._accessToken());

    constructor() {
        // Tokens da versão anterior (sessionStorage) não são mais usados.
        try {
            LEGACY_KEYS.forEach(key => sessionStorage.removeItem(key));
        } catch { /* armazenamento indisponível */ }

        // Saiu em outra aba → esta aba também sai.
        if (this._channel) {
            this._channel.onmessage = event => {
                if (event.data?.type === 'logout') {
                    this._accessToken.set(null);
                    this._router.navigate(['/login'], { replaceUrl: true });
                }
            };
        }
    }

    login(email: string, password: string): Observable<BaseResponse<AuthTokens>> {
        return this._http
            .post<BaseResponse<AuthTokens>>(`${environment.apiUrl}/auth/login`, { email, password }, { withCredentials: true })
            .pipe(tap(res => this._accessToken.set(res.data!.accessToken)));
    }

    /** Pede um novo token de acesso usando o cookie de sessão. */
    refresh(): Observable<void> {
        return this._http
            .post<BaseResponse<string>>(`${environment.apiUrl}/auth/refresh`, {}, { withCredentials: true })
            .pipe(
                map(res => {
                    if (res.code < 200 || res.code > 299 || !res.data) {
                        throw new Error(res.message ?? 'Sessão expirada');
                    }
                    this._accessToken.set(res.data);
                }),
                catchError(e => {
                    this._accessToken.set(null);
                    return throwError(() => e);
                })
            );
    }

    /** Usado na inicialização: tenta recuperar a sessão; sem sessão, só fica deslogado. */
    restore(): Promise<void> {
        return firstValueFrom(this.refresh()).catch(() => undefined);
    }

    logout() {
        this._http.post(`${environment.apiUrl}/auth/logout`, {}, { withCredentials: true }).subscribe({ error: () => undefined });
        this._accessToken.set(null);
        this._channel?.postMessage({ type: 'logout' });
    }
}
