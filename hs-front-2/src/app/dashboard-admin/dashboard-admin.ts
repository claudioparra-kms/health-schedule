import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { AuthService } from '../core/auth.service';
import { CitaAdmin, ResumenAdmin, UsuarioAdmin } from '../core/models';
import { HealthService } from '../core/health.service';

@Component({
  selector: 'app-dashboard-admin',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './dashboard-admin.html',
  styleUrls: ['./dashboard-admin.css'],
})
export class DashboardAdmin implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);

  resumen: ResumenAdmin = { usuarios: 0, pacientes: 0, doctores: 0, citasHoy: 0, citasPendientes: 0 };
  usuarios: UsuarioAdmin[] = [];
  citas: CitaAdmin[] = [];
  seccion: 'usuarios' | 'citas' = 'usuarios';
  busqueda = '';
  filtroEstado = 'todas';
  cargando = true;
  mensaje = '';
  esError = false;
  procesandoId: number | null = null;

  constructor(
    readonly auth: AuthService,
    private readonly health: HealthService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.cargarDatos();
  }

  get usuariosFiltrados(): UsuarioAdmin[] {
    const term = this.busqueda.trim().toLowerCase();
    return term
      ? this.usuarios.filter((usuario) => `${usuario.nombre} ${usuario.rut} ${usuario.correo ?? ''} ${usuario.rol}`.toLowerCase().includes(term))
      : this.usuarios;
  }

  get citasFiltradas(): CitaAdmin[] {
    const term = this.busqueda.trim().toLowerCase();
    return this.citas.filter((cita) => {
      const coincideTexto = !term || `${cita.paciente} ${cita.rutPaciente} ${cita.doctor} ${cita.especialidad}`.toLowerCase().includes(term);
      const coincideEstado = this.filtroEstado === 'todas' || cita.estado === this.filtroEstado;
      return coincideTexto && coincideEstado;
    });
  }

  alternarUsuario(usuario: UsuarioAdmin): void {
    const accion = usuario.activo ? 'desactivar' : 'activar';
    if (!confirm(`¿Deseas ${accion} la cuenta de ${usuario.nombre}?`)) return;

    this.procesandoId = usuario.id;
    this.health.cambiarEstadoUsuario(usuario.id, !usuario.activo).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (response) => {
        usuario.activo = !usuario.activo;
        this.procesandoId = null;
        this.mensaje = response.mensaje;
        this.esError = false;
        this.actualizarResumen();
      },
      error: (error: HttpErrorResponse) => {
        this.procesandoId = null;
        this.esError = true;
        this.mensaje = error.error?.mensaje ?? 'No fue posible cambiar el estado del usuario.';
      },
    });
  }

  cambiarEstadoCita(cita: CitaAdmin, estado: string): void {
    this.procesandoId = cita.id;
    this.health.cambiarEstadoCita(cita.id, estado).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (response) => {
        cita.estado = estado;
        this.procesandoId = null;
        this.mensaje = response.mensaje;
        this.esError = false;
        this.actualizarResumen();
      },
      error: (error: HttpErrorResponse) => {
        this.procesandoId = null;
        this.esError = true;
        this.mensaje = error.error?.mensaje ?? 'No fue posible actualizar la cita.';
      },
    });
  }

  formatearFecha(fecha: string): string {
    return new Date(fecha).toLocaleString('es-CL', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  cerrarSesion(): void {
    this.auth.logout().pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: () => this.router.navigate(['/']),
      error: () => this.router.navigate(['/']),
    });
  }

  private cargarDatos(): void {
    forkJoin({
      resumen: this.health.getResumenAdmin(),
      usuarios: this.health.getUsuariosAdmin(),
      citas: this.health.getCitasAdmin(),
    }).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: ({ resumen, usuarios, citas }) => {
        this.resumen = resumen;
        this.usuarios = usuarios;
        this.citas = citas;
        this.cargando = false;
      },
      error: () => {
        this.esError = true;
        this.mensaje = 'No fue posible cargar el panel administrativo.';
        this.cargando = false;
      },
    });
  }

  private actualizarResumen(): void {
    this.health.getResumenAdmin().pipe(finalize(() => this.cdr.markForCheck())).subscribe({ next: (resumen) => (this.resumen = resumen) });
  }
}
