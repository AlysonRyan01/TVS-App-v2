import { Routes } from '@angular/router';
import { LoginComponent } from './features/authentication/login/login.component';
import { guestGuard } from './core/auth/guest-guard';
import { HomeComponent } from './features/home/home.component';
import { authGuard } from './core/auth/auth-guard';

export const routes: Routes = [
    { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
    { path: '', component: HomeComponent, canActivate: [authGuard] },
    // Telas de cadastro carregadas sob demanda: o pacote inicial estava no limite de 1 MB.
    {
        path: 'adicionar',
        loadComponent: () => import('./features/add-service-order/add-service-order.component').then(m => m.AddServiceOrderComponent),
        canActivate: [authGuard],
    },
    {
        path: 'vender',
        loadComponent: () => import('./features/sell/sell.component').then(m => m.SellComponent),
        canActivate: [authGuard],
    },
    {
        path: 'buscar',
        loadComponent: () => import('./features/search-service-orders/search-service-orders.component').then(m => m.SearchServiceOrdersComponent),
        canActivate: [authGuard],
    },
    {
        path: 'clientes',
        loadComponent: () => import('./features/customers/customers-page/customers-page.component').then(m => m.CustomersPageComponent),
        canActivate: [authGuard],
    },
    {
        path: 'metricas',
        loadComponent: () => import('./features/metrics/metrics.component').then(m => m.MetricsComponent),
        canActivate: [authGuard],
    },
    { path: '**', redirectTo: '' },
];
