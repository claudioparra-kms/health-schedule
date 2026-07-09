import { CommonModule } from '@angular/common';
import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FichaClinica, PacienteDoctor } from '../core/models';
import { HealthService } from '../core/health.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-dashboard-doctor-fichas',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './dashboard-doctor-fichas.html',
  styleUrls: ['./dashboard-doctor-fichas.css'],
})
export class DashboardDoctorFichas implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);

  pacientes: PacienteDoctor[] = [];
  pacienteId = 0;
  ficha: FichaClinica | null = null;
  cargandoPacientes = true;
  cargandoFicha = false;
  mensajeError = '';

  constructor(private readonly health: HealthService, private readonly route: ActivatedRoute) {}

  ngOnInit(): void {
    const queryPacienteId = Number(this.route.snapshot.queryParamMap.get('pacienteId')) || 0;
    this.health.getPacientesDoctor().pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (pacientes) => {
        this.pacientes = pacientes;
        this.cargandoPacientes = false;
        this.pacienteId = pacientes.some((paciente) => paciente.id === queryPacienteId)
          ? queryPacienteId
          : pacientes[0]?.id ?? 0;
        if (this.pacienteId) this.cargarFicha();
      },
      error: () => {
        this.mensajeError = 'No fue posible cargar los pacientes.';
        this.cargandoPacientes = false;
      },
    });
  }

  cargarFicha(): void {
    this.ficha = null;
    this.mensajeError = '';
    if (!this.pacienteId) return;
    this.cargandoFicha = true;
    this.health.getFichaPaciente(this.pacienteId).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (ficha) => {
        this.ficha = ficha;
        this.cargandoFicha = false;
      },
      error: () => {
        this.mensajeError = 'No fue posible cargar la ficha seleccionada.';
        this.cargandoFicha = false;
      },
    });
  }

  formatearFecha(fecha: string): string {
    return new Date(fecha).toLocaleDateString('es-CL', { day: '2-digit', month: 'long', year: 'numeric' });
  }
}
