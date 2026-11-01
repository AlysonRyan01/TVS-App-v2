import { Component, inject } from "@angular/core";
import { AuthService } from "../../../core/auth/auth.service";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { MatSnackBar } from "@angular/material/snack-bar";
import { Router } from "@angular/router";

@Component({
    selector: 'app-authentication-login',
    styleUrl: 'login.component.scss',
    templateUrl: 'login.component.html',
    imports: [ReactiveFormsModule]
})
export class LoginComponent {
    private _service = inject(AuthService)
    private _fb = inject(FormBuilder)
    private _snack = inject(MatSnackBar)
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
                this._snack.open('Login realizado com sucesso!', 'Fechar', {
                    duration: 3000,
                    horizontalPosition: 'right',
                    verticalPosition: 'top',
                });
                this._router.navigate(['/'])
            }
        }
        )
    }
}