import { Routes } from '@angular/router';
import { LoginComponent } from './features/authentication/login/login.component';
import { guestGuard } from './core/auth/guest-guard';
import { HomeComponent } from './features/home/home.component';
import { authGuard } from './core/auth/auth-guard';

export const routes: Routes = [
    { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
    { path: '', component: HomeComponent, canActivate: [authGuard] },
    { path: '**', redirectTo: '' },
];
