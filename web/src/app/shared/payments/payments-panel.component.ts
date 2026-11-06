import { Component, computed, effect, inject, input, signal } from "@angular/core";
import { CurrencyPipe, DatePipe, NgTemplateOutlet } from "@angular/common";
import { HttpErrorResponse } from "@angular/common/http";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { finalize, Observable } from "rxjs";
import { SnackbarService } from "../snackbar/snackbar.service";
import { isPdfResponse, openPdfWindow, readBlobErrorMessage, showPdf } from "../pdf/pdf-print";
import { BaseResponse } from "../models/base-response.interface";
import { Payment, PaymentsService } from "./payments.service";

function today(): string {
    const now = new Date();
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

@Component({
    selector: 'app-payments-panel',
    templateUrl: './payments-panel.component.html',
    styleUrl: './payments-panel.component.scss',
    imports: [ReactiveFormsModule, CurrencyPipe, DatePipe, NgTemplateOutlet, MatButtonModule, MatProgressSpinnerModule],
})
export class PaymentsPanelComponent {
    private _service = inject(PaymentsService);
    private _snackbar = inject(SnackbarService);
    private _fb = inject(FormBuilder);

    readonly serviceOrderId = input.required<number>();
    readonly totalAmount = input<number>(0);

    protected payments = signal<Payment[]>([]);
    protected isLoading = signal(true);
    protected isSaving = signal(false);
    protected editingId = signal<number | 'new' | null>(null);
    protected confirmingRemoveId = signal<number | null>(null);
    protected printingId = signal<number | null>(null);

    protected form = this._fb.nonNullable.group({
        amount: this._fb.control<number | null>(null, [Validators.required, Validators.min(0.01)]),
        paymentDate: this._fb.nonNullable.control(today(), Validators.required),
    });

    protected sorted = computed(() =>
        [...this.payments()].sort((a, b) => b.paidAt.localeCompare(a.paidAt) || b.id - a.id));
    protected totalPaid = computed(() => this.payments().reduce((sum, p) => sum + p.amount, 0));
    protected balance = computed(() => Math.round((this.totalAmount() - this.totalPaid()) * 100) / 100);

    constructor() {
        effect(() => this.load(this.serviceOrderId()));
    }

    protected startAdd() {
        this.form.reset({ amount: null, paymentDate: today() });
        this.confirmingRemoveId.set(null);
        this.editingId.set('new');
    }

    protected startEdit(payment: Payment) {
        this.form.reset({ amount: payment.amount, paymentDate: payment.paidAt.slice(0, 10) });
        this.confirmingRemoveId.set(null);
        this.editingId.set(payment.id);
    }

    protected cancelEdit() {
        this.editingId.set(null);
    }

    protected save() {
        const editing = this.editingId();
        if (editing === null || this.form.invalid || this.isSaving()) {
            this.form.markAllAsTouched();
            return;
        }

        const value = this.form.getRawValue();
        const body = { amount: Number(value.amount), paymentDate: value.paymentDate };
        const request = editing === 'new'
            ? this._service.add(this.serviceOrderId(), body)
            : this._service.update(this.serviceOrderId(), editing, body);

        this.run(request, () => this.editingId.set(null));
    }

    protected askRemove(payment: Payment) {
        this.editingId.set(null);
        this.confirmingRemoveId.set(payment.id);
    }

    protected remove(payment: Payment) {
        this.run(this._service.remove(this.serviceOrderId(), payment.id), () => this.confirmingRemoveId.set(null));
    }

    protected printReceipt(payment: Payment) {
        if (this.printingId() !== null) {
            return;
        }

        const pdfWindow = openPdfWindow();
        this.printingId.set(payment.id);
        this._service.receipt(this.serviceOrderId(), payment.id)
            .pipe(finalize(() => this.printingId.set(null)))
            .subscribe({
                next: response => {
                    if (isPdfResponse(response)) {
                        showPdf(response.body!, pdfWindow, `comprovante_pagamento_${payment.id}.pdf`, `Comprovante de pagamento ${payment.id}`);
                    } else {
                        pdfWindow?.close();
                    }
                },
                error: async (error: HttpErrorResponse) => {
                    pdfWindow?.close();
                    this._snackbar.error(await readBlobErrorMessage(error, 'Não foi possível gerar o comprovante.'));
                },
            });
    }

    private run(request: Observable<BaseResponse<string>>, onSuccess: () => void) {
        this.isSaving.set(true);
        request.pipe(finalize(() => this.isSaving.set(false))).subscribe({
            next: response => {
                this._snackbar.success(response.message ?? 'Pagamento salvo.');
                onSuccess();
                this.load(this.serviceOrderId());
            },
            error: (error: HttpErrorResponse) =>
                this._snackbar.error(error.error?.message ?? 'Não foi possível salvar o pagamento. Tente de novo.'),
        });
    }

    private load(serviceOrderId: number) {
        this.isLoading.set(true);
        this._service.list(serviceOrderId)
            .pipe(finalize(() => this.isLoading.set(false)))
            .subscribe({
                next: response => this.payments.set(response.data ?? []),
                error: (error: HttpErrorResponse) =>
                    this._snackbar.error(error.error?.message ?? 'Não foi possível carregar os pagamentos.'),
            });
    }
}
