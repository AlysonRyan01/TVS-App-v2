import { Component, DestroyRef, inject, OnInit, signal } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { BaseLayoutComponent } from "../../shared/layout/base-layout/base-layout.component";
import { MatTabsModule } from "@angular/material/tabs";
import { MatTableModule } from "@angular/material/table";
import { ApprovedServiceOrder } from "./models/approved-service-order.interface";
import { PendingEstimateServiceOrder } from "./models/pending-estimate-service-order.interface";
import { ReadyForPickupServiceOrder } from "./models/ready-for-pickup-service-order.interface";
import { RejectedServiceOrder } from "./models/rejected-service-order.interface";
import { AwaitingResponseServiceOrder } from "./models/awaiting-response-service-order.interface";
import { AwaitingPartServiceOrder } from "./models/awaiting-part-service-order.interface";
import { HomeService } from "./home.service";
import { TablePagination } from "./models/table-pagination.interface";
import { DatePipe } from '@angular/common';
import { MatPaginator } from "@angular/material/paginator";
import { PageEvent } from '@angular/material/paginator';
import { MatTabChangeEvent } from "@angular/material/tabs";
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { debounceTime, finalize } from "rxjs";
import { ServiceOrderHubService } from "../../shared/realtime/service-order-hub.service";
import { HttpErrorResponse } from "@angular/common/http";
import { SnackbarService } from "../../shared/snackbar/snackbar.service";
import { CurrencyPipe } from "@angular/common";
import { EnumLabelPipe } from "../../shared/enums";
import { MatButtonModule } from "@angular/material/button";
import { MatDialog } from "@angular/material/dialog";
import { AddServiceOrderEstimateModalComponent } from "./modals/estimate/add-service-order-estimate-modal.component";
import { EstimateResponseModalComponent } from "./modals/estimate-response/estimate-response-modal.component";
import { PartPurchaseModalComponent } from "./modals/part-purchase/part-purchase-modal.component";
import { RejectedReleaseModalComponent } from "./modals/rejected-release/rejected-release-modal.component";
import { RepairModalComponent } from "./modals/repair/repair-modal.component";
import { DeliveryModalComponent } from "./modals/delivery/delivery-modal.component";

@Component({
    selector: 'app-home',
    styleUrl: 'home.component.scss',
    templateUrl: 'home.component.html',
    imports: [BaseLayoutComponent, MatTabsModule, MatTableModule, DatePipe, MatPaginator, MatProgressSpinner, CurrencyPipe, EnumLabelPipe, MatButtonModule],
})
export class HomeComponent implements OnInit {
    private _service = inject(HomeService);
    private _snack = inject(SnackbarService);

    protected isLoadingPendingEstimateServiceOrders = signal<boolean>(false);
    protected isLoadingAwaitingResponseServiceOrders = signal<boolean>(false);
    protected isLoadingApprovedServiceOrders = signal<boolean>(false);
    protected isLoadingRejectedServiceOrders = signal<boolean>(false);
    protected isLoadingAwaitingPartServiceOrders = signal<boolean>(false);
    protected isLoadingReadyForPickupServiceOrders = signal<boolean>(false);

    protected pendingEstimateServiceOrders = signal<PendingEstimateServiceOrder[]>([]);
    protected awaitingResponseServiceOrders = signal<AwaitingResponseServiceOrder[]>([]);
    protected approvedServiceOrders = signal<ApprovedServiceOrder[]>([]);
    protected rejectedServiceOrders = signal<RejectedServiceOrder[]>([]);
    protected awaitingPartServiceOrders = signal<AwaitingPartServiceOrder[]>([]);
    protected readyForPickupServiceOrders = signal<ReadyForPickupServiceOrder[]>([]);

    protected pendingEstimateServiceOrdersPagination = signal<TablePagination>(this.createPagination());
    protected awaitingResponseServiceOrdersPagination = signal<TablePagination>(this.createPagination());
    protected approvedServiceOrdersPagination = signal<TablePagination>(this.createPagination());
    protected rejectedServiceOrdersPagination = signal<TablePagination>(this.createPagination());
    protected awaitingPartServiceOrdersPagination = signal<TablePagination>(this.createPagination());
    protected readyForPickupServiceOrdersPagination = signal<TablePagination>(this.createPagination());

