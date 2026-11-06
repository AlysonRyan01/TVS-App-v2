import { Component, DestroyRef, inject, input, OnInit, output, signal } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { HttpErrorResponse } from "@angular/common/http";
import { MatButtonModule } from "@angular/material/button";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { debounceTime, distinctUntilChanged, finalize, Subject, switchMap, of, catchError } from "rxjs";
import { CustomersService } from "../customers.service";
import { Customer } from "../models/customer.model";
import { maskPhone } from "../../../shared/masks/masks";

const VISIBLE_RESULTS = 20;
const MIN_LETTERS = 3;

@Component({
    selector: 'app-customer-search',
    templateUrl: './customer-search.component.html',
    styleUrl: './customer-search.component.scss',
    imports: [MatButtonModule, MatProgressSpinnerModule],
})
export class CustomerSearchComponent implements OnInit {
    private _service = inject(CustomersService);
    private _destroyRef = inject(DestroyRef);
    private _terms = new Subject<string>();

    selected = output<Customer>();
    createNew = output<string>();
    allowCreate = input(true);

    protected term = signal('');
    protected results = signal<Customer[]>([]);
    protected hasMore = signal(false);
    protected isSearching = signal(false);
    protected searched = signal(false);
    protected errorMessage = signal<string | null>(null);

    protected readonly minLetters = MIN_LETTERS;
    protected readonly maskPhone = maskPhone;

    ngOnInit(): void {
        this._terms.pipe(
            debounceTime(300),
            distinctUntilChanged(),
            switchMap(term => {
                if (term.trim().length < MIN_LETTERS) {
                    this.searched.set(false);
                    this.results.set([]);
                    return of(null);
                }

                this.isSearching.set(true);
                this.errorMessage.set(null);
                return this._service.searchCustomers(term.trim()).pipe(
                    catchError((error: HttpErrorResponse) => {
                        this.errorMessage.set(error.error?.message ?? 'Não foi possível buscar os clientes.');
                        return of(null);
                    }),
                    finalize(() => this.isSearching.set(false)),
                );
            }),
            takeUntilDestroyed(this._destroyRef),
        ).subscribe(result => {
            if (!result) {
                return;
            }

            const customers = result.data ?? [];
            this.results.set(customers.slice(0, VISIBLE_RESULTS));
            this.hasMore.set(customers.length > VISIBLE_RESULTS);
            this.searched.set(true);
        });
    }

    protected onInput(value: string) {
        this.term.set(value);
        this._terms.next(value);
    }

    clear() {
        this.term.set('');
        this.results.set([]);
        this.hasMore.set(false);
        this.searched.set(false);
        this.errorMessage.set(null);
        this._terms.next('');
    }
}
