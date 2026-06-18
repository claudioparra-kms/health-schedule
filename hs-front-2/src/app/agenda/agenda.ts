import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
@Component({selector:'app-agenda', standalone:true, imports:[FormsModule, CommonModule, RouterLink], templateUrl:'./agenda.html', styleUrls:['./agenda.css']})
export class Agenda {
    especialidad='';
    doctorId=0;
    fecha='';
    hora='';
    motivo='';
    mensajeError='';
    pacienteId=0;
    horarios=['10:00','13:00','16:00','17:00'];
    constructor(private http:HttpClient)
        {const pacienteId=localStorage.getItem("paciente_id");
        if(pacienteId) this.pacienteId=Number(pacienteId);}
            agendar(){this.mensajeError='';
                if(!this.especialidad || !this.doctorId || !this.fecha || !this.hora)
                    {this.mensajeError='Debe completar especialidad, profesional, fecha y horario.'; return;}
                const fechaInicio=`${this.fecha}T${this.hora}:00`;
                const fin=new Date(fechaInicio);
                fin.setMinutes(fin.getMinutes()+30);
                const fechaFin=fin.toISOString().slice(0,19);
                this.http.post<any>('http://localhost:5220/Citas/Crear',
                    {pacienteId:this.pacienteId,
                    doctorId:Number(this.doctorId),
                    fechaInicio,
                    fechaFin,
                    motivo:this.motivo || this.especialidad}).subscribe({next:(res)=>alert(res.mensaje || 'Hora agendada correctamente'),
                    error:(err)=>this.mensajeError=err.error?.mensaje || 'Error al agendar hora'});}}