    protected pendingEstimateColumns = ['number', 'company', 'customer', 'model', 'enteredAt', 'action'];
    protected awaitingResponseColumns = ['number','customer','model','solution','amount','evaluatedAt','action'];
    protected approvedColumns = ['number', 'company', 'customer', 'model', 'solution', 'approvedAt', 'action'];
    protected rejectedColumns = ['number', 'customer', 'model', 'solution', 'rejectedAt', 'action'];
    protected awaitingPartColumns = ['number', 'customer', 'model', 'solution', 'partPurchasedAt', 'action'];
    protected readyForPickupColumns = ['number', 'customer', 'model', 'repairedAt', 'repairResult', 'repairStatus', 'action'];

    protected dialog = inject(MatDialog);

    private _hub = inject(ServiceOrderHubService);
    private _destroyRef = inject(DestroyRef);

    /** Aba aberta agora: é a que recarrega quando outra tela muda uma OS. */
    private _selectedTab = 0;

    ngOnInit(): void {
        this.loadPendingEstimateServiceOrders();
        this.listenToServiceOrderUpdates();
    }

    /**
     * Outra tela (ou outro computador) mudou uma OS: recarrega a aba aberta, mantendo a página.
     * O debounce junta avisos em sequência num recarregamento só.
     */
    private listenToServiceOrderUpdates() {
        this._hub.start();
        this._hub.updates
            .pipe(debounceTime(500), takeUntilDestroyed(this._destroyRef))
            .subscribe(() => this.loadTab(this._selectedTab));
    }

    protected openEstimateDialog(id: number) {
        this.dialog.open(AddServiceOrderEstimateModalComponent, {
            data: id,
            width: '100vw',
            height: '100vh',
            maxWidth: '100vw',
            maxHeight: '100vh',
            panelClass: 'fullscreen-dialog',
            autoFocus: 'dialog',
        }).afterClosed().subscribe(sent => {
            // A OS sai da fila de orçamentos pendentes assim que o orçamento é enviado.
            if (sent) {
                this.loadPendingEstimateServiceOrders();
            }
        })
    }

    protected openEstimateResponseDialog(id: number) {
        this.dialog.open(EstimateResponseModalComponent, {
            data: id,
            width: '100vw',
            height: '100vh',
            maxWidth: '100vw',
            maxHeight: '100vh',
            panelClass: 'fullscreen-dialog',
            autoFocus: 'dialog',
        }).afterClosed().subscribe(responded => {
            // A OS sai da fila assim que a resposta é registrada (ou se já tinha sido respondida em outra tela).
            if (responded) {
                this.loadAwaitingResponseServiceOrders();
            }
        })
    }

    protected openPartPurchaseDialog(id: number) {
        this.dialog.open(PartPurchaseModalComponent, {
            data: id,
            width: '100vw',
            height: '100vh',
            maxWidth: '100vw',
            maxHeight: '100vh',
            panelClass: 'fullscreen-dialog',
            autoFocus: 'dialog',
        }).afterClosed().subscribe(purchased => {
            // A OS sai de "Aprovados" e vai para "Aguardando peça" assim que a compra é registrada.
            if (purchased) {
                this.loadApprovedServiceOrders();
            }
        })
    }

    protected openRejectedReleaseDialog(id: number) {
        this.dialog.open(RejectedReleaseModalComponent, {
            data: id,
            width: '100vw',
            height: '100vh',
            maxWidth: '100vw',
            maxHeight: '100vh',
            panelClass: 'fullscreen-dialog',
            autoFocus: 'dialog',
        }).afterClosed().subscribe(released => {
            // A OS sai de "Reprovados" e vai para "Prontos" assim que é liberada.
            if (released) {
                this.loadRejectedServiceOrders();
            }
        })
    }

    protected openRepairDialog(id: number) {
        this.dialog.open(RepairModalComponent, {
            data: id,
            width: '100vw',
            height: '100vh',
            maxWidth: '100vw',
            maxHeight: '100vh',
            panelClass: 'fullscreen-dialog',
            autoFocus: 'dialog',
        }).afterClosed().subscribe(repaired => {
            // A OS sai de "Aguardando peça" e vai para "Prontos" assim que o conserto é registrado.
            if (repaired) {
                this.loadAwaitingPartServiceOrders();
            }
        })
    }

