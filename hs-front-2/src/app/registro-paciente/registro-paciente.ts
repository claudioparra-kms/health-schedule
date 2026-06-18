import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
@Component({selector:'app-registro-paciente', standalone:true, imports:[FormsModule, RouterLink, CommonModule], templateUrl:'./registro-paciente.html', styleUrls:['./registro-paciente.css']})
export class RegistroPaciente { rut=''; correo=''; nombre=''; telefono=''; password=''; mensajeError=''; constructor(private http:HttpClient, private router:Router){} registrar(){this.mensajeError=''; if(this.password.length<8){this.mensajeError='La contraseña debe tener mínimo 8 caracteres'; return;} this.http.post<any>('http://localhost:5220/Auth/Registro',{rut:this.rut,nombre:this.nombre,correo:this.correo,telefono:this.telefono,password:this.password}).subscribe({next:()=>{alert('Cuenta creada exitosamente'); this.router.navigate(['/dashboard-paciente']);}, error:(e)=>this.mensajeError=e.error?.mensaje || 'No se pudo crear la cuenta. Revisa los datos.'});}}
