import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink, CommonModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css']
})
export class Login {
  rut = '';
  password = '';
  rutInvitado = '';
  mensajeError = '';
  mensajeErrorInvitado = '';

  constructor(private http: HttpClient, private router: Router) {}

  formatearRut() {
    let r = this.rut.replace(/\./g, '').replace(/-/g, '').trim();
    if (r.length < 2) { this.mensajeError = 'El RUT ingresado no es válido'; return; }
    this.rut = r.slice(0, -1) + '-' + r.slice(-1);
  }

  formatearRutInvitado() {
    let r = this.rutInvitado.replace(/\./g, '').replace(/-/g, '').trim();
    if (r.length < 2) { this.mensajeErrorInvitado = 'El RUT ingresado no es válido'; return; }
    this.rutInvitado = r.slice(0, -1) + '-' + r.slice(-1);
  }

  validarRut(rut: string) {
    return /^[0-9]{7,8}-[0-9Kk]$/.test(rut);
  }

  login() {
    this.mensajeError = '';
    this.formatearRut();

    if (!this.rut.trim()) { this.mensajeError = 'El RUT es obligatorio'; return; }
    if (!this.validarRut(this.rut)) { this.mensajeError = 'El RUT ingresado no es válido. Ejemplo: 21667001-6'; return; }
    if (!this.password.trim()) { this.mensajeError = 'La contraseña es obligatoria'; return; }
    if (this.password.length < 8) { this.mensajeError = 'La contraseña debe tener mínimo 8 caracteres'; return; }

    this.http.post<any>('http://localhost:5220/Auth/Login', {
      rut: this.rut,
      password: this.password
    }).subscribe({
      next: (data) => {
        // Limpiar sesión anterior
        localStorage.clear();

        // Guardar datos individuales
        localStorage.setItem('rol',        data.rol);
        localStorage.setItem('nombre',     data.nombre);
        localStorage.setItem('rut',        data.rut);
        localStorage.setItem('paciente_id', data.paciente_id?.toString() ?? '');
        localStorage.setItem('doctor_id',   data.doctor_id?.toString() ?? '');

        if (data.rol === 'admin')    this.router.navigate(['/dashboard-admin']);
        else if (data.rol === 'doctor')   this.router.navigate(['/dashboard-doctor']);
        else if (data.rol === 'paciente') this.router.navigate(['/dashboard-paciente']);
      },
      error: () => this.mensajeError = 'RUT o contraseña incorrectos'
    });
  }

  guestLogin() {
    this.mensajeErrorInvitado = '';

    if (!this.rutInvitado.trim()) { this.mensajeErrorInvitado = 'Ingresa tu RUT para continuar como invitado'; return; }
    this.formatearRutInvitado();
    if (!this.validarRut(this.rutInvitado)) { this.mensajeErrorInvitado = 'El RUT ingresado no es válido. Ejemplo: 21667001-6'; return; }

    this.http.post<any>('http://localhost:5220/Auth/Invitado', {
      rut: this.rutInvitado
    }).subscribe({
      next: (data) => {
        // Limpiar sesión anterior
        localStorage.clear();

        // Guardar datos del invitado
        localStorage.setItem('rol',         'invitado');
        localStorage.setItem('nombre',      'Invitado');
        localStorage.setItem('rut',         this.rutInvitado);
        localStorage.setItem('paciente_id', data.paciente_id?.toString() ?? '');

        this.router.navigate(['/dashboard-invitado']);
      },
      error: () => this.mensajeErrorInvitado = 'Error al registrar ingreso como invitado'
    });
  }
}