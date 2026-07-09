import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { Doctor, HorarioDisponible } from '../core/models';
import { HealthService } from '../core/health.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-agenda',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './agenda.html',
  styleUrls: ['./agenda.css'],
})
export class Agenda implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);

  especialidades: string[] = [];
  doctores: Doctor[] = [];
  horarios: HorarioDisponible[] = [];
  especialidad = '';
  doctorId = 0;
  fecha = '';
  horarioSeleccionado: HorarioDisponible | null = null;
  motivo = '';
  mensajeError = '';
  cargandoDoctores = false;
  cargandoHorarios = false;
  guardando = false;
  readonly fechaMinima = this.formatInputDate(new Date());
  readonly fechaMaxima = this.formatInputDate(new Date(Date.now() + 90 * 24 * 60 * 60 * 1000));

  constructor(
    readonly auth: AuthService,
    private readonly health: HealthService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.health.getEspecialidades().pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (especialidades) => (this.especialidades = especialidades),
      error: () => (this.mensajeError = 'No fue posible cargar las especialidades.'),
    });
  }

  get esInvitado(): boolean {
    return this.auth.usuario?.rol === 'invitado';
  }

  get inicioLink(): string {
    return this.esInvitado ? '/dashboard-invitado' : '/dashboard-paciente';
  }

  get nombreDoctorSeleccionado(): string {
    return this.doctores.find((doctor) => doctor.id === this.doctorId)?.nombre ?? 'Por seleccionar';
  }

  cambiarEspecialidad(): void {
    this.doctorId = 0;
    this.horarios = [];
    this.horarioSeleccionado = null;
    if (!this.especialidad) {
      this.doctores = [];
      return;
    }

    this.cargandoDoctores = true;
    this.health.getDoctores(this.especialidad).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (doctores) => {
        this.doctores = doctores;
        this.cargandoDoctores = false;
      },
      error: () => {
        this.cargandoDoctores = false;
        this.mensajeError = 'No fue posible cargar los profesionales.';
      },
    });
  }

  cargarHorarios(): void {
    this.horarioSeleccionado = null;
    this.horarios = [];
    this.mensajeError = '';
    if (!this.doctorId || !this.fecha) return;

    this.cargandoHorarios = true;
    this.health.getHorarios(this.doctorId, this.fecha).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (horarios) => {
        this.horarios = horarios;
        this.cargandoHorarios = false;
      },
      error: (error: HttpErrorResponse) => {
        this.cargandoHorarios = false;
        this.mensajeError = error.error?.mensaje ?? 'No fue posible consultar los horarios.';
      },
    });
  }

  agendar(): void {
    this.mensajeError = '';
    if (!this.especialidad || !this.doctorId || !this.fecha || !this.horarioSeleccionado) {
      this.mensajeError = 'Completa especialidad, profesional, fecha y horario.';
      return;
    }
    if (this.motivo.trim().length < 3) {
      this.mensajeError = 'Describe brevemente el motivo de la consulta.';
      return;
    }

    this.guardando = true;
    this.health.crearCita({
      doctorId: this.doctorId,
      fechaInicio: this.horarioSeleccionado.fechaInicio,
      motivo: this.motivo.trim(),
    }).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: () => {
        this.guardando = false;
        this.router.navigate(['/mis-citas'], { state: { mensaje: 'Hora reservada correctamente.' } });
      },
      error: (error: HttpErrorResponse) => {
        this.guardando = false;
        this.mensajeError = error.error?.mensaje ?? 'No fue posible reservar la hora.';
        this.cargarHorarios();
      },
    });
  }

  private formatInputDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
