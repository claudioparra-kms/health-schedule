import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, ChangeDetectorRef, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-recuperar-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './recuperar-password.html',
  styleUrls: ['./recuperar-password.css'],
})
export class RecuperarPassword {
  private readonly cdr = inject(ChangeDetectorRef);

  rutOCorreo = '';
  mensaje = '';
  esError = false;
  passwordTemporal = '';
  cargando = false;

  constructor(private readonly auth: AuthService) {}

  recuperar(): void {
    this.mensaje = '';
    this.passwordTemporal = '';
    this.esError = false;

    if (!this.rutOCorreo.trim()) {
      this.mensaje = 'Ingresa el RUT o correo de la cuenta.';
      this.esError = true;
      return;
    }

    this.cargando = true;
    this.auth.recuperarPassword(this.rutOCorreo.trim()).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (response) => {
        this.cargando = false;
        this.mensaje = response.mensaje;
        this.passwordTemporal = response.passwordTemporal;
      },
      error: (error: HttpErrorResponse) => {
        this.cargando = false;
        this.esError = true;
        this.mensaje = error.error?.mensaje ?? 'No fue posible generar la contraseña temporal.';
      },
    });
  }
}
