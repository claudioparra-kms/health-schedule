import { CommonModule } from '@angular/common';
import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { AuthService } from '../core/auth.service';
import { CitaPaciente, Perfil } from '../core/models';
import { HealthService } from '../core/health.service';

@Component({
  selector: 'app-dashboard-paciente',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard-paciente.html',
  styleUrls: ['./dashboard-paciente.css'],
})
export class DashboardPaciente implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);

  perfil: Perfil | null = null;
  citas: CitaPaciente[] = [];
  cargando = true;
  mensajeError = '';

  constructor(
    readonly auth: AuthService,
    private readonly health: HealthService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    forkJoin({ perfil: this.health.getPerfil(), citas: this.health.getMisCitas() }).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: ({ perfil, citas }) => {
        this.perfil = perfil;
        this.citas = citas;
        this.cargando = false;
      },
      error: () => {
        this.mensajeError = 'No fue posible cargar la información del panel.';
        this.cargando = false;
      },
    });
  }

  get proximas(): CitaPaciente[] {
    const ahora = Date.now();
    return this.citas
      .filter((cita) => new Date(cita.fechaInicio).getTime() > ahora && ['pendiente', 'confirmada'].includes(cita.estado))
      .sort((a, b) => new Date(a.fechaInicio).getTime() - new Date(b.fechaInicio).getTime())
      .slice(0, 3);
  }

  get pendientes(): number {
    return this.citas.filter((cita) => cita.estado === 'pendiente').length;
  }

  get realizadas(): number {
    return this.citas.filter((cita) => cita.estado === 'realizada').length;
  }

  formatearFecha(fecha: string): string {
    return new Date(fecha).toLocaleDateString('es-CL', { weekday: 'short', day: '2-digit', month: 'short' });
  }

  formatearHora(fecha: string): string {
    return new Date(fecha).toLocaleTimeString('es-CL', { hour: '2-digit', minute: '2-digit' });
  }

  cerrarSesion(): void {
    this.auth.logout().pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: () => this.router.navigate(['/']),
      error: () => this.router.navigate(['/']),
    });
  }
}
