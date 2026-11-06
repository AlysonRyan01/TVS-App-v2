import { Component, computed, inject, signal } from "@angular/core";
import { CurrencyPipe, DatePipe, DecimalPipe } from "@angular/common";
import { HttpErrorResponse } from "@angular/common/http";
import { FormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatSelectModule } from "@angular/material/select";
import { MatTableModule } from "@angular/material/table";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { finalize } from "rxjs";
import { BaseLayoutComponent } from "../../shared/layout/base-layout/base-layout.component";
import { SnackbarService } from "../../shared/snackbar/snackbar.service";
import { MetricsService } from "./metrics.service";
import { DashboardMetrics, DeliveredProduct, MetricsPeriod } from "./models/dashboard-metrics.model";
import { RankedBarsComponent } from "./ranked-bars/ranked-bars.component";

const MONTHS = ['Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho', 'Julho', 'Agosto',
    'Setembro', 'Outubro', 'Novembro', 'Dezembro'];

function brDate(value: string): string {
    const [y, m, d] = value.split('-');
    return `${d}/${m}/${y}`;
}

@Component({
    selector: 'app-metrics',
    templateUrl: './metrics.component.html',
    styleUrl: './metrics.component.scss',
    imports: [
        BaseLayoutComponent,
        FormsModule,
        CurrencyPipe,
        DatePipe,
        DecimalPipe,
        MatButtonModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        MatTableModule,
        MatProgressSpinnerModule,
        RankedBarsComponent,
    ],
})
export class MetricsComponent {
    private _service = inject(MetricsService);
    private _snackbar = inject(SnackbarService);

    protected readonly months = MONTHS.map((label, i) => ({ value: i + 1, label }));
    protected readonly years = Array.from({ length: 6 }, (_, i) => new Date().getFullYear() - i);
    protected readonly columns = ['id', 'customer', 'product', 'type', 'status', 'enterprise', 'deliveredAt', 'total', 'partCost'];

    protected month = signal(new Date().getMonth() + 1);
    protected year = signal(new Date().getFullYear());
    protected from = signal('');
    protected to = signal('');

    protected metrics = signal<DashboardMetrics | null>(null);
    protected isLoading = signal(false);
    protected subtitle = signal('');
    protected savingIds = signal<ReadonlySet<number>>(new Set());

    protected rangeError = computed(() => {
        const from = this.from();
        const to = this.to();
        if (!from && !to) {
            return false;
        }
        return !from || !to || to < from;
    });

    constructor() {
        this.consult();
    }

    protected onMonthOrYearChange() {
        this.from.set('');
        this.to.set('');
        this.consult();
    }

    protected consult() {
        if (this.rangeError() || this.isLoading()) {
            return;
        }

        const useRange = !!this.from() && !!this.to();
        const period: MetricsPeriod = useRange
            ? { from: this.from(), to: this.to() }
            : { month: this.month(), year: this.year() };
        const subtitle = useRange
            ? `${brDate(this.from())} a ${brDate(this.to())}`
            : `${MONTHS[this.month() - 1]}/${this.year()}`;

        this.isLoading.set(true);
        this._service.get(period)
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe({
                next: response => {
                    this.metrics.set(response.data ?? null);
                    this.subtitle.set(subtitle);
                },
                error: (error: HttpErrorResponse) =>
                    this._snackbar.error(error.error?.message ?? 'Não foi possível carregar as métricas. Tente de novo.'),
            });
    }

    protected savePartCost(product: DeliveredProduct, input: HTMLInputElement) {
        const raw = input.value.trim();
        const value = raw === '' ? 0 : Number(raw);
        if (Number.isNaN(value) || value < 0) {
            input.classList.add('invalid');
            return;
        }
        input.classList.remove('invalid');

        if (value === product.partPurchaseCost || this.savingIds().has(product.id)) {
            return;
        }

        this.setSaving(product.id, true);
        this._service.updatePartPurchaseCost(product.id, value)
            .pipe(finalize(() => this.setSaving(product.id, false)))
            .subscribe({
                next: () => {
                    this.metrics.update(current => {
                        if (!current) {
                            return current;
                        }
                        const diff = value - product.partPurchaseCost;
                        return {
                            ...current,
                            totalPartsSpend: current.totalPartsSpend + diff,
                            netTotal: current.netTotal - diff,
                            deliveredProducts: current.deliveredProducts.map(p =>
                                p.id === product.id ? { ...p, partPurchaseCost: value } : p),
                        };
                    });
                    this._snackbar.success('Valor da peça salvo.');
                },
                error: (error: HttpErrorResponse) => {
                    input.value = String(product.partPurchaseCost);
                    this._snackbar.error(error.error?.message ?? 'Não foi possível salvar o valor da peça.');
                },
            });
    }

    protected isSaving(id: number): boolean {
        return this.savingIds().has(id);
    }

    private setSaving(id: number, saving: boolean) {
        const next = new Set(this.savingIds());
        if (saving) {
            next.add(id);
        } else {
            next.delete(id);
        }
        this.savingIds.set(next);
    }
}
