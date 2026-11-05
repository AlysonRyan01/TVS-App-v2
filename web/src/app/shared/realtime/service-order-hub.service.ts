import { Injectable, OnDestroy } from "@angular/core";
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { Observable, Subject } from "rxjs";
import { environment } from "../../../environments/environment";

/**
 * Conexão com o hub de ordens de serviço da API (/osHub).
 * Toda ação que muda uma OS (orçamento, resposta, compra, conserto, entrega...) envia "Atualizar";
 * as telas abertas escutam `updates` para recarregar a lista.
 */
@Injectable({ providedIn: 'root' })
export class ServiceOrderHubService implements OnDestroy {
    private readonly _updates = new Subject<string>();
    private readonly _connection: HubConnection = new HubConnectionBuilder()
        .withUrl(`${environment.apiUrl}/osHub`)
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build();

    /** Mensagem enviada pela API a cada mudança em uma OS. */
    readonly updates: Observable<string> = this._updates.asObservable();

    private _stopped = false;

    constructor() {
        this._connection.on('Atualizar', (message: string) => this._updates.next(message));

        // Se as tentativas automáticas se esgotarem, tenta de novo depois de um tempo.
        this._connection.onclose(() => this.retryLater());
    }

    /** Conecta ao hub; chamar mais de uma vez não abre conexões repetidas. */
    start(): void {
        if (this._stopped || this._connection.state !== HubConnectionState.Disconnected) {
            return;
        }

        this._connection.start().catch(() => this.retryLater());
    }

    private retryLater() {
        if (!this._stopped) {
            setTimeout(() => this.start(), 10000);
        }
    }

    ngOnDestroy(): void {
        this._stopped = true;
        this._connection.stop();
        this._updates.complete();
    }
}
