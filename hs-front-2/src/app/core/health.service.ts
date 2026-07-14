import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from './api.config';
import {
  CitaAdmin,
  CitaDoctor,
  CitaPaciente,
  Doctor,
  FichaClinica,
  HorarioDisponible,
  PacienteDoctor,
  Perfil,
  ResumenAdmin,
  ResumenDoctor,
  UsuarioAdmin,
  UsuarioAdminDetalle,
  ActualizarUsuarioAdminPayload,
  CrearUsuarioAdminPayload,
} from './models';

@Injectable({ providedIn: 'root' })
export class HealthService {
  constructor(private readonly http: HttpClient) {}

  getEspecialidades(): Observable<string[]> {
    return this.http.get<string[]>(`${API_URL}/public/especialidades`);
  }

  getDoctores(especialidad = ''): Observable<Doctor[]> {
    const params = new HttpParams().set('especialidad', especialidad);
    return this.http.get<Doctor[]>(`${API_URL}/public/doctores`, { params });
  }

  getHorarios(doctorId: number, fecha: string): Observable<HorarioDisponible[]> {
    const params = new HttpParams().set('doctorId', doctorId).set('fecha', fecha);
    return this.http.get<HorarioDisponible[]>(`${API_URL}/public/horarios`, { params });
  }

  crearCita(payload: { doctorId: number; fechaInicio: string; motivo: string }): Observable<{ mensaje: string }> {
    return this.http.post<{ mensaje: string }>(`${API_URL}/citas`, payload);
  }

  getMisCitas(): Observable<CitaPaciente[]> {
    return this.http.get<CitaPaciente[]>(`${API_URL}/citas/mias`);
  }

  cancelarCita(id: number): Observable<{ mensaje: string }> {
    return this.http.patch<{ mensaje: string }>(`${API_URL}/citas/${id}/cancelar`, {});
  }

  getAgendaDoctor(): Observable<CitaDoctor[]> {
    return this.http.get<CitaDoctor[]>(`${API_URL}/citas/agenda-doctor`);
  }

  cambiarEstadoCita(id: number, estado: string): Observable<{ mensaje: string }> {
    return this.http.patch<{ mensaje: string }>(`${API_URL}/citas/${id}/estado`, { estado });
  }

  getPerfil(): Observable<Perfil> {
    return this.http.get<Perfil>(`${API_URL}/perfil`);
  }

  actualizarPerfil(payload: {
    correo: string;
    telefono: string;
    direccion: string;
    prevision: string;
    alergias: string;
    antecedentes: string;
    fechaNacimiento: string | null;
    passwordActual: string;
    passwordNueva: string;
  }): Observable<{ mensaje: string; requiereNuevoLogin: boolean }> {
    return this.http.put<{ mensaje: string; requiereNuevoLogin: boolean }>(`${API_URL}/perfil`, payload);
  }

  getMiFicha(): Observable<FichaClinica> {
    return this.http.get<FichaClinica>(`${API_URL}/fichas/mi-ficha`);
  }

  getFichaPaciente(pacienteId: number): Observable<FichaClinica> {
    return this.http.get<FichaClinica>(`${API_URL}/fichas/paciente/${pacienteId}`);
  }

  registrarAtencion(payload: {
    citaId: number;
    motivo: string;
    diagnostico: string;
    tratamiento: string;
    receta: string;
    observaciones: string;
  }): Observable<{ mensaje: string }> {
    return this.http.post<{ mensaje: string }>(`${API_URL}/fichas/atenciones`, payload);
  }

  getResumenDoctor(): Observable<ResumenDoctor> {
    return this.http.get<ResumenDoctor>(`${API_URL}/doctor/resumen`);
  }

  getPacientesDoctor(): Observable<PacienteDoctor[]> {
    return this.http.get<PacienteDoctor[]>(`${API_URL}/doctor/pacientes`);
  }

  getResumenAdmin(): Observable<ResumenAdmin> {
    return this.http.get<ResumenAdmin>(`${API_URL}/admin/resumen`);
  }

  getUsuariosAdmin(): Observable<UsuarioAdmin[]> {
    return this.http.get<UsuarioAdmin[]>(`${API_URL}/admin/usuarios`);
  }

  getUsuarioAdmin(id: number): Observable<UsuarioAdminDetalle> {
    return this.http.get<UsuarioAdminDetalle>(`${API_URL}/admin/usuarios/${id}`);
  }

  actualizarUsuarioAdmin(
    id: number,
    payload: ActualizarUsuarioAdminPayload,
  ): Observable<{ mensaje: string; usuario: UsuarioAdminDetalle }> {
    return this.http.put<{ mensaje: string; usuario: UsuarioAdminDetalle }>(`${API_URL}/admin/usuarios/${id}`, payload);
  }

  crearUsuarioAdmin(
    payload: CrearUsuarioAdminPayload,
  ): Observable<{ mensaje: string; usuario: UsuarioAdminDetalle }> {
    return this.http.post<{ mensaje: string; usuario: UsuarioAdminDetalle }>(`${API_URL}/admin/usuarios`, payload);
  }

  getCitasAdmin(): Observable<CitaAdmin[]> {
    return this.http.get<CitaAdmin[]>(`${API_URL}/admin/citas`);
  }

  cambiarEstadoUsuario(id: number, activo: boolean): Observable<{ mensaje: string }> {
    return this.http.patch<{ mensaje: string }>(`${API_URL}/admin/usuarios/${id}/estado`, { activo });
  }
}
