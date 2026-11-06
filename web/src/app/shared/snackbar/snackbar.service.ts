import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

type SnackbarKind = 'success' | 'error';

@Injectable({
    providedIn: 'root'
})
export class SnackbarService {
    private _snack = inject(MatSnackBar);

    success(message: string): void {
        this.show(message, 'success');
    }

    error(message: string): void {
        this.show(message, 'error');
    }

    private show(message: string, kind: SnackbarKind): void {
        this._snack.open(message, 'Fechar', {
            duration: kind === 'error' ? 5000 : 3000,
            horizontalPosition: 'center',
            verticalPosition: 'bottom',
            panelClass: [`snackbar-${kind}`],
            politeness: kind === 'error' ? 'assertive' : 'polite',
        });
    }
}
