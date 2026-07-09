import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { HealthService } from '../core/health.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-modificar-perfil',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './modificar-perfil.html',
  styleUrls: ['./modificar-perfil.css'],
})
export class ModificarPerfil implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);

  nombre = '';
  rut = '';
  correo = '';
  telefono = '';
  direccion = '';
  prevision = '';
  alergias = '';
  antecedentes = '';
  fechaNacimiento = '';
  passwordActual = '';
  passwordNueva = '';
  confirmarPassword = '';
  mensaje = '';
  esError = false;
  cargando = true;
  guardando = false;

  constructor(
    private readonly health: HealthService,
    private readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.health.getPerfil().pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (perfil) => {
        this.nombre = perfil.nombre;
        this.rut = perfil.rut;
        this.correo = perfil.correo ?? '';
        this.telefono = perfil.telefono ?? '';
        this.direccion = perfil.direccion ?? '';
        this.prevision = perfil.prevision ?? '';
        this.alergias = perfil.alergias ?? '';
        this.antecedentes = perfil.antecedentes ?? '';
        this.fechaNacimiento = perfil.fechaNacimiento?.slice(0, 10) ?? '';
        this.cargando = false;
      },
      error: () => {
        this.mensaje = 'No fue posible cargar tus datos.';
        this.esError = true;
        this.cargando = false;
      },
    });
  }

  guardar(): void {
    this.mensaje = '';
    this.esError = false;

    if (!/^\S+@\S+\.\S+$/.test(this.correo.trim())) {
      this.mensaje = 'Ingresa un correo válido.';
      this.esError = true;
      return;
    }
    if (this.passwordNueva && this.passwordNueva.length < 8) {
      this.mensaje = 'La nueva contraseña debe tener al menos 8 caracteres.';
      this.esError = true;
      return;
    }
    if (this.passwordNueva !== this.confirmarPassword) {
      this.mensaje = 'La confirmación de contraseña no coincide.';
      this.esError = true;
      return;
    }

    this.guardando = true;
    this.health.actualizarPerfil({
      correo: this.correo.trim(),
      telefono: this.telefono.trim(),
      direccion: this.direccion.trim(),
      prevision: this.prevision.trim(),
      alergias: this.alergias.trim(),
      antecedentes: this.antecedentes.trim(),
      fechaNacimiento: this.fechaNacimiento || null,
      passwordActual: this.passwordActual,
      passwordNueva: this.passwordNueva,
    }).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (response) => {
        this.guardando = false;
        this.mensaje = response.mensaje;
        if (response.requiereNuevoLogin) {
          this.auth.clearSession();
          setTimeout(() => this.router.navigate(['/login']), 900);
        }
        this.passwordActual = '';
        this.passwordNueva = '';
        this.confirmarPassword = '';
      },
      error: (error: HttpErrorResponse) => {
        this.guardando = false;
        this.esError = true;
        this.mensaje = error.error?.mensaje ?? 'No fue posible actualizar el perfil.';
      },
    });
  }
}
