import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

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

  login() {
    fetch('http://localhost:5220/Auth/Login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        rut: this.rut,
        password: this.password
      })
    })
    .then(res => {
      if (!res.ok) throw new Error('Login incorrecto');
      return res.json();
    })
    .then(data => {
      alert('Login correcto');
      console.log(data);
    })
    .catch(error => {
      console.error(error);
      alert('Error en login');
    });
  }

  guestLogin() {
    alert('Ingreso como invitado: ' + this.rutInvitado);
  }
}