import { CommonModule } from '@angular/common';
import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FichaClinica as FichaClinicaModel } from '../core/models';
import { HealthService } from '../core/health.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-ficha-clinica',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './ficha-clinica.html',
  styleUrls: ['./ficha-clinica.css'],
})
export class FichaClinica implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);

  ficha: FichaClinicaModel | null = null;
  cargando = true;
  mensajeError = '';

  constructor(private readonly health: HealthService) {}

  ngOnInit(): void {
    this.health.getMiFicha().pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (ficha) => {
        this.ficha = ficha;
        this.cargando = false;
      },
      error: () => {
        this.mensajeError = 'No fue posible cargar la ficha clínica.';
        this.cargando = false;
      },
    });
  }

  formatearFecha(fecha: string): string {
    return new Date(fecha).toLocaleDateString('es-CL', { day: '2-digit', month: 'long', year: 'numeric' });
  }
}
