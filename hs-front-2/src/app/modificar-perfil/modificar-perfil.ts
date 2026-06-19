import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({selector:'app-modificar-perfil', standalone:true, imports:[FormsModule, RouterLink, CommonModule], templateUrl:'./modificar-perfil.html', styleUrls:['./modificar-perfil.css']})
export class ModificarPerfil {
  usuarioId = Number(localStorage.getItem('id')) || 0;
  correo = localStorage.getItem('correo') || '';
  telefono = '';
  direccion = '';
  password = '';
  fechaNacimiento = '';
  mensaje = '';

  constructor(private http: HttpClient) {}

  guardar() {
    this.mensaje = '';

    this.http.put<any>('http://localhost:5220/Auth/ActualizarPerfil', {
      usuarioId: this.usuarioId,
      correo: this.correo,
      telefono: this.telefono,
      direccion: this.direccion,
      password: this.password,
      fechaNacimiento: this.fechaNacimiento || null
    }).subscribe({
      next: (r) => {
        this.mensaje = r.mensaje || 'Datos actualizados';
        // Mantener el correo actualizado en localStorage
        localStorage.setItem('correo', this.correo);
        // Limpiar el campo contraseña tras guardar
        this.password = '';
      },
      error: (e) => this.mensaje = e.error?.mensaje || 'Error al actualizar'
    });
  }
}