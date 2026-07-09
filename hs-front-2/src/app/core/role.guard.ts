import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { Rol } from './models';

export const roleGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const roles = (route.data['roles'] as Rol[] | undefined) ?? [];
  const usuario = auth.usuario;

  if (!usuario) return router.createUrlTree(['/login']);
  if (roles.length === 0 || roles.includes(usuario.rol)) return true;

  const fallback: Record<Rol, string> = {
    paciente: '/dashboard-paciente',
    doctor: '/dashboard-doctor',
    admin: '/dashboard-admin',
    invitado: '/dashboard-invitado',
  };
  return router.createUrlTree([fallback[usuario.rol]]);
};
