import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { Rol } from '../core/models';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './landing.html',
  styleUrls: ['./landing.css'],
})
export class Landing {
  constructor(readonly auth: AuthService) {}

  get dashboardLink(): string {
    const links: Record<Rol, string> = {
      paciente: '/dashboard-paciente',
      doctor: '/dashboard-doctor',
      admin: '/dashboard-admin',
      invitado: '/dashboard-invitado',
    };
    return this.auth.usuario ? links[this.auth.usuario.rol] : '/login';
  }
}
