import { inject } from '@angular/core';
import { Router, type CanActivateFn, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * RoleGuard — vérifie que l'utilisateur possède au moins l'un des rôles
 * déclarés dans la propriété `data.roles` de la route.
 *
 * Usage dans app.routes.ts :
 *   {
 *     path: 'atms/create',
 *     component: AtmFormComponent,
 *     canActivate: [authGuard, roleGuard],
 *     data: { roles: ['Support'] }
 *   }
 *
 * Si l'utilisateur est connecté mais sans les droits → /access-denied
 * Si l'utilisateur n'est pas connecté            → /login
 */
export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const authService = inject(AuthService);
  const router      = inject(Router);

  // Pas connecté → login
  if (!authService.isAuthenticated()) {
    return router.parseUrl('/login');
  }

  const requiredRoles: string[] = route.data?.['roles'] ?? [];

  // Aucune restriction déclarée → on laisse passer
  if (requiredRoles.length === 0) {
    return true;
  }

  // Vérifie si l'utilisateur possède au moins un rôle requis
  const hasAccess = authService.hasRole(requiredRoles);

  if (!hasAccess) {
    return router.parseUrl('/access-denied');
  }

  return true;
};

