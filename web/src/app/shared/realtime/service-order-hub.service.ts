import { Injectable, OnDestroy } from "@angular/core";
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import { Observable, Subject } from "rxjs";
import { environment } from "../../../environments/environment";

@Injectable({ providedIn: 'root' })
export class ServiceOrderHubService implements OnDestroy {
    private readonly _updates = new Subject<string>();
    private readonly _connection: HubConnection = new HubConnectionBuilder()
        .withUrl(`${environment.apiUrl}/osHub`)
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build();

    readonly updates: Observable<string> = this._updates.asObservable();

    private _stopped = false;

    constructor() {
        this._connection.on('Atualizar', (message: string) => this._updates.next(message));

        this._connection.onclose(() => this.retryLater());
    }

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
