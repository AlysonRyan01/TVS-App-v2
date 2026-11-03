import { Component, inject, OnInit, signal } from "@angular/core";
import { BaseLayoutComponent } from "../../shared/layout/base-layout/base-layout.component";
import { MatTabsModule } from "@angular/material/tabs";
import { MatTableModule } from "@angular/material/table";
import { ApprovedServiceOrder } from "./models/approved-service-orders.interface";
import { PendingEstimateServiceOrder } from "./models/pending-estimate-service-ordens.interface";
import { ReadyServiceOrder } from "./models/ready-service-orders.interface";
import { RefusedServiceOrder } from "./models/refused-service-orders.interface";
import { WaitingResponseServiceOrder } from "./models/waiting-response-service-orders.interface";
import { WaitingPartServiceOrder } from "./models/waiting-part-service-orders.interface";
import { HomeService } from "./home.service";
import { TablePagination } from "./models/table-pagination.interface";
import { DatePipe } from '@angular/common';
import { MatPaginator } from "@angular/material/paginator";
import { PageEvent } from '@angular/material/paginator';
import { MatTabChangeEvent } from "@angular/material/tabs";
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { finalize } from "rxjs";
import { HttpErrorResponse } from "@angular/common/http";
import { SnackbarService } from "../../shared/snackbar/snackbar.service";
import { CurrencyPipe } from "@angular/common";
import { EnumLabelPipe } from "../../shared/enums";

@Component({
    selector: 'app-home',
    styleUrl: 'home.component.scss',
    templateUrl: 'home.component.html',
    imports: [BaseLayoutComponent, MatTabsModule, MatTableModule, DatePipe, MatPaginator, MatProgressSpinner, CurrencyPipe, EnumLabelPipe],
})
export class HomeComponent implements OnInit {
    private _service = inject(HomeService);
    private _snack = inject(SnackbarService);

    protected isLoadingPendingEstimateServiceOrders = signal<boolean>(false);
    protected isLoadingWaitingResponseServiceOrders = signal<boolean>(false);
    protected isLoadingApprovedServiceOrders = signal<boolean>(false);
    protected isLoadingReprovedServiceOrders = signal<boolean>(false);
    protected isLoadingWaitingPartServiceOrders = signal<boolean>(false);
    protected isLoadingReadyServiceOrders = signal<boolean>(false);

    protected pendingEstimateServiceOrders = signal<PendingEstimateServiceOrder[]>([]);
    protected waitingResponseServiceOrders = signal<WaitingResponseServiceOrder[]>([]);
    protected approvedServiceOrders = signal<ApprovedServiceOrder[]>([]);
    protected refusedServiceOrders = signal<RefusedServiceOrder[]>([]);
    protected waitingPartServiceOrders = signal<WaitingPartServiceOrder[]>([]);
    protected readyServiceOrders = signal<ReadyServiceOrder[]>([]);

    protected pendingServiceOrdersPagination = signal<TablePagination>(this.createPagination());
    protected waitingResponseServiceOrdersPagination = signal<TablePagination>(this.createPagination());
    protected approvedServiceOrdersPagination = signal<TablePagination>(this.createPagination());
    protected refusedServiceOrdersPagination = signal<TablePagination>(this.createPagination());
    protected waitingPartServiceOrdersPagination = signal<TablePagination>(this.createPagination());
    protected readyServiceOrdersPagination = signal<TablePagination>(this.createPagination());

    protected pendingColumns = ['number','customer','model','entryDate'];
    protected waitingResponseColumns = ['number','customer','model','solution','amount','inspectionIn'];
    protected approvedColumns = ['number', 'company', 'customer', 'model', 'solution', 'approvedIn'];
    protected refusedColumns = ['number', 'customer', 'model', 'solution', 'refusedIn'];
    protected waitingPartColumns = ['number', 'customer', 'model', 'solution', 'purchasedIn'];
    protected readyColumns = ['number', 'customer', 'model', 'repairDate', 'situation', 'response'];


    ngOnInit(): void {
        this.loadPendingServiceOrders();
    }

    private loadPendingServiceOrders() {
        this.isLoadingPendingEstimateServiceOrders.set(true);
        this.pendingEstimateServiceOrders.set([]);

        const pagination = this.pendingServiceOrdersPagination();

        this._service.getPendingEstimateServiceOrders({pageNumber: pagination.pageNumber, pageSize: pagination.pageSize})
        .pipe(
            finalize(() => this.isLoadingPendingEstimateServiceOrders.set(false))
        )
        .subscribe({
            next: result => {
            const data = result.data!;
            this.pendingEstimateServiceOrders.set(data.items);
            this.pendingServiceOrdersPagination.set(this.createPaginationWithParams(data.pageNumber, result.data!.pageSize, result.data!.totalCount));
        },
            error: (error: HttpErrorResponse) => {
                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Erro ao carregar ordens de serviço.');

                this._snack.open(message);
            }
        })
    }

