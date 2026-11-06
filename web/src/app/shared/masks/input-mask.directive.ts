import { Directive, ElementRef, HostListener, inject, input, OnInit } from "@angular/core";
import { NgControl } from "@angular/forms";
import { maskDocument, maskPhone } from "./masks";

export type InputMaskKind = 'phone' | 'document';

const MASKS: Record<InputMaskKind, (value: string) => string> = {
    phone: maskPhone,
    document: maskDocument,
};

/**
 * Formata o campo enquanto a pessoa digita: `<input formControlName="phone" appInputMask="phone">`.
 * O valor do formulário fica formatado; quem envia para a API remove a máscara.
 */
@Directive({ selector: 'input[appInputMask]' })
export class InputMaskDirective implements OnInit {
    private _control = inject(NgControl, { self: true });
    private _element = inject<ElementRef<HTMLInputElement>>(ElementRef);

    appInputMask = input.required<InputMaskKind>();

    ngOnInit(): void {
        // Valor que já veio preenchido (ex.: cliente existente) também aparece formatado.
        this.apply(this._control.value ?? '', false);
    }

    @HostListener('input')
    protected onInput() {
        this.apply(this._element.nativeElement.value, true);
    }

    private apply(raw: string, keepCaretAtEnd: boolean) {
        const masked = MASKS[this.appInputMask()](raw);
        if (masked === this._element.nativeElement.value && masked === this._control.value) {
            return;
        }

        this._control.control?.setValue(masked, { emitEvent: true });
        this._element.nativeElement.value = masked;

        if (keepCaretAtEnd) {
            this._element.nativeElement.setSelectionRange(masked.length, masked.length);
        }
    }
}
