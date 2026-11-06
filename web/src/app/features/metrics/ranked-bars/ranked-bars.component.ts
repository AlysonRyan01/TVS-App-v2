import { Component, computed, input } from "@angular/core";

interface BarRow {
    label: string;
    value: number;
    share: number;
    /** Largura relativa ao maior valor (0–100). */
    width: number;
}

/**
 * Partes de um todo comparadas entre si (ex.: ordens por situação): barras horizontais ordenadas,
 * maior primeiro, numa cor só (guia de dataviz: barras na mesma base comparam melhor que fatias de
 * rosca). Cada linha traz nome, quantidade e %, então a lista já é a "tabela" do gráfico.
 */
@Component({
    selector: 'app-ranked-bars',
    templateUrl: './ranked-bars.component.html',
    styleUrl: './ranked-bars.component.scss',
})
export class RankedBarsComponent {
    readonly title = input.required<string>();
    readonly labels = input<string[]>([]);
    readonly values = input<number[]>([]);

    protected total = computed(() => this.values().reduce((sum, v) => sum + (v || 0), 0));

    protected rows = computed<BarRow[]>(() => {
        const total = this.total();
        const max = Math.max(0, ...this.values());
        return this.labels()
            .map((label, i) => ({ label, value: this.values()[i] ?? 0 }))
            .filter(row => row.value > 0)
            .sort((a, b) => b.value - a.value)
            .map(row => ({
                ...row,
                share: total ? Math.round((row.value / total) * 100) : 0,
                width: max ? (row.value / max) * 100 : 0,
            }));
    });
}
