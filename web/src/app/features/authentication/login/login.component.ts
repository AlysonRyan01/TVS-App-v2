import { Component, inject } from "@angular/core";
import { AuthService } from "../../../core/auth/auth.service";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { Router } from "@angular/router";
import { HttpErrorResponse } from "@angular/common/http";
import { SnackbarService } from "../../../shared/snackbar/snackbar.service";

@Component({
    selector: 'app-authentication-login',
    styleUrl: 'login.component.scss',
    templateUrl: 'login.component.html',
    imports: [ReactiveFormsModule]
})
export class LoginComponent {
    private _service = inject(AuthService)
    private _fb = inject(FormBuilder)
    private _snack = inject(SnackbarService)
    private _router = inject(Router)

    form = this._fb.nonNullable.group({
        email: ['', [Validators.required, Validators.email]],
        password: ['', [Validators.required, Validators.minLength(6)]]
    });

    login() {
        if (this.form.invalid) return;
        const { email, password } = this.form.getRawValue();
        this._service.login(email, password).subscribe({
            next: () => {
                this._router.navigate(['/'])
            },
            error: (error: HttpErrorResponse) => {
                console.log(error)
                const message = error.error?.message ??
                    (typeof error.error === 'string' ? error.error : 'Erro no servidor.');
                
                this._snack.open(message);
        }
    })
    }
}