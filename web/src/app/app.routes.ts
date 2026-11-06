import { Routes } from '@angular/router';
import { LoginComponent } from './features/authentication/login/login.component';
import { guestGuard } from './core/auth/guest-guard';
import { authGuard } from './core/auth/auth-guard';

export const routes: Routes = [
    { path: 'login', title: 'Entrar', component: LoginComponent, canActivate: [guestGuard] },
    {
        path: '',
        title: 'Gerenciar',
        loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent),
        canActivate: [authGuard],
    },
    {
        path: 'adicionar',
        title: 'Adicionar OS',
        loadComponent: () => import('./features/add-service-order/add-service-order.component').then(m => m.AddServiceOrderComponent),
        canActivate: [authGuard],
    },
    {
        path: 'vender',
        title: 'Vender',
        loadComponent: () => import('./features/sell/sell.component').then(m => m.SellComponent),
        canActivate: [authGuard],
    },
    {
        path: 'ordens',
        title: 'Ordens',
        loadComponent: () => import('./features/search-service-orders/search-service-orders.component').then(m => m.SearchServiceOrdersComponent),
        canActivate: [authGuard],
    },
    {
        path: 'clientes',
        title: 'Clientes',
        loadComponent: () => import('./features/customers/customers-page/customers-page.component').then(m => m.CustomersPageComponent),
        canActivate: [authGuard],
    },
    {
        path: 'metricas',
        title: 'Métricas',
        loadComponent: () => import('./features/metrics/metrics.component').then(m => m.MetricsComponent),
        canActivate: [authGuard],
    },
    // Endereço antigo da tela de ordens (antes "Buscar OS").
    { path: 'buscar', redirectTo: 'ordens', pathMatch: 'full' },
    { path: '**', redirectTo: '' },
];
