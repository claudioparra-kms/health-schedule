import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css']
})
export class Login {
  rut = '';
  password = '';
  rutInvitado = '';

  constructor(private http: HttpClient) {}

  login() {
    this.http.post<any>('http://localhost:5220/Auth/Login', {
      rut: this.rut,
      password: this.password
    }).subscribe({
      next: (data) => {
        alert('Login correcto. Bienvenido, ' + data.nombre);
        console.log(data);
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
    alert('Ingreso como invitado: ' + this.rutInvitado);
  }
}
