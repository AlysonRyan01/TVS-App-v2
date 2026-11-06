import { Component, computed, inject, OnInit, signal } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { CurrencyPipe } from "@angular/common";
import { HttpErrorResponse } from "@angular/common/http";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from "@angular/material/dialog";
import { MatButtonModule } from "@angular/material/button";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatSelectModule } from "@angular/material/select";
import { finalize } from "rxjs";
import { ServiceOrder } from "../../../shared/models/service-order.interface";
import { EnumLabelPipe } from "../../../shared/enums/enum-label.pipe";
import { EEnterprise, EProduct, enumOptions } from "../../../shared/enums";
import { maskPhone } from "../../../shared/masks/masks";
import { SnackbarService } from "../../../shared/snackbar/snackbar.service";
import { isPdfResponse, openPdfWindow, readBlobErrorMessage, showPdf } from "../../../shared/pdf/pdf-print";
import { formatDocument } from "../../customers/models/document";
import { SearchServiceOrdersService } from "../search-service-orders.service";
import { PaymentsPanelComponent } from "../../../shared/payments/payments-panel.component";

function toDateInput(value: string | null | undefined): string {
    return value ? value.slice(0, 10) : '';
}

function toApiDate(original: string | null | undefined, edited: string): string | null {
    if (!edited) {
        return null;
    }
    return original && original.slice(0, 10) === edited ? original : edited;
}

@Component({
    selector: 'app-service-order-details-dialog',
    templateUrl: './service-order-details-dialog.component.html',
    styleUrl: './service-order-details-dialog.component.scss',
    imports: [
        ReactiveFormsModule,
        MatDialogModule,
        MatButtonModule,
        MatProgressSpinnerModule,
        MatFormFieldModule,
        MatInputModule,
        MatSelectModule,
        CurrencyPipe,
        EnumLabelPipe,
        PaymentsPanelComponent,
    ],
})
export class ServiceOrderDetailsDialogComponent implements OnInit {
    private _service = inject(SearchServiceOrdersService);
    private _snackbar = inject(SnackbarService);
    private _dialogRef = inject(MatDialogRef<ServiceOrderDetailsDialogComponent, ServiceOrder | undefined>);
    private _fb = inject(FormBuilder);
    protected readonly data = inject<{ id: number }>(MAT_DIALOG_DATA);

    protected order = signal<ServiceOrder | null>(null);
    protected isLoading = signal(true);
    protected errorMessage = signal<string | null>(null);
    protected editing = signal(false);
    protected isSaving = signal(false);
    protected isPrinting = signal(false);
    protected isSavingLocation = signal(false);
    private _changed = false;

    protected readonly productOptions = enumOptions('product');
    protected readonly enterpriseOptions = enumOptions('enterprise');
    protected readonly statusOptions = enumOptions('serviceOrderStatus');
    protected readonly repairStatusOptions = enumOptions('repairStatus');
    protected readonly repairResultOptions = enumOptions('repairResult');
    protected readonly maskPhone = maskPhone;
    protected readonly formatDocument = formatDocument;

    protected form = this._fb.nonNullable.group({
        type: [EProduct.Tv as number],
        enterprise: [EEnterprise.Particular as number],
        brand: [''],
        model: ['', Validators.required],
        serialNumber: ['', Validators.required],
        location: [''],
        defect: [''],
        accessories: [''],
        solution: [''],
        guarantee: [''],
        partCost: [0, Validators.min(0)],
        laborCost: [0, Validators.min(0)],
        entryDate: ['', Validators.required],
        inspectionDate: [''],
        responseDate: [''],
        purchasePartDate: [''],
        repairDate: [''],
        deliveryDate: [''],
        serviceOrderStatus: [1],
        repairStatus: [1],
        repairResult: this._fb.control<number | null>(null),
        estimateMessage: [''],
        partPurchaseCost: [0, Validators.min(0)],
    });

    private _values = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });
    protected total = computed(() => (Number(this._values().partCost) || 0) + (Number(this._values().laborCost) || 0));
    protected locationChanged = computed(() =>
        !this.editing() && (this._values().location ?? '').trim() !== (this.order()?.location ?? '').trim());

    ngOnInit() {
        this.syncSelects();
        this.load();
        this._dialogRef.disableClose = true;
        this._dialogRef.keydownEvents().subscribe(event => {
            if (event.key === 'Escape') {
                this.onEscape(event);
            }
        });
    }

    protected load() {
        this.isLoading.set(true);
        this.errorMessage.set(null);
        this._service.getById(this.data.id).subscribe({
            next: response => {
                this.setOrder(response.data ?? null);
                this.isLoading.set(false);
            },
            error: (error: HttpErrorResponse) => {
                this.errorMessage.set(error.error?.message ?? `Não foi possível carregar a OS ${this.data.id}.`);
                this.isLoading.set(false);
            },
        });
    }

    protected isSale(order: ServiceOrder): boolean {
        return order.defect === 'VENDA';
    }

    protected address(order: ServiceOrder): string {
        const street = [order.street, order.number].filter(Boolean).join(', ');
        return [street, order.neighborhood, order.city, order.state].filter(Boolean).join(' – ');
    }

    protected close() {
        this._dialogRef.close(this._changed ? this.order() ?? undefined : undefined);
    }

    protected reprint() {
        if (this.isPrinting()) {
            return;
        }

        const pdfWindow = openPdfWindow();
        this.isPrinting.set(true);
        this._service.regeneratePdf(this.data.id)
            .pipe(finalize(() => this.isPrinting.set(false)))
            .subscribe({
                next: response => {
                    if (isPdfResponse(response)) {
                        showPdf(response.body!, pdfWindow, `ordem_servico_${this.data.id}.pdf`, `OS ${this.data.id}`);
                    } else {
                        pdfWindow?.close();
                    }
                },
                error: async (error: HttpErrorResponse) => {
                    pdfWindow?.close();
                    this._snackbar.error(await readBlobErrorMessage(error, 'Não foi possível reimprimir. Tente de novo.'));
                },
            });
    }

    protected saveLocation() {
        const location = this.form.controls.location.value.trim();
        if (this.editing() || !location || !this.locationChanged() || this.isSavingLocation()) {
            return;
        }

        this.isSavingLocation.set(true);
        this._service.updateLocation(this.data.id, location)
            .pipe(finalize(() => this.isSavingLocation.set(false)))
            .subscribe({
                next: () => {
                    const order = this.order()!;
                    this.setOrder({ ...order, location: location.toUpperCase() });
                    this._changed = true;
                    this._snackbar.success('Localização salva.');
                },
                error: (error: HttpErrorResponse) =>
                    this._snackbar.error(error.error?.message ?? 'Não foi possível salvar a localização.'),
            });
    }

    protected startEditing() {
        this.setOrder(this.order());
        this.editing.set(true);
        this.syncSelects();
    }

    protected cancelEditing() {
        this.setOrder(this.order());
        this.editing.set(false);
        this.syncSelects();
    }

    private syncSelects() {
        const c = this.form.controls;
        for (const control of [c.type, c.enterprise, c.serviceOrderStatus, c.repairStatus, c.repairResult]) {
            if (this.editing()) {
                control.enable({ emitEvent: false });
            } else {
                control.disable({ emitEvent: false });
            }
        }
    }

    protected save() {
        const order = this.order();
        if (!order || this.form.invalid || this.isSaving()) {
            this.form.markAllAsTouched();
            return;
        }

        const v = this.form.getRawValue();
        this.isSaving.set(true);
        this._service.update(this.data.id, {
            brand: v.brand,
            model: v.model,
            serialNumber: v.serialNumber,
            defect: v.defect,
            accessories: v.accessories,
            type: v.type,
            location: v.location,
            enterprise: v.enterprise,
            entryDate: toApiDate(order.entryDate, v.entryDate),
            inspectionDate: toApiDate(order.inspectionDate, v.inspectionDate),
            responseDate: toApiDate(order.responseDate, v.responseDate),
            repairDate: toApiDate(order.repairDate, v.repairDate),
            purchasePartDate: toApiDate(order.purchasePartDate, v.purchasePartDate),
            deliveryDate: toApiDate(order.deliveryDate, v.deliveryDate),
            solution: v.solution,
            guarantee: v.guarantee,
            partCost: Number(v.partCost) || 0,
            laborCost: Number(v.laborCost) || 0,
            // Vendas não têm orçamento: os dados de orçamento só vão para OS de conserto.
            estimate: this.isSale(order) ? undefined : {
                serviceOrderStatus: v.serviceOrderStatus,
                repairStatus: v.repairStatus,
                repairResult: v.repairResult,
                estimateMessage: v.estimateMessage.trim() || null,
                partPurchaseCost: Number(v.partPurchaseCost) || 0,
            },
        })
        .pipe(finalize(() => this.isSaving.set(false)))
        .subscribe({
            next: response => {
                this.setOrder(response.data ?? order);
                this._changed = true;
                this.editing.set(false);
                this.syncSelects();
                this._snackbar.success(`OS ${this.data.id} alterada.`);
            },
            error: (error: HttpErrorResponse) =>
                this._snackbar.error(error.error?.message ?? 'Não foi possível alterar a OS. Tente de novo.'),
        });
    }

    protected onEscape(event: Event) {
        event.preventDefault();
        if (this.editing()) {
            if (!this.isSaving()) {
                this.cancelEditing();
            }
            return;
        }
        this.close();
    }

    private setOrder(order: ServiceOrder | null) {
        this.order.set(order);
        if (!order) {
            return;
        }

        this.form.reset({
            type: order.type,
            enterprise: order.enterprise,
            brand: order.brand ?? '',
            model: order.model ?? '',
            serialNumber: order.serialNumber ?? '',
            location: order.location ?? '',
            defect: order.defect ?? '',
            accessories: order.accessories ?? '',
            solution: order.solution ?? '',
            guarantee: order.guarantee ?? '',
            partCost: order.partCost ?? 0,
            laborCost: order.laborCost ?? 0,
            entryDate: toDateInput(order.entryDate),
            inspectionDate: toDateInput(order.inspectionDate),
            responseDate: toDateInput(order.responseDate),
            purchasePartDate: toDateInput(order.purchasePartDate),
            repairDate: toDateInput(order.repairDate),
            deliveryDate: toDateInput(order.deliveryDate),
            serviceOrderStatus: order.serviceOrderStatus,
            repairStatus: order.repairStatus,
            repairResult: order.repairResult ?? null,
            estimateMessage: order.estimateMessage ?? '',
            partPurchaseCost: order.partPurchaseCost ?? 0,
        });
    }
}