    private loadWaitingResponseServiceOrders() {
        this.isLoadingWaitingResponseServiceOrders.set(true);
        this.waitingResponseServiceOrders.set([]);
        const pagination = this.waitingResponseServiceOrdersPagination();

        this._service.getWaitingResponseServiceOrders({pageNumber: pagination.pageNumber, pageSize: pagination.pageSize})
        .pipe(
            finalize(() => this.isLoadingWaitingResponseServiceOrders.set(false))
        )
        .subscribe({
            next: result => {
                const data = result.data!;
                this.waitingResponseServiceOrders.set(data.items);
                this.waitingResponseServiceOrdersPagination.set(this.createPaginationWithParams(data.pageNumber, result.data!.pageSize, result.data!.totalCount));
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
                this.approvedServiceOrdersPagination.set(this.createPaginationWithParams(data.pageNumber, result.data!.pageSize, result.data!.totalCount));
            },
            error: (error: HttpErrorResponse) => {
                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Erro ao carregar ordens de serviço.');

                this._snack.open(message);
            }
        })
    }

    private loadRefusedServiceOrders() {
        this.isLoadingReprovedServiceOrders.set(true);
        this.refusedServiceOrders.set([]);
        const pagination = this.refusedServiceOrdersPagination();

        this._service.getRefusedServiceOrders({pageNumber: pagination.pageNumber, pageSize: pagination.pageSize})
        .pipe(
            finalize(() => this.isLoadingReprovedServiceOrders.set(false))
        )
        .subscribe({
            next: result => {
                const data = result.data!;
                this.refusedServiceOrders.set(data.items);
                this.refusedServiceOrdersPagination.set(this.createPaginationWithParams(data.pageNumber, result.data!.pageSize, result.data!.totalCount));
            },
            error: (error: HttpErrorResponse) => {
                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Erro ao carregar ordens de serviço.');

                this._snack.open(message);
            }
        })
    }

    private loadWaitingPartServiceOrders() {
        this.isLoadingWaitingPartServiceOrders.set(true);
        this.waitingPartServiceOrders.set([]);
        const pagination = this.waitingPartServiceOrdersPagination();

        this._service.getWaitingPartServiceOrders({pageNumber: pagination.pageNumber, pageSize: pagination.pageSize})
        .pipe(
            finalize(() => this.isLoadingWaitingPartServiceOrders.set(false))
        )
        .subscribe({
            next: result => {
                const data = result.data!;
                this.waitingPartServiceOrders.set(data.items);
                this.waitingPartServiceOrdersPagination.set(this.createPaginationWithParams(data.pageNumber, result.data!.pageSize, result.data!.totalCount));
            },
            error: (error: HttpErrorResponse) => {
                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Erro ao carregar ordens de serviço.');

                this._snack.open(message);
            }
        })
    }

    private loadReadyServiceOrders() {
        this.isLoadingReadyServiceOrders.set(true);
        this.readyServiceOrders.set([]);
        const pagination = this.readyServiceOrdersPagination();

        this._service.getReadyServiceOrders({pageNumber: pagination.pageNumber, pageSize: pagination.pageSize})
        .pipe(
            finalize(() => this.isLoadingReadyServiceOrders.set(false))
        )
        .subscribe({
            next: result => {
                const data = result.data!;
                this.readyServiceOrders.set(data.items);
                this.readyServiceOrdersPagination.set(this.createPaginationWithParams(data.pageNumber, result.data!.pageSize, result.data!.totalCount));
            },
            error: (error: HttpErrorResponse) => {
                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Erro ao carregar ordens de serviço.');

                this._snack.open(message);
            }
        })
    }

    protected onPendingPageChange(event: PageEvent): void {
        this.pendingServiceOrdersPagination.update(current => ({
            ...current,
            pageNumber: event.pageIndex + 1,
            pageSize: event.pageSize
        }));

        this.loadPendingServiceOrders();
    }

    protected onWaitingResponsePageChange(event: PageEvent): void {
        this.waitingResponseServiceOrdersPagination.update(current => ({
            ...current,
            pageNumber: event.pageIndex + 1,
            pageSize: event.pageSize
        }));

        this.loadWaitingResponseServiceOrders();
    }

    protected onApprovedPageChange(event: PageEvent): void {
        this.approvedServiceOrdersPagination.update(current => ({
            ...current,
            pageNumber: event.pageIndex + 1,
            pageSize: event.pageSize
        }));

        this.loadApprovedServiceOrders();
    }

    protected onRefusedPageChange(event: PageEvent): void {
        this.refusedServiceOrdersPagination.update(current => ({
            ...current,
            pageNumber: event.pageIndex + 1,
            pageSize: event.pageSize
        }));

        this.loadRefusedServiceOrders();
    }

    protected onWaitingPartPageChange(event: PageEvent): void {
        this.waitingPartServiceOrdersPagination.update(current => ({
            ...current,
            pageNumber: event.pageIndex + 1,
            pageSize: event.pageSize
        }));

        this.loadWaitingPartServiceOrders();
    }

    protected onReadyPageChange(event: PageEvent): void {
        this.readyServiceOrdersPagination.update(current => ({
            ...current,
            pageNumber: event.pageIndex + 1,
            pageSize: event.pageSize
        }));

        this.loadReadyServiceOrders();
    }

    protected myTabSelectedTabChange(changeEvent: MatTabChangeEvent) {
        switch (changeEvent.index) {
            case 0:
                this.loadPendingServiceOrders();
                break;
            case 1:
                this.loadWaitingResponseServiceOrders();
                break;
            case 2:
                this.loadApprovedServiceOrders();
                break;
            case 3:
                this.loadRefusedServiceOrders();
                break;
            case 4:
                this.loadWaitingPartServiceOrders();
                break;
            case 5:
                this.loadReadyServiceOrders();
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