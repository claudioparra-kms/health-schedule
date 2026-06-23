import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
    selector: 'app-recuperar-password',
    standalone: true,
    imports: [FormsModule, CommonModule, RouterLink],
    templateUrl: './recuperar-password.html',
    styleUrls: ['./recuperar-password.css'] })

export class RecuperarPassword {
    rutOCorreo = '';
    mensaje = '';
    passwordTemporal = '';

    constructor(private http: HttpClient) { }

    recuperar() {
        this.mensaje = '';
        this.passwordTemporal = '';
        if (!this.rutOCorreo.trim()) {
            this.mensaje = 'Debe ingresar su RUT o correo.';
            return;
        }
        this.http.post<any>('http://localhost:5220/Auth/RecuperarPassword',
            { rutOCorreo: this.rutOCorreo }
        ).subscribe({
            next: (res) => {
                this.mensaje = res.mensaje;
                this.passwordTemporal = res.passwordTemporal; },
                error: (err) => this.mensaje = err.error?.mensaje || 'Error al recuperar contraseña' }); } }
