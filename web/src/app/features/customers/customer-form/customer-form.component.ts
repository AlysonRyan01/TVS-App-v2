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

/**
 * Campos do cliente (nome, CPF/CNPJ, telefones, e-mail e endereço com busca por CEP).
 * Usado no cadastro da página e no modal de confirmação; quem usa decide os botões.
 */
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

    /** Cliente existente para editar; vazio = cadastro novo. */
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
        // Não aparecem na tela: a UF vem do CEP (padrão PR) e o e-mail do cliente existente é mantido.
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

    /** Valor pronto para a API: documento e telefones sem máscara (documento vazio vira null). */
    value(): CustomerForm {
        const raw = this.form.getRawValue();
        return {
            ...raw,
            document: normalizeDocument(raw.document) || null,
            phone: unmaskPhone(raw.phone),
            phone2: unmaskPhone(raw.phone2),
        };
    }

    /** Preenche o nome (ex.: com o texto digitado na busca). */
    setName(name: string) {
        this.form.controls.name.setValue(name);
        this.form.controls.name.markAsDirty();
    }

    reset() {
        this.form.reset();
    }

    /** Botão de lupa (ou Enter no CEP): busca rua, bairro, cidade e UF. */
    protected searchZipCode() {
        const cep = this.form.controls.zipCode.value.replace(/\D/g, '');
        if (cep.length !== 8) {
            this._snackbar.open('Informe um CEP com 8 dígitos.');
            return;
        }

        this.loadAddress(cep);
    }

    /** Preenche o endereço pelo CEP; a pessoa ainda pode corrigir. */
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
                    this._snackbar.open(error.status === 404 ? 'CEP não encontrado.' : 'Não foi possível buscar o CEP.'),
            });
    }
}
