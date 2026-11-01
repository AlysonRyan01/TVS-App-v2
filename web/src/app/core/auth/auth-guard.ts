import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const service = inject(AuthService)
  const router = inject(Router)

  return service.isLoggedIn() ? true : router.parseUrl('/login');
};
