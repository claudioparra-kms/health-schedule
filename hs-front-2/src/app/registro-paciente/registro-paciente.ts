import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, ChangeDetectorRef, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { normalizarRut, validarRut } from '../core/rut';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-registro-paciente',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './registro-paciente.html',
  styleUrls: ['./registro-paciente.css'],
})
export class RegistroPaciente {
  private readonly cdr = inject(ChangeDetectorRef);

  rut = '';
  correo = '';
  nombre = '';
  telefono = '';
  password = '';
  confirmarPassword = '';
  mensajeError = '';
  cargando = false;

  constructor(private readonly auth: AuthService, private readonly router: Router) {}

  formatearRut(): void {
    this.rut = normalizarRut(this.rut);
  }

  registrar(): void {
    this.mensajeError = '';
    this.formatearRut();

    if (!validarRut(this.rut)) {
      this.mensajeError = 'El RUT ingresado no es válido.';
      return;
    }
    if (this.nombre.trim().length < 4 || !this.nombre.trim().includes(' ')) {
      this.mensajeError = 'Ingresa tu nombre y apellido.';
      return;
    }
    if (!/^\S+@\S+\.\S+$/.test(this.correo.trim())) {
      this.mensajeError = 'Ingresa un correo válido.';
      return;
    }
    if (this.password.length < 8) {
      this.mensajeError = 'La contraseña debe tener al menos 8 caracteres.';
      return;
    }
    if (this.password !== this.confirmarPassword) {
      this.mensajeError = 'Las contraseñas no coinciden.';
      return;
    }

    this.cargando = true;
    this.auth.registrar({
      rut: this.rut,
      nombre: this.nombre.trim(),
      correo: this.correo.trim(),
      telefono: this.telefono.trim(),
      password: this.password,
    }).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: () => {
        this.cargando = false;
        this.router.navigate(['/dashboard-paciente']);
      },
      error: (error: HttpErrorResponse) => {
        this.cargando = false;
        this.mensajeError = error.error?.mensaje ?? 'No fue posible crear la cuenta.';
      },
    });
  }
}