    protected openDeliveryDialog(id: number) {
        this.dialog.open(DeliveryModalComponent, {
            data: id,
            width: '100vw',
            height: '100vh',
            maxWidth: '100vw',
            maxHeight: '100vh',
            panelClass: 'fullscreen-dialog',
            autoFocus: 'dialog',
        }).afterClosed().subscribe(delivered => {
            // A OS sai de "Prontos" assim que a entrega é registrada.
            if (delivered) {
                this.loadReadyForPickupServiceOrders();
            }
        })
    }

    private loadPendingEstimateServiceOrders() {
        this.isLoadingPendingEstimateServiceOrders.set(true);
        this.pendingEstimateServiceOrders.set([]);

        const pagination = this.pendingEstimateServiceOrdersPagination();

        this._service.getPendingEstimateServiceOrders({pageNumber: pagination.pageNumber, pageSize: pagination.pageSize})
        .pipe(
            finalize(() => this.isLoadingPendingEstimateServiceOrders.set(false))
        )
        .subscribe({
            next: result => {
            const data = result.data!;
            this.pendingEstimateServiceOrders.set(data.items);
            this.pendingEstimateServiceOrdersPagination.set(this.createPaginationWithParams(data.pageNumber, data.pageSize, data.totalCount));
        },
            error: (error: HttpErrorResponse) => {
                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Erro ao carregar ordens de serviço.');

                this._snack.open(message);
            }
        })
    }

    private loadAwaitingResponseServiceOrders() {
        this.isLoadingAwaitingResponseServiceOrders.set(true);
        this.awaitingResponseServiceOrders.set([]);
        const pagination = this.awaitingResponseServiceOrdersPagination();

        this._service.getAwaitingResponseServiceOrders({pageNumber: pagination.pageNumber, pageSize: pagination.pageSize})
        .pipe(
            finalize(() => this.isLoadingAwaitingResponseServiceOrders.set(false))
        )
        .subscribe({
            next: result => {
                const data = result.data!;
                this.awaitingResponseServiceOrders.set(data.items);
                this.awaitingResponseServiceOrdersPagination.set(this.createPaginationWithParams(data.pageNumber, data.pageSize, data.totalCount));
            },
            error: (error: HttpErrorResponse) => {
                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Erro ao carregar ordens de serviço.');

                this._snack.open(message);
            }
        })
    }

    private loadApprovedServiceOrders() {
        this.isLoadingApprovedServiceOrders.set(true);
        this.approvedServiceOrders.set([]);
        const pagination = this.approvedServiceOrdersPagination();

        this._service.getApprovedServiceOrders({pageNumber: pagination.pageNumber, pageSize: pagination.pageSize})
        .pipe(
            finalize(() => this.isLoadingApprovedServiceOrders.set(false))
        )
        .subscribe({
            next: result => {
                const data = result.data!;
                this.approvedServiceOrders.set(data.items);
                this.approvedServiceOrdersPagination.set(this.createPaginationWithParams(data.pageNumber, data.pageSize, data.totalCount));
            },
            error: (error: HttpErrorResponse) => {
                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Erro ao carregar ordens de serviço.');

                this._snack.open(message);
            }
        })
    }

    private loadRejectedServiceOrders() {
        this.isLoadingRejectedServiceOrders.set(true);
        this.rejectedServiceOrders.set([]);
        const pagination = this.rejectedServiceOrdersPagination();

        this._service.getRejectedServiceOrders({pageNumber: pagination.pageNumber, pageSize: pagination.pageSize})
        .pipe(
            finalize(() => this.isLoadingRejectedServiceOrders.set(false))
        )
        .subscribe({
            next: result => {
                const data = result.data!;
                this.rejectedServiceOrders.set(data.items);
                this.rejectedServiceOrdersPagination.set(this.createPaginationWithParams(data.pageNumber, data.pageSize, data.totalCount));
            },
            error: (error: HttpErrorResponse) => {
                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Erro ao carregar ordens de serviço.');

                this._snack.open(message);
            }
        })
    }

