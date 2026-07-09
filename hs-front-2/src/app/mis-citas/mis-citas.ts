import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { CitaPaciente } from '../core/models';
import { HealthService } from '../core/health.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-mis-citas',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './mis-citas.html',
  styleUrls: ['./mis-citas.css'],
})
export class MisCitas implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);

  citas: CitaPaciente[] = [];
  cargando = true;
  mensajeError = '';
  mensaje = (history.state as { mensaje?: string }).mensaje ?? '';
  filtro = 'todas';
  cancelandoId: number | null = null;

  constructor(readonly auth: AuthService, private readonly health: HealthService) {}

  ngOnInit(): void {
    this.cargarCitas();
  }

  get esInvitado(): boolean {
    return this.auth.usuario?.rol === 'invitado';
  }

  get inicioLink(): string {
    return this.esInvitado ? '/dashboard-invitado' : '/dashboard-paciente';
  }

  get citasFiltradas(): CitaPaciente[] {
    const ahora = Date.now();
    if (this.filtro === 'proximas') {
      return this.citas.filter((cita) => new Date(cita.fechaInicio).getTime() >= ahora && ['pendiente', 'confirmada'].includes(cita.estado));
    }
    if (this.filtro === 'historial') {
      return this.citas.filter((cita) => new Date(cita.fechaInicio).getTime() < ahora || ['realizada', 'cancelada', 'no_asiste'].includes(cita.estado));
    }
    return this.citas;
  }

  puedeCancelar(cita: CitaPaciente): boolean {
    return new Date(cita.fechaInicio).getTime() > Date.now() && ['pendiente', 'confirmada'].includes(cita.estado);
  }

  cancelar(cita: CitaPaciente): void {
    if (!this.puedeCancelar(cita) || !confirm('¿Confirmas que deseas cancelar esta cita?')) return;

    this.cancelandoId = cita.id;
    this.mensaje = '';
    this.mensajeError = '';
    this.health.cancelarCita(cita.id).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (response) => {
        this.cancelandoId = null;
        this.mensaje = response.mensaje;
        this.cargarCitas(false);
      },
      error: (error: HttpErrorResponse) => {
        this.cancelandoId = null;
        this.mensajeError = error.error?.mensaje ?? 'No fue posible cancelar la cita.';
      },
    });
  }

  formatearFecha(fecha: string): string {
    return new Date(fecha).toLocaleDateString('es-CL', { weekday: 'long', day: '2-digit', month: 'long', year: 'numeric' });
  }

  formatearHora(fecha: string): string {
    return new Date(fecha).toLocaleTimeString('es-CL', { hour: '2-digit', minute: '2-digit' });
  }

  private cargarCitas(mostrarCarga = true): void {
    if (mostrarCarga) this.cargando = true;
    this.health.getMisCitas().pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (citas) => {
        this.citas = citas;
        this.cargando = false;
      },
      error: () => {
        this.mensajeError = 'No fue posible cargar tus citas.';
        this.cargando = false;
      },
    });
  }
}
