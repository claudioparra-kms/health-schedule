import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
@Component({selector:'app-modificar-perfil', standalone:true, imports:[FormsModule, RouterLink, CommonModule], templateUrl:'./modificar-perfil.html', styleUrls:['./modificar-perfil.css']})
export class ModificarPerfil { usuario: any = JSON.parse(localStorage.getItem('usuario') || '{}'); correo = ''; telefono = ''; direccion = ''; password = ''; fechaNacimiento = ''; mensaje = ''; constructor(private http: HttpClient) { this.correo = this.usuario.correo || ''; } guardar() { this.http.put<any>('http://localhost:5220/Auth/ActualizarPerfil', { usuarioId: this.usuario.id, correo: this.correo, telefono: this.telefono, direccion: this.direccion, password: this.password, fechaNacimiento: this.fechaNacimiento || null }).subscribe({ next: (r) => this.mensaje = r.mensaje || 'Datos actualizados', error: (e) => this.mensaje = e.error?.mensaje || 'Error al actualizar' }); } }
