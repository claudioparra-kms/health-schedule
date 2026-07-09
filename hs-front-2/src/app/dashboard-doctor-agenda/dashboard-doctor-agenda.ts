import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CitaDoctor } from '../core/models';
import { HealthService } from '../core/health.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-dashboard-doctor-agenda',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './dashboard-doctor-agenda.html',
  styleUrls: ['./dashboard-doctor-agenda.css'],
})
export class DashboardDoctorAgenda implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);

  citas: CitaDoctor[] = [];
  cargando = true;
  mensaje = '';
  esError = false;
  filtroEstado = 'activas';
  filtroFecha = '';
  actualizandoId: number | null = null;

  constructor(private readonly health: HealthService) {}

  ngOnInit(): void {
    this.cargarAgenda();
  }

  get citasFiltradas(): CitaDoctor[] {
    return this.citas.filter((cita) => {
      const coincideEstado = this.filtroEstado === 'todas'
        || (this.filtroEstado === 'activas' && ['pendiente', 'confirmada'].includes(cita.estado))
        || cita.estado === this.filtroEstado;
      const coincideFecha = !this.filtroFecha || cita.fechaInicio.slice(0, 10) === this.filtroFecha;
      return coincideEstado && coincideFecha;
    });
  }

  cambiarEstado(cita: CitaDoctor, estado: string): void {
    this.actualizandoId = cita.id;
    this.mensaje = '';
    this.esError = false;
    this.health.cambiarEstadoCita(cita.id, estado).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (response) => {
        this.actualizandoId = null;
        this.mensaje = response.mensaje;
        cita.estado = estado;
      },
      error: (error: HttpErrorResponse) => {
        this.actualizandoId = null;
        this.esError = true;
        this.mensaje = error.error?.mensaje ?? 'No fue posible actualizar la cita.';
      },
    });
  }

  formatearFecha(fecha: string): string {
    return new Date(fecha).toLocaleDateString('es-CL', { weekday: 'short', day: '2-digit', month: 'short', year: 'numeric' });
  }

  formatearHora(fecha: string): string {
    return new Date(fecha).toLocaleTimeString('es-CL', { hour: '2-digit', minute: '2-digit' });
  }

  private cargarAgenda(): void {
    this.health.getAgendaDoctor().pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (citas) => {
        this.citas = citas;
        this.cargando = false;
      },
      error: () => {
        this.esError = true;
        this.mensaje = 'No fue posible cargar la agenda.';
        this.cargando = false;
      },
    });
  }
}
