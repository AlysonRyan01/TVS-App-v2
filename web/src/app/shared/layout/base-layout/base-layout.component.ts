import { Component, HostListener, inject, input, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

interface NavItem {
  label: string;
  icon: string;
  route?: string;
}

@Component({
  selector: 'app-base-layout',
  styleUrl: 'base-layout.component.scss',
  templateUrl: 'base-layout.component.html',
  imports: [RouterLink, RouterLinkActive],
})
export class BaseLayoutComponent {
  private _auth = inject(AuthService);
  private _router = inject(Router);

  title = input.required<string>();
  menuOpen = signal(false);

  navItems: NavItem[] = [
    { label: 'Gerenciar', icon: 'dashboard', route: '/' },
    { label: 'Adicionar OS', icon: 'add', route: '/adicionar' },
    { label: 'Vender', icon: 'sell', route: '/vender' },
    { label: 'Ordens', icon: 'receipt_long', route: '/ordens' },
    { label: 'Clientes', icon: 'group', route: '/clientes' },
    { label: 'Métricas', icon: 'bar_chart', route: '/metricas' },
  ];

  @HostListener('document:keydown.escape')
  closeMenu() {
    this.menuOpen.set(false);
  }

  toggleMenu() {
    this.menuOpen.update((open) => !open);
  }

  logout() {
    this._auth.logout();
    this._router.navigate(['/login']);
  }
}
