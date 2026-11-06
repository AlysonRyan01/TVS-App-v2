import { Component, DestroyRef, inject, signal } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors } from "@angular/forms";
import { HttpErrorResponse } from "@angular/common/http";
import { MatDialog } from "@angular/material/dialog";
import { MatButtonModule } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatTableModule } from "@angular/material/table";
import { MatPaginator, PageEvent } from "@angular/material/paginator";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { EMPTY, Subject, catchError, finalize, switchMap } from "rxjs";
import { BaseLayoutComponent } from "../../../shared/layout/base-layout/base-layout.component";
import { SnackbarService } from "../../../shared/snackbar/snackbar.service";
import { StackedTableDirective } from "../../../shared/stacked-table/stacked-table.directive";
import { ServiceOrderHubService } from "../../../shared/realtime/service-order-hub.service";
import { CustomerDialogComponent, CustomerDialogData } from "../customer-dialog/customer-dialog.component";
import { CustomersService } from "../customers.service";
import { Customer } from "../models/customer.model";
import { CustomerSearchParams } from "../models/customer-search.model";
import { formatDocument, normalizeDocument } from "../models/document";

const PAGE_SIZE = 20;

type Filters = Omit<CustomerSearchParams, 'pageNumber' | 'pageSize'>;

function minLength(length: number, normalize: (value: string) => string = v => v.trim()) {
    return (control: AbstractControl<string>): ValidationErrors | null => {
        const value = normalize(control.value ?? '');
        return value.length > 0 && value.length < length ? { minLength: true } : null;
    };
}

const digitsOnly = (value: string) => value.replace(/\D/g, '');

@Component({
    selector: 'app-customers-page',
    templateUrl: './customers-page.component.html',
    styleUrl: './customers-page.component.scss',
    imports: [
        StackedTableDirective,
        BaseLayoutComponent,
        ReactiveFormsModule,
        MatButtonModule,
        MatFormFieldModule,
        MatInputModule,
        MatTableModule,
        MatPaginator,
        MatProgressSpinnerModule,
    ],
})
export class CustomersPageComponent {
    private _service = inject(CustomersService);
    private _dialog = inject(MatDialog);
    private _snackbar = inject(SnackbarService);
    private _hub = inject(ServiceOrderHubService);
    private _fb = inject(FormBuilder);

    protected readonly columns = ['name', 'phone', 'document', 'city', 'email'];
    protected readonly formatDocument = formatDocument;

    protected filters = this._fb.nonNullable.group({
        name: ['', minLength(3)],
        phone: ['', minLength(4, digitsOnly)],
        document: ['', minLength(3, normalizeDocument)],
    });

    protected results = signal<Customer[]>([]);
    protected totalCount = signal(0);
    protected pageIndex = signal(0);
    protected isLoading = signal(false);
    protected hasSearched = signal(false);
    protected isFiltered = signal(false);

    private _current: Filters = {};
    private readonly _requests = new Subject<{ filters: Filters; pageIndex: number }>();

    constructor() {
        const destroyRef = inject(DestroyRef);

        // A última pesquisa sempre vence (switchMap cancela a anterior).
        this._requests.pipe(
            switchMap(({ filters, pageIndex }) => {
                this.isLoading.set(true);
                return this._service
                    .searchPaged({ ...filters, pageNumber: pageIndex + 1, pageSize: PAGE_SIZE })
                    .pipe(
                        // Erro numa pesquisa não encerra a tela: avisa e mantém a grade atual.
                        catchError((error: HttpErrorResponse) => {
                            this._snackbar.error(error.error?.message ?? 'Não foi possível buscar os clientes. Tente de novo.');
                            return EMPTY;
                        }),
                        finalize(() => this.isLoading.set(false)),
                    );
            }),
            takeUntilDestroyed(destroyRef),
        ).subscribe({
            next: response => {
                const page = response.data;
                this.results.set(page?.items ?? []);
                this.totalCount.set(page?.totalCount ?? 0);
                this.pageIndex.set(Math.max(0, (page?.pageNumber ?? 1) - 1));
                this.hasSearched.set(true);
                this.isFiltered.set(Object.values(this._current).some(v => !!v));
            },
        });

        // Outro atendente cadastrou ou alterou algo → recarrega a página atual.
        this._hub.start();
        this._hub.updates
            .pipe(takeUntilDestroyed(destroyRef))
            .subscribe(() => this.reload());

        this.load(0);
    }

    protected search() {
        if (this.filters.invalid || this.isLoading()) {
            this.filters.markAllAsTouched();
            return;
        }
        const v = this.filters.getRawValue();
        this._current = {
            name: v.name.trim() || undefined,
            phone: digitsOnly(v.phone) || undefined,
            document: normalizeDocument(v.document) || undefined,
        };
        this.load(0);
    }

    protected clear() {
        this.filters.reset({ name: '', phone: '', document: '' });
        this._current = {};
        this.load(0);
    }

    protected changePage(event: PageEvent) {
        this.load(event.pageIndex);
    }

    protected edit(customer: Customer) {
        const data: CustomerDialogData = { customer, mode: 'edit' };
        this._dialog.open(CustomerDialogComponent, { data, width: '44rem', maxWidth: '95vw', autoFocus: 'first-tabbable' })
            .afterClosed()
            .subscribe((updated?: Customer) => {
                if (updated) {
                    this.reload();
                }
            });
    }

    private reload() {
        this.load(this.pageIndex());
    }

    private load(pageIndex: number) {
        this._requests.next({ filters: this._current, pageIndex });
    }
}
