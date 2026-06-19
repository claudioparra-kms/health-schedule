import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-dashboard-doctor-agenda',
  standalone: true,
  imports: [RouterLink, CommonModule],
  templateUrl: './dashboard-doctor-agenda.html',
  styleUrls: ['./dashboard-doctor-agenda.css']
})
export class DashboardDoctorAgenda implements OnInit {
  citas: any[] = [];
  cargando = true;
  mensajeError = '';

  constructor(private http: HttpClient) {}

  ngOnInit() {
    const doctorId = localStorage.getItem('doctor_id');

    if (!doctorId) {
      this.mensajeError = 'No se pudo identificar al doctor.';
      this.cargando = false;
      return;
    }

    this.http.get<any[]>(`http://localhost:5220/Citas/Doctor/${doctorId}`)
      .subscribe({
        next: (data) => {
          this.citas = data;
          this.cargando = false;
        },
        error: (err) => {
          console.log('Error del backend:', err);
          this.mensajeError = 'Error al cargar la agenda.';
          this.cargando = false;
        }
      });
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

  get citasHoy(): number {
    const hoy = new Date().toDateString();
    return this.citas.filter(c => new Date(c.fechaInicio).toDateString() === hoy).length;
  }

  get confirmadas(): number {
    return this.citas.filter(c => c.estado === 'confirmada').length;
  }

  get pendientes(): number {
    return this.citas.filter(c => c.estado === 'pendiente').length;
  }
}