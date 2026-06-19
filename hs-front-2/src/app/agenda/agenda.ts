import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector:'app-agenda',
  standalone:true,
  imports:[FormsModule, CommonModule, RouterLink],
  templateUrl:'./agenda.html',
  styleUrls:['./agenda.css']
})
export class Agenda {
  especialidad='';
  doctorId=0;
  fecha='';
  hora='';
  motivo='';
  mensajeError='';
  pacienteId=1;
  horarios=['10:00','13:00','16:00','17:00'];

  usuario:any = {};
  esInvitado = false;

  constructor(private http:HttpClient, private router:Router){
    this.usuario = JSON.parse(localStorage.getItem('usuario') || '{}');
    this.esInvitado = this.usuario?.rol === 'invitado';

    if(this.usuario?.pacienteId){
      this.pacienteId = this.usuario.pacienteId;
    }
  }

  volverInicio(){
    if(this.esInvitado){
      this.router.navigate(['/dashboard-invitado']);
    } else {
      this.router.navigate(['/dashboard-paciente']);
    }
  }

  agendar(){
    this.mensajeError='';

    if(!this.especialidad || !this.doctorId || !this.fecha || !this.hora){
      this.mensajeError='Debe completar especialidad, profesional, fecha y horario.';
      return;
    }

    if(this.esInvitado){
      alert('Hora solicitada correctamente como invitado. Para ver historial, ficha clínica o modificar datos debes crear una cuenta.');
      this.volverInicio();
      return;
    }

    const fechaInicio=`${this.fecha}T${this.hora}:00`;
    const fin=new Date(fechaInicio);
    fin.setMinutes(fin.getMinutes()+30);
    const fechaFin=fin.toISOString().slice(0,19);

    this.http.post<any>('http://localhost:5220/Citas/Crear',{
      pacienteId:this.pacienteId,
      doctorId:Number(this.doctorId),
      fechaInicio,
      fechaFin,
      motivo:this.motivo || this.especialidad
    }).subscribe({
      next:(res)=>{
        alert(res.mensaje || 'Hora agendada correctamente');
        this.volverInicio();
      },
      error:(err)=>this.mensajeError=err.error?.mensaje || 'Error al agendar hora'
    });
  }
}
