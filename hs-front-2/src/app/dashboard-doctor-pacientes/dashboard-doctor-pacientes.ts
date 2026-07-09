import { CommonModule } from '@angular/common';
import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PacienteDoctor } from '../core/models';
import { HealthService } from '../core/health.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-dashboard-doctor-pacientes',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './dashboard-doctor-pacientes.html',
  styleUrls: ['./dashboard-doctor-pacientes.css'],
})
export class DashboardDoctorPacientes implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);

  pacientes: PacienteDoctor[] = [];
  busqueda = '';
  cargando = true;
  mensajeError = '';

  constructor(private readonly health: HealthService, private readonly router: Router) {}

  ngOnInit(): void {
    this.health.getPacientesDoctor().pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (pacientes) => {
        this.pacientes = pacientes;
        this.cargando = false;
      },
      error: () => {
        this.mensajeError = 'No fue posible cargar los pacientes.';
        this.cargando = false;
      },
    });
  }

  get pacientesFiltrados(): PacienteDoctor[] {
    const term = this.busqueda.trim().toLowerCase();
    return term
      ? this.pacientes.filter((paciente) => `${paciente.nombre} ${paciente.rut}`.toLowerCase().includes(term))
      : this.pacientes;
  }

  abrirFicha(pacienteId: number): void {
    this.router.navigate(['/dashboard-doctor/fichas'], { queryParams: { pacienteId } });
  }

  formatearFecha(fecha: string | null): string {
    return fecha ? new Date(fecha).toLocaleDateString('es-CL') : 'Sin fecha';
  }
}
