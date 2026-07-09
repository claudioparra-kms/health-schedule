import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CitaDoctor } from '../core/models';
import { HealthService } from '../core/health.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-dashboard-doctor-atencion',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './dashboard-doctor-atencion.html',
  styleUrls: ['./dashboard-doctor-atencion.css'],
})
export class DashboardDoctorAtencion implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);

  citas: CitaDoctor[] = [];
  citaId = 0;
  motivo = '';
  diagnostico = '';
  tratamiento = '';
  receta = '';
  observaciones = '';
  mensaje = '';
  esError = false;
  cargando = true;
  guardando = false;

  constructor(private readonly health: HealthService) {}

  ngOnInit(): void {
    this.cargarCitas();
  }

  get citasDisponibles(): CitaDoctor[] {
    return this.citas
      .filter((cita) => ['pendiente', 'confirmada'].includes(cita.estado))
      .sort((a, b) => new Date(b.fechaInicio).getTime() - new Date(a.fechaInicio).getTime());
  }

  seleccionarCita(): void {
    const cita = this.citas.find((item) => item.id === this.citaId);
    this.motivo = cita?.motivo ?? '';
  }

  registrar(): void {
    this.mensaje = '';
    this.esError = false;
    if (!this.citaId) {
      this.mensaje = 'Selecciona la cita atendida.';
      this.esError = true;
      return;
    }
    if (this.diagnostico.trim().length < 3 || this.tratamiento.trim().length < 3) {
      this.mensaje = 'Completa diagnóstico y tratamiento.';
      this.esError = true;
      return;
    }

    this.guardando = true;
    this.health.registrarAtencion({
      citaId: this.citaId,
      motivo: this.motivo.trim(),
      diagnostico: this.diagnostico.trim(),
      tratamiento: this.tratamiento.trim(),
      receta: this.receta.trim(),
      observaciones: this.observaciones.trim(),
    }).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (response) => {
        this.guardando = false;
        this.mensaje = response.mensaje;
        this.citaId = 0;
        this.motivo = '';
        this.diagnostico = '';
        this.tratamiento = '';
        this.receta = '';
        this.observaciones = '';
        this.cargarCitas(false);
      },
      error: (error: HttpErrorResponse) => {
        this.guardando = false;
        this.esError = true;
        this.mensaje = error.error?.mensaje ?? 'No fue posible registrar la atención.';
      },
    });
  }

  etiquetaCita(cita: CitaDoctor): string {
    const fecha = new Date(cita.fechaInicio).toLocaleString('es-CL', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' });
    return `${fecha} · ${cita.paciente} · ${cita.motivo}`;
  }

  private cargarCitas(mostrarCarga = true): void {
    if (mostrarCarga) this.cargando = true;
    this.health.getAgendaDoctor().pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (citas) => {
        this.citas = citas;
        this.cargando = false;
      },
      error: () => {
        this.esError = true;
        this.mensaje = 'No fue posible cargar las citas.';
        this.cargando = false;
      },
    });
  }
}
