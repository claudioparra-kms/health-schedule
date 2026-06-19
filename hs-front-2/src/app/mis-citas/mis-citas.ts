import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-mis-citas',
  standalone: true,
  imports: [RouterLink, CommonModule],
  templateUrl: './mis-citas.html',
  styleUrls: ['./mis-citas.css']
})
export class MisCitas implements OnInit {
  citas: any[] = [];
  cargando = true;
  mensajeError = '';

  constructor(private http: HttpClient) {}

  ngOnInit() {
  const pacienteId = localStorage.getItem('paciente_id');
  console.log('paciente_id en localStorage:', pacienteId);

  if (!pacienteId) {
    this.mensajeError = 'No se pudo identificar al paciente.';
    this.cargando = false;
    return;
  }

  this.http.get<any[]>(`http://localhost:5220/Citas/Paciente/${pacienteId}`)
    .subscribe({
      next: (data) => {
        console.log('Respuesta del backend:', data);
        this.citas = data;
        this.cargando = false;
        console.log("cargando ahora es", this.cargando);
      },
      error: (err) => {
        console.log('Error del backend:', err);
        this.mensajeError = 'Error al cargar las citas.';
        this.cargando = false;
      }
    });
}
 

  formatearFecha(fechaStr: string): string {
    const fecha = new Date(fechaStr);
    return fecha.toLocaleDateString('es-CL');
  }

  formatearHora(fechaStr: string): string {
    const fecha = new Date(fechaStr);
    return fecha.toLocaleTimeString('es-CL', { hour: '2-digit', minute: '2-digit' });
  }

  badgeClase(estado: string): string {
    const clases: Record<string, string> = {
      confirmada: 'badge badge-confirmada',
      pendiente:  'badge badge-pendiente',
      cancelada:  'badge badge-cancelada',
      realizada:  'badge badge-realizada',
      no_asiste:  'badge badge-no-asiste'
    };
    return clases[estado] ?? 'badge';
  }
}
