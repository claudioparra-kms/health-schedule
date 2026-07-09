import { CommonModule } from '@angular/common';
import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { AuthService } from '../core/auth.service';
import { CitaDoctor, ResumenDoctor } from '../core/models';
import { HealthService } from '../core/health.service';

@Component({
  selector: 'app-dashboard-doctor',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard-doctor.html',
  styleUrls: ['./dashboard-doctor.css'],
})
export class DashboardDoctor implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);

  resumen: ResumenDoctor = { citasHoy: 0, proximas: 0, pendientes: 0, pacientes: 0 };
  citas: CitaDoctor[] = [];
  cargando = true;
  mensajeError = '';

  constructor(
    readonly auth: AuthService,
    private readonly health: HealthService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    forkJoin({ resumen: this.health.getResumenDoctor(), citas: this.health.getAgendaDoctor() }).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: ({ resumen, citas }) => {
        this.resumen = resumen;
        this.citas = citas;
        this.cargando = false;
      },
      error: () => {
        this.mensajeError = 'No fue posible cargar el panel profesional.';
        this.cargando = false;
      },
    });
  }

  get proximasCitas(): CitaDoctor[] {
    const ahora = Date.now();
    return this.citas
      .filter((cita) => new Date(cita.fechaInicio).getTime() >= ahora && ['pendiente', 'confirmada'].includes(cita.estado))
      .sort((a, b) => new Date(a.fechaInicio).getTime() - new Date(b.fechaInicio).getTime())
      .slice(0, 5);
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
