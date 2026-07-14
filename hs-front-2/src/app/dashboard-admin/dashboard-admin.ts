import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, ChangeDetectorRef, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { AuthService } from '../core/auth.service';
import {
  ActualizarUsuarioAdminPayload,
  CitaAdmin,
  CrearUsuarioAdminPayload,
  ResumenAdmin,
  Rol,
  UsuarioAdmin,
  UsuarioAdminDetalle,
} from '../core/models';
import { HealthService } from '../core/health.service';
import { normalizarRut, validarRut } from '../core/rut';

type ModoFormularioAdmin = 'crear' | 'editar' | null;
type RolCreableAdmin = Exclude<Rol, 'invitado'>;

type FormularioUsuarioAdmin = CrearUsuarioAdminPayload & {
  passwordConfirmacion: string;
};

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

  modoFormulario: ModoFormularioAdmin = null;
  usuarioEditando: UsuarioAdminDetalle | null = null;
  cargandoEdicion = false;
  guardandoEdicion = false;
  errorEdicion = '';
  formUsuario: FormularioUsuarioAdmin = this.crearFormularioUsuario();
  rolesCreables: Array<{ valor: RolCreableAdmin; etiqueta: string; descripcion: string }> = [
    { valor: 'doctor', etiqueta: 'Profesional / Doctor', descripcion: 'Cuenta interna con agenda y acceso clínico.' },
    { valor: 'paciente', etiqueta: 'Paciente', descripcion: 'Cuenta para reserva de horas y ficha clínica.' },
    { valor: 'admin', etiqueta: 'Administrador', descripcion: 'Cuenta con acceso al panel administrativo.' },
  ];

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

  get mostrarModalUsuario(): boolean {
    return this.modoFormulario !== null || this.cargandoEdicion;
  }

  get rolFormulario(): RolCreableAdmin {
    return this.modoFormulario === 'editar'
      ? (this.usuarioEditando?.rol as RolCreableAdmin)
      : this.formUsuario.rol;
  }

  get tituloModalUsuario(): string {
    return this.modoFormulario === 'crear' ? 'Crear nuevo usuario' : 'Editar datos de usuario';
  }

  abrirCreacion(): void {
    this.modoFormulario = 'crear';
    this.usuarioEditando = null;
    this.cargandoEdicion = false;
    this.guardandoEdicion = false;
    this.errorEdicion = '';
    this.formUsuario = this.crearFormularioUsuario();
  }

  abrirEdicion(usuario: UsuarioAdmin): void {
    this.modoFormulario = 'editar';
    this.usuarioEditando = null;
    this.errorEdicion = '';
    this.cargandoEdicion = true;
    this.procesandoId = usuario.id;

    this.health.getUsuarioAdmin(usuario.id).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: (detalle) => {
        this.usuarioEditando = detalle;
        this.formUsuario = this.crearFormularioUsuario(detalle);
        this.cargandoEdicion = false;
        this.procesandoId = null;
      },
      error: (error: HttpErrorResponse) => {
        this.cargandoEdicion = false;
        this.procesandoId = null;
        this.modoFormulario = null;
        this.mensaje = error.error?.mensaje ?? 'No fue posible cargar los datos del usuario.';
        this.esError = true;
      },
    });
  }

  cerrarEdicion(): void {
    if (this.guardandoEdicion) return;
    this.modoFormulario = null;
    this.usuarioEditando = null;
    this.errorEdicion = '';
    this.formUsuario = this.crearFormularioUsuario();
  }

  formatearRutEdicion(): void {
    this.formUsuario.rut = normalizarRut(this.formUsuario.rut);
  }

  guardarUsuario(): void {
    if (this.modoFormulario === 'crear') {
      this.crearUsuario();
      return;
    }

    this.actualizarUsuario();
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

  private crearUsuario(): void {
    this.formatearRutEdicion();
    const error = this.validarFormularioUsuario(this.formUsuario.rol);
    if (error) {
      this.errorEdicion = error;
      return;
    }
    if (this.formUsuario.password.trim().length < 8) {
      this.errorEdicion = 'La contraseña inicial debe tener al menos 8 caracteres.';
      return;
    }
    if (this.formUsuario.password !== this.formUsuario.passwordConfirmacion) {
      this.errorEdicion = 'La confirmación de contraseña no coincide.';
      return;
    }

    const payload: CrearUsuarioAdminPayload = {
      ...this.crearPayloadBase(),
      rol: this.formUsuario.rol,
      password: this.formUsuario.password.trim(),
    };

    this.guardandoEdicion = true;
    this.errorEdicion = '';

    this.health.crearUsuarioAdmin(payload).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: ({ mensaje, usuario }) => {
        this.guardandoEdicion = false;
        this.usuarios = [this.usuarioDetalleComoResumen(usuario), ...this.usuarios];
        this.mensaje = `${mensaje} Entrega la contraseña inicial únicamente por un canal seguro.`;
        this.esError = false;
        this.cerrarEdicion();
        this.actualizarResumen();
      },
      error: (error: HttpErrorResponse) => {
        this.guardandoEdicion = false;
        this.errorEdicion = error.error?.mensaje ?? 'No fue posible crear el usuario.';
      },
    });
  }

  private actualizarUsuario(): void {
    if (!this.usuarioEditando) return;

    this.formatearRutEdicion();
    const error = this.validarFormularioUsuario(this.usuarioEditando.rol as RolCreableAdmin);
    if (error) {
      this.errorEdicion = error;
      return;
    }

    const payload: ActualizarUsuarioAdminPayload = this.crearPayloadBase();

    this.guardandoEdicion = true;
    this.errorEdicion = '';

    this.health.actualizarUsuarioAdmin(this.usuarioEditando.id, payload).pipe(finalize(() => this.cdr.markForCheck())).subscribe({
      next: ({ mensaje, usuario }) => {
        this.guardandoEdicion = false;
        this.usuarioEditando = usuario;
        this.formUsuario = this.crearFormularioUsuario(usuario);
        this.actualizarUsuarioEnTabla(usuario);
        this.mensaje = mensaje;
        this.esError = false;
      },
      error: (error: HttpErrorResponse) => {
        this.guardandoEdicion = false;
        this.errorEdicion = error.error?.mensaje ?? 'No fue posible actualizar el usuario.';
      },
    });
  }

  private validarFormularioUsuario(rol: RolCreableAdmin): string {
    if (!validarRut(this.formUsuario.rut)) return 'Ingresa un RUT chileno válido.';
    if (!this.formUsuario.nombre.trim()) return 'El nombre del usuario es obligatorio.';
    if (!this.formUsuario.correo?.trim()) return 'El correo del usuario es obligatorio.';
    if (rol === 'doctor' && (!this.formUsuario.especialidad.trim() || !this.formUsuario.numeroRegistro.trim())) {
      return 'La especialidad y el número de registro son obligatorios para profesionales.';
    }
    return '';
  }

  private crearPayloadBase(): ActualizarUsuarioAdminPayload {
    return {
      rut: this.formUsuario.rut.trim(),
      nombre: this.formUsuario.nombre.trim(),
      correo: this.formUsuario.correo?.trim().toLowerCase() || null,
      telefono: this.formUsuario.telefono.trim(),
      fechaNacimiento: this.formUsuario.fechaNacimiento || null,
      direccion: this.formUsuario.direccion.trim(),
      prevision: this.formUsuario.prevision.trim(),
      alergias: this.formUsuario.alergias.trim(),
      antecedentes: this.formUsuario.antecedentes.trim(),
      especialidad: this.formUsuario.especialidad.trim(),
      numeroRegistro: this.formUsuario.numeroRegistro.trim(),
    };
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

  private actualizarUsuarioEnTabla(usuario: UsuarioAdminDetalle): void {
    this.usuarios = this.usuarios.map((actual) => (actual.id === usuario.id ? this.usuarioDetalleComoResumen(usuario) : actual));
  }

  private usuarioDetalleComoResumen(usuario: UsuarioAdminDetalle): UsuarioAdmin {
    return {
      id: usuario.id,
      rut: usuario.rut,
      nombre: usuario.nombre,
      correo: usuario.correo,
      telefono: usuario.telefono,
      rol: usuario.rol,
      activo: usuario.activo,
      creadoEn: usuario.creadoEn,
    };
  }

  private crearFormularioUsuario(usuario?: UsuarioAdminDetalle): FormularioUsuarioAdmin {
    return {
      rol: (usuario?.rol as RolCreableAdmin) ?? 'doctor',
      rut: usuario?.rut ?? '',
      nombre: usuario?.nombre ?? '',
      correo: usuario?.correo ?? null,
      telefono: usuario?.telefono ?? '',
      fechaNacimiento: this.fechaParaInput(usuario?.fechaNacimiento),
      direccion: usuario?.direccion ?? '',
      prevision: usuario?.prevision ?? '',
      alergias: usuario?.alergias ?? '',
      antecedentes: usuario?.antecedentes ?? '',
      especialidad: usuario?.especialidad ?? '',
      numeroRegistro: usuario?.numeroRegistro ?? '',
      password: '',
      passwordConfirmacion: '',
    };
  }

  private fechaParaInput(fecha?: string | null): string | null {
    if (!fecha) return null;
    return fecha.slice(0, 10);
  }
}
