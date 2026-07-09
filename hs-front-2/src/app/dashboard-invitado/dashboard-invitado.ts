import { CommonModule } from '@angular/common';
import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { CitaPaciente } from '../core/models';
import { HealthService } from '../core/health.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-dashboard-invitado',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard-invitado.html',
  styleUrls: ['./dashboard-invitado.css'],
})
export class DashboardInvitado implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);

  citas: CitaPaciente[] = [];
  cargando = true;

  constructor(
    readonly auth: AuthService,
    private readonly health: HealthService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.health.getMisCitas().pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (citas) => {
        this.citas = citas;
        this.cargando = false;
      },
      error: () => {
        this.cargando = false;
      },
    });
  }

  cerrarSesion(): void {
    this.auth.logout().pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: () => this.router.navigate(['/']),
      error: () => this.router.navigate(['/']),
    });
  }
}