    private loadAwaitingPartServiceOrders() {
        this.isLoadingAwaitingPartServiceOrders.set(true);
        this.awaitingPartServiceOrders.set([]);
        const pagination = this.awaitingPartServiceOrdersPagination();

        this._service.getAwaitingPartServiceOrders({pageNumber: pagination.pageNumber, pageSize: pagination.pageSize})
        .pipe(
            finalize(() => this.isLoadingAwaitingPartServiceOrders.set(false))
        )
        .subscribe({
            next: result => {
                const data = result.data!;
                this.awaitingPartServiceOrders.set(data.items);
                this.awaitingPartServiceOrdersPagination.set(this.createPaginationWithParams(data.pageNumber, data.pageSize, data.totalCount));
            },
            error: (error: HttpErrorResponse) => {
                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Erro ao carregar ordens de serviço.');

                this._snack.open(message);
            }
        })
    }

    private loadReadyForPickupServiceOrders() {
        this.isLoadingReadyForPickupServiceOrders.set(true);
        this.readyForPickupServiceOrders.set([]);
        const pagination = this.readyForPickupServiceOrdersPagination();

        this._service.getReadyForPickupServiceOrders({pageNumber: pagination.pageNumber, pageSize: pagination.pageSize})
        .pipe(
            finalize(() => this.isLoadingReadyForPickupServiceOrders.set(false))
        )
        .subscribe({
            next: result => {
                const data = result.data!;
                this.readyForPickupServiceOrders.set(data.items);
                this.readyForPickupServiceOrdersPagination.set(this.createPaginationWithParams(data.pageNumber, data.pageSize, data.totalCount));
            },
            error: (error: HttpErrorResponse) => {
                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Erro ao carregar ordens de serviço.');

                this._snack.open(message);
            }
        })
    }

    protected onPendingEstimatePageChange(event: PageEvent): void {
        this.pendingEstimateServiceOrdersPagination.update(current => ({
            ...current,
            pageNumber: event.pageIndex + 1,
            pageSize: event.pageSize
        }));

        this.loadPendingEstimateServiceOrders();
    }

    protected onAwaitingResponsePageChange(event: PageEvent): void {
        this.awaitingResponseServiceOrdersPagination.update(current => ({
            ...current,
            pageNumber: event.pageIndex + 1,
            pageSize: event.pageSize
        }));

        this.loadAwaitingResponseServiceOrders();
    }

    protected onApprovedPageChange(event: PageEvent): void {
        this.approvedServiceOrdersPagination.update(current => ({
            ...current,
            pageNumber: event.pageIndex + 1,
            pageSize: event.pageSize
        }));

        this.loadApprovedServiceOrders();
    }

    protected onRejectedPageChange(event: PageEvent): void {
        this.rejectedServiceOrdersPagination.update(current => ({
            ...current,
            pageNumber: event.pageIndex + 1,
            pageSize: event.pageSize
        }));

        this.loadRejectedServiceOrders();
    }

    protected onAwaitingPartPageChange(event: PageEvent): void {
        this.awaitingPartServiceOrdersPagination.update(current => ({
            ...current,
            pageNumber: event.pageIndex + 1,
            pageSize: event.pageSize
        }));

        this.loadAwaitingPartServiceOrders();
    }

    protected onReadyForPickupPageChange(event: PageEvent): void {
        this.readyForPickupServiceOrdersPagination.update(current => ({
            ...current,
            pageNumber: event.pageIndex + 1,
            pageSize: event.pageSize
        }));

        this.loadReadyForPickupServiceOrders();
    }

    protected onTabChange(changeEvent: MatTabChangeEvent) {
        this._selectedTab = changeEvent.index;
        this.loadTab(changeEvent.index);
    }

    private loadTab(index: number) {
        switch (index) {
            case 0:
                this.loadPendingEstimateServiceOrders();
                break;
            case 1:
                this.loadAwaitingResponseServiceOrders();
                break;
            case 2:
                this.loadApprovedServiceOrders();
                break;
            case 3:
                this.loadRejectedServiceOrders();
                break;
            case 4:
                this.loadAwaitingPartServiceOrders();
                break;
            case 5:
                this.loadReadyForPickupServiceOrders();
                break;
        }
    }

    private createPagination(): TablePagination {
        return {
            pageNumber: 1,
            pageSize: 25,
            totalCount: 0
        };
    }

    private createPaginationWithParams(pageNumber: number, pageSize: number, totalCount: number): TablePagination {
        return {
            pageNumber: pageNumber,
            pageSize: pageSize,
            totalCount: totalCount
        };
    }
}