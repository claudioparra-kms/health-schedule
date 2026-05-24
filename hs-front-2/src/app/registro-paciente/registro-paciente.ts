import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-registro-paciente',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './registro-paciente.html',
  styleUrls: ['./registro-paciente.css']
})
export class RegistroPaciente {
  rut = '';
  correo = '';
  nombre = '';
  telefono = '';
  password = '';

  constructor(private http: HttpClient, private router: Router) {}

  registrar() {
    this.http.post<any>('http://localhost:5220/Auth/Registro', {
      rut: this.rut,
      correo: this.correo,
      nombre: this.nombre,
      telefono: this.telefono,
      password: this.password
    }).subscribe({
      next: () => {
        alert('Cuenta creada correctamente');
        this.router.navigate(['/']);
      },
      error: (err) => {
        console.error(err);
        alert('Error al crear la cuenta');
      }
    });
  }
}