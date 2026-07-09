import { Routes } from '@angular/router';
import { Agenda } from './agenda/agenda';
import { DashboardAdmin } from './dashboard-admin/dashboard-admin';
import { DashboardDoctorAgenda } from './dashboard-doctor-agenda/dashboard-doctor-agenda';
import { DashboardDoctorAtencion } from './dashboard-doctor-atencion/dashboard-doctor-atencion';
import { DashboardDoctorFichas } from './dashboard-doctor-fichas/dashboard-doctor-fichas';
import { DashboardDoctorPacientes } from './dashboard-doctor-pacientes/dashboard-doctor-pacientes';
import { DashboardDoctor } from './dashboard-doctor/dashboard-doctor';
import { DashboardInvitado } from './dashboard-invitado/dashboard-invitado';
import { DashboardPaciente } from './dashboard-paciente/dashboard-paciente';
import { FichaClinica } from './ficha-clinica/ficha-clinica';
import { Landing } from './landing/landing';
import { Login } from './login/login';
import { MisCitas } from './mis-citas/mis-citas';
import { ModificarPerfil } from './modificar-perfil/modificar-perfil';
import { RecuperarPassword } from './recuperar-password/recuperar-password';
import { RegistroPaciente } from './registro-paciente/registro-paciente';
import { roleGuard } from './core/role.guard';

export const routes: Routes = [
  { path: '', component: Landing },
  { path: 'login', component: Login },
  { path: 'registro-paciente', component: RegistroPaciente },
  { path: 'recuperar-password', component: RecuperarPassword },

  { path: 'dashboard-paciente', component: DashboardPaciente, canActivate: [roleGuard], data: { roles: ['paciente'] } },
  { path: 'dashboard-invitado', component: DashboardInvitado, canActivate: [roleGuard], data: { roles: ['invitado'] } },
  { path: 'agenda', component: Agenda, canActivate: [roleGuard], data: { roles: ['paciente', 'invitado'] } },
  { path: 'mis-citas', component: MisCitas, canActivate: [roleGuard], data: { roles: ['paciente', 'invitado'] } },
  { path: 'ficha-clinica', component: FichaClinica, canActivate: [roleGuard], data: { roles: ['paciente'] } },
  { path: 'modificar-perfil', component: ModificarPerfil, canActivate: [roleGuard], data: { roles: ['paciente'] } },

  { path: 'dashboard-doctor', component: DashboardDoctor, canActivate: [roleGuard], data: { roles: ['doctor'] } },
  { path: 'dashboard-doctor/agenda', component: DashboardDoctorAgenda, canActivate: [roleGuard], data: { roles: ['doctor'] } },
  { path: 'dashboard-doctor/pacientes', component: DashboardDoctorPacientes, canActivate: [roleGuard], data: { roles: ['doctor'] } },
  { path: 'dashboard-doctor/fichas', component: DashboardDoctorFichas, canActivate: [roleGuard], data: { roles: ['doctor'] } },
  { path: 'dashboard-doctor/atencion', component: DashboardDoctorAtencion, canActivate: [roleGuard], data: { roles: ['doctor'] } },

  { path: 'dashboard-admin', component: DashboardAdmin, canActivate: [roleGuard], data: { roles: ['admin'] } },
  { path: '**', redirectTo: '' },
];
