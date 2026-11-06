import { Component, DestroyRef, inject, signal } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from "@angular/forms";
import { HttpErrorResponse } from "@angular/common/http";
import { DatePipe } from "@angular/common";
import { MatDialog } from "@angular/material/dialog";
import { MatButtonModule } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatTableModule } from "@angular/material/table";
import { MatPaginator, PageEvent } from "@angular/material/paginator";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { finalize } from "rxjs";
import { BaseLayoutComponent } from "../../shared/layout/base-layout/base-layout.component";
import { SnackbarService } from "../../shared/snackbar/snackbar.service";
import { EnumLabelPipe } from "../../shared/enums/enum-label.pipe";
import { SearchServiceOrdersService } from "./search-service-orders.service";
import { ServiceOrderSearchItem, ServiceOrderSearchParams } from "./models/service-order-search.model";
import { ServiceOrder } from "../../shared/models/service-order.interface";
import { ServiceOrderDetailsDialogComponent } from "./service-order-details-dialog/service-order-details-dialog.component";

const PAGE_SIZE = 20;

function minLettersOrEmpty(control: AbstractControl<string>): ValidationErrors | null {
    const value = (control.value ?? '').trim();
    return value.length > 0 && value.length < 3 ? { minLetters: true } : null;
}

function notBeforeFrom(control: AbstractControl<string>): ValidationErrors | null {
    const from = control.parent?.get('from')?.value as string | undefined;
    return from && control.value && control.value < from ? { beforeFrom: true } : null;
}

const FILTER_GROUPS = [['number'], ['customer'], ['model'], ['serialNumber'], ['from', 'to']] as const;

@Component({
    selector: 'app-search-service-orders',
    templateUrl: './search-service-orders.component.html',
    styleUrl: './search-service-orders.component.scss',
    imports: [
        BaseLayoutComponent,
        ReactiveFormsModule,
        DatePipe,
        EnumLabelPipe,
        MatButtonModule,
        MatFormFieldModule,
        MatInputModule,
        MatTableModule,
        MatPaginator,
        MatProgressSpinnerModule,
    ],
})
export class SearchServiceOrdersComponent {
    private _service = inject(SearchServiceOrdersService);
    private _dialog = inject(MatDialog);
    private _snackbar = inject(SnackbarService);
    private _fb = inject(FormBuilder);

    protected readonly columns = ['number', 'customer', 'device', 'serialNumber', 'enterprise', 'status', 'enteredAt'];

    protected filters = this._fb.group({
        number: this._fb.control<number | null>(null, Validators.min(1)),
        customer: this._fb.nonNullable.control('', minLettersOrEmpty),
        model: this._fb.nonNullable.control(''),
        serialNumber: this._fb.nonNullable.control(''),
        from: this._fb.nonNullable.control(''),
        to: this._fb.nonNullable.control('', notBeforeFrom),
    });

    protected results = signal<ServiceOrderSearchItem[]>([]);
    protected totalCount = signal(0);
    protected pageIndex = signal(0);
    protected isLoading = signal(false);
    protected hasSearched = signal(false);
    protected isFiltered = signal(false);

    private _lastParams: Omit<ServiceOrderSearchParams, 'pageNumber' | 'pageSize'> | null = null;

    constructor() {
        const destroyRef = inject(DestroyRef);

        this._lastParams = {};
        this.load(0);

        for (const group of FILTER_GROUPS) {
            for (const name of group) {
                this.filters.get(name)!.valueChanges
                    .pipe(takeUntilDestroyed(destroyRef))
                    .subscribe(value => {
                        if (value !== null && value !== '') {
                            this.clearOtherGroups(group);
                        }
                        if (name === 'from') {
                            this.filters.controls.to.updateValueAndValidity({ emitEvent: false });
                        }
                    });
            }
        }
    }

    protected hasFilter(): boolean {
        const v = this.filters.getRawValue();
        return v.number !== null || [v.customer, v.model, v.serialNumber, v.from, v.to].some(x => x.trim() !== '');
    }

    protected search() {
        if (this.filters.invalid || this.isLoading()) {
            this.filters.markAllAsTouched();
            return;
        }

        if (!this.hasFilter()) {
            this._lastParams = {};
            this.load(0);
            return;
        }

        const v = this.filters.getRawValue();
        this._lastParams = {
            number: v.number,
            customer: v.customer.trim(),
            model: v.model.trim(),
            serialNumber: v.serialNumber.trim(),
            from: v.from,
            to: v.to,
        };
        this.load(0);
    }

    protected changePage(event: PageEvent) {
        this.load(event.pageIndex);
    }

    protected clear() {
        this.filters.reset({ number: null, customer: '', model: '', serialNumber: '', from: '', to: '' }, { emitEvent: false });
        this._lastParams = {};
        this.load(0);
    }

    protected openDetails(item: ServiceOrderSearchItem) {
        this._dialog.open(ServiceOrderDetailsDialogComponent, {
            data: { id: item.number },
            width: '100vw',
            height: '100vh',
            maxWidth: '100vw',
            maxHeight: '100vh',
            panelClass: 'fullscreen-dialog',
            autoFocus: 'dialog',
        }).afterClosed().subscribe((changed?: ServiceOrder) => {
            if (changed) {
                this.results.update(rows => rows.map(row => row.number !== item.number ? row : {
                    ...row,
                    customer: changed.name,
                    productType: changed.type,
                    brand: changed.brand,
                    model: changed.model,
                    serialNumber: changed.serialNumber,
                    enterprise: changed.enterprise,
                    enteredAt: changed.entryDate,
                }));
            }
        });
    }

    private load(pageIndex: number) {
        if (!this._lastParams) {
            return;
        }

        const filtered = Object.values(this._lastParams).some(x => x !== null && x !== undefined && x !== '');
        this.isLoading.set(true);
        this._service.search({ ...this._lastParams, pageNumber: pageIndex + 1, pageSize: PAGE_SIZE })
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe({
                next: response => {
                    this.results.set(response.data?.items ?? []);
                    this.totalCount.set(response.data?.totalCount ?? 0);
                    this.pageIndex.set(pageIndex);
                    this.hasSearched.set(true);
                    this.isFiltered.set(filtered);
                },
                error: (error: HttpErrorResponse) => {
                    this._snackbar.error(error.error?.message ?? 'Não foi possível buscar. Tente de novo.');
                },
            });
    }

    private clearOtherGroups(keep: readonly string[]) {
        for (const group of FILTER_GROUPS) {
            if (group === keep) {
                continue;
            }
            for (const name of group) {
                const control = this.filters.get(name)!;
                if (control.value !== null && control.value !== '') {
                    control.setValue(name === 'number' ? null : '', { emitEvent: false });
                    control.markAsUntouched();
                }
            }
        }
        this.filters.controls.to.updateValueAndValidity({ emitEvent: false });
    }
}
