export type Rol = 'paciente' | 'doctor' | 'admin' | 'invitado';

export interface UsuarioSesion {
  id: number;
  nombre: string;
  rut: string;
  correo: string | null;
  rol: Rol;
  pacienteId: number | null;
  doctorId: number | null;
  especialidad: string | null;
}

export interface SesionResponse {
  token: string;
  usuario: UsuarioSesion;
}

export interface Doctor {
  id: number;
  nombre: string;
  especialidad: string;
  numeroRegistro: string | null;
}

export interface HorarioDisponible {
  hora: string;
  fechaInicio: string;
  duracionMinutos: number;
}

export interface CitaPaciente {
  id: number;
  fechaInicio: string;
  fechaFin: string;
  estado: string;
  motivo: string;
  doctorId: number;
  doctor: string;
  especialidad: string;
}

export interface CitaDoctor {
  id: number;
  fechaInicio: string;
  fechaFin: string;
  estado: string;
  motivo: string;
  pacienteId: number;
  paciente: string;
  rutPaciente: string;
}

export interface Perfil {
  id: number;
  rut: string;
  nombre: string;
  correo: string | null;
  telefono: string | null;
  rol: Rol;
  fechaNacimiento: string | null;
  direccion: string | null;
  prevision: string | null;
  alergias: string | null;
  antecedentes: string | null;
  edad: number | null;
  especialidad: string | null;
  numeroRegistro: string | null;
}

export interface AtencionClinica {
  id: number;
  fecha: string;
  motivo: string;
  diagnostico: string | null;
  tratamiento: string | null;
  receta: string | null;
  observaciones: string | null;
  doctor: string;
  especialidad: string;
}

export interface FichaClinica {
  id: number;
  observacionesGenerales: string | null;
  creadaEn: string;
  paciente: {
    id: number;
    nombre: string;
    rut: string;
    fechaNacimiento: string | null;
    prevision: string | null;
    alergias: string | null;
    antecedentes: string | null;
  };
  atenciones: AtencionClinica[];
}

export interface PacienteDoctor {
  id: number;
  nombre: string;
  rut: string;
  correo: string | null;
  telefono: string | null;
  prevision: string | null;
  alergias: string | null;
  ultimaCita: string | null;
  proximaCita: string | null;
  totalCitas: number;
}

export interface ResumenDoctor {
  citasHoy: number;
  proximas: number;
  pendientes: number;
  pacientes: number;
}

export interface ResumenAdmin {
  usuarios: number;
  pacientes: number;
  doctores: number;
  citasHoy: number;
  citasPendientes: number;
}

export interface UsuarioAdmin {
  id: number;
  rut: string;
  nombre: string;
  correo: string | null;
  telefono: string | null;
  rol: Rol;
  activo: boolean;
  creadoEn: string;
}

export interface CitaAdmin {
  id: number;
  fechaInicio: string;
  fechaFin: string;
  estado: string;
  motivo: string | null;
  paciente: string;
  rutPaciente: string;
  doctor: string;
  especialidad: string;
}


export interface UsuarioAdminDetalle extends UsuarioAdmin {
  fechaNacimiento: string | null;
  direccion: string | null;
  prevision: string | null;
  alergias: string | null;
  antecedentes: string | null;
  especialidad: string | null;
  numeroRegistro: string | null;
}

export interface ActualizarUsuarioAdminPayload {
  rut: string;
  nombre: string;
  correo: string | null;
  telefono: string;
  fechaNacimiento: string | null;
  direccion: string;
  prevision: string;
  alergias: string;
  antecedentes: string;
  especialidad: string;
  numeroRegistro: string;
}

export interface CrearUsuarioAdminPayload extends ActualizarUsuarioAdminPayload {
  rol: Exclude<Rol, 'invitado'>;
  password: string;
}
