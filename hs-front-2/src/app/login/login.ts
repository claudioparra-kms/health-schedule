import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, ChangeDetectorRef, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { Rol } from '../core/models';
import { normalizarRut, validarRut } from '../core/rut';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrls: ['./login.css'],
})
export class Login {
  private readonly cdr = inject(ChangeDetectorRef);

  rut = '';
  password = '';
  rutInvitado = '';
  mensajeError = '';
  mensajeErrorInvitado = '';
  cargando = false;
  cargandoInvitado = false;
  mostrarPassword = false;

  constructor(private readonly auth: AuthService, private readonly router: Router) {}

  formatearRut(): void {
    this.rut = normalizarRut(this.rut);
  }

  formatearRutInvitado(): void {
    this.rutInvitado = normalizarRut(this.rutInvitado);
  }

  login(): void {
    this.mensajeError = '';
    this.formatearRut();

    if (!validarRut(this.rut)) {
      this.mensajeError = 'Ingresa un RUT chileno válido, por ejemplo 12345678-5.';
      return;
    }
    if (this.password.length < 8) {
      this.mensajeError = 'La contraseña debe tener al menos 8 caracteres.';
      return;
    }

    this.cargando = true;
    this.auth.login(this.rut, this.password).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: ({ usuario }) => {
        this.cargando = false;
        this.irAlPanel(usuario.rol);
      },
      error: (error: HttpErrorResponse) => {
        this.cargando = false;
        this.mensajeError = error.error?.mensaje ?? 'No fue posible iniciar sesión. Revisa que el backend esté ejecutándose.';
      },
    });
  }

  guestLogin(): void {
    this.mensajeErrorInvitado = '';
    this.formatearRutInvitado();

    if (!validarRut(this.rutInvitado)) {
      this.mensajeErrorInvitado = 'Ingresa un RUT chileno válido.';
      return;
    }

    this.cargandoInvitado = true;
    this.auth.ingresarInvitado(this.rutInvitado).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: () => {
        this.cargandoInvitado = false;
        this.router.navigate(['/dashboard-invitado']);
      },
      error: (error: HttpErrorResponse) => {
        this.cargandoInvitado = false;
        this.mensajeErrorInvitado = error.error?.mensaje ?? 'No fue posible continuar como invitado.';
      },
    });
  }

  private irAlPanel(rol: Rol): void {
    const routes: Record<Rol, string> = {
      paciente: '/dashboard-paciente',
      doctor: '/dashboard-doctor',
      admin: '/dashboard-admin',
      invitado: '/dashboard-invitado',
    };
    this.router.navigate([routes[rol]]);
  }
}
