import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrls: ['./login.css']
})
export class Login {
  rut = '';
  password = '';
  rutInvitado = '';

  constructor(private http: HttpClient, private router: Router) {}

  login() {
    this.http.post<any>('http://localhost:5220/Auth/Login', {
      rut: this.rut,
      password: this.password
    }).subscribe({
      next: (data) => {
        alert('Login correcto. Bienvenido, ' + data.nombre);
        console.log(data);
        if (data.rol === 'admin') {
          this.router.navigate(['/dashboard-admin']);
        } else if (data.rol === 'doctor') {
          this.router.navigate(['/dashboard-doctor']);
        } else if (data.rol === 'paciente') {
          this.router.navigate(['/dashboard-paciente']);
        }
      },
      error: (err) => {
        console.error(err);
        alert('Rut o contraseña incorrectos');
      }
    });
  }

  guestLogin() {
  if (!this.rutInvitado.trim()) {
    alert('Ingresa tu RUT para continuar como invitado');
    return;
  }

  this.http.post<any>('http://localhost:5220/Auth/Invitado', {
    rut: this.rutInvitado
  }).subscribe({
    next: () => {
      this.router.navigate(['/dashboard-invitado']);
    },
    error: (err) => {
      console.error(err);
      alert('Error al registrar ingreso como invitado');
    }
  });
  }
}
