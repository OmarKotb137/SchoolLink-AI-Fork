import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { AppRole, RoleService } from '../../shared/role.service';

export const authGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const roleService = inject(RoleService);
  const router = inject(Router);
  const allowedRoles = (route.data?.['roles'] as AppRole[] | undefined) ?? undefined;

  if (!authService.isAuthenticated()) {
    return router.createUrlTree([roleService.getLoginRouteForAllowedRoles(allowedRoles)]);
  }

  if (!roleService.hasRole()) {
    authService.clearSession();
    return router.createUrlTree([roleService.getLoginRouteForAllowedRoles(allowedRoles)]);
  }

  if (allowedRoles?.length && !roleService.canAccess(allowedRoles)) {
    return router.createUrlTree([roleService.getHomeRoute()]);
  }

  return true;
};
