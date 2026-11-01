import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const guestGuard: CanActivateFn = () => {
  const service = inject(AuthService)
  const router = inject(Router)

  return service.isLoggedIn() ? router.parseUrl('/') : true;
};
