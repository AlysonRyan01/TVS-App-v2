import { Component, inject, input, OnInit, signal } from "@angular/core";
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from "@angular/forms";
import { HttpErrorResponse } from "@angular/common/http";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { finalize } from "rxjs";
import { SnackbarService } from "../../../shared/snackbar/snackbar.service";
import { CustomersService } from "../customers.service";
import { Customer, CustomerForm } from "../models/customer.model";
import { formatDocument, isValidDocument, normalizeDocument } from "../models/document";
import { InputMaskDirective } from "../../../shared/masks/input-mask.directive";
import { unmaskPhone } from "../../../shared/masks/masks";

function documentValidator(control: AbstractControl): ValidationErrors | null {
    return isValidDocument(control.value) ? null : { document: true };
}

@Component({
    selector: 'app-customer-form',
    templateUrl: './customer-form.component.html',
    styleUrl: './customer-form.component.scss',
    imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, InputMaskDirective],
})
export class CustomerFormComponent implements OnInit {
    private _service = inject(CustomersService);
    private _snackbar = inject(SnackbarService);
    private _fb = inject(FormBuilder);

    customer = input<Customer | null>(null);

    protected isLoadingAddress = signal(false);

    readonly form = this._fb.nonNullable.group({
        name: ['', Validators.required],
        document: ['', documentValidator],
        phone: ['', Validators.required],
        phone2: [''],
        email: ['', Validators.email],
        zipCode: [''],
        street: [''],
        number: [''],
        neighborhood: [''],
        city: ['Campo Largo'],
        state: ['PR', Validators.maxLength(2)],
    });

    ngOnInit(): void {
        const customer = this.customer();
        if (customer) {
            this.form.reset({
                name: customer.name,
                document: formatDocument(customer.document),
                phone: customer.phone,
                phone2: customer.phone2,
                email: customer.email ?? '',
                zipCode: customer.zipCode,
                street: customer.street,
                number: customer.number,
                neighborhood: customer.neighborhood,
                city: customer.city,
                state: customer.state,
            });
        }
    }

    value(): CustomerForm {
        const raw = this.form.getRawValue();
        return {
            ...raw,
            document: normalizeDocument(raw.document) || null,
            phone: unmaskPhone(raw.phone),
            phone2: unmaskPhone(raw.phone2),
        };
    }

    setName(name: string) {
        this.form.controls.name.setValue(name);
        this.form.controls.name.markAsDirty();
    }

    reset() {
        this.form.reset();
    }

    protected searchZipCode() {
        const cep = this.form.controls.zipCode.value.replace(/\D/g, '');
        if (cep.length !== 8) {
            this._snackbar.error('Informe um CEP com 8 dígitos.');
            return;
        }

        this.loadAddress(cep);
    }

    private loadAddress(cep: string) {
        this.isLoadingAddress.set(true);
        this._service.getAddress(cep)
            .pipe(finalize(() => this.isLoadingAddress.set(false)))
            .subscribe({
                next: result => {
                    const address = result.data;
                    if (!address) {
                        return;
                    }

                    this.form.patchValue({
                        street: address.street,
                        neighborhood: address.neighborhood,
                        city: address.city,
                        state: address.state,
                    });
                    this.form.markAsDirty();
                },
                error: (error: HttpErrorResponse) =>
                    this._snackbar.error(error.status === 404 ? 'CEP não encontrado.' : 'Não foi possível buscar o CEP.'),
            });
    }
}
