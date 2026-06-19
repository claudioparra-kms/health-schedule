import { Routes } from '@angular/router';
import { Landing } from './landing/landing';
import { Login } from './login/login';
import { RegistroPaciente } from './registro-paciente/registro-paciente';
import { RecuperarPassword } from './recuperar-password/recuperar-password';
import { DashboardPaciente } from './dashboard-paciente/dashboard-paciente';
import { DashboardDoctor } from './dashboard-doctor/dashboard-doctor';
import { DashboardAdmin } from './dashboard-admin/dashboard-admin';
import { DashboardInvitado } from './dashboard-invitado/dashboard-invitado';
import { Agenda } from './agenda/agenda';
import { MisCitas } from './mis-citas/mis-citas';
import { FichaClinica } from './ficha-clinica/ficha-clinica';
import { ModificarPerfil } from './modificar-perfil/modificar-perfil';

import { DashboardDoctorAgenda } from './dashboard-doctor-agenda/dashboard-doctor-agenda';
import { DashboardDoctorPacientes } from './dashboard-doctor-pacientes/dashboard-doctor-pacientes';
import { DashboardDoctorFichas } from './dashboard-doctor-fichas/dashboard-doctor-fichas';
import { DashboardDoctorAtencion } from './dashboard-doctor-atencion/dashboard-doctor-atencion';

export const routes: Routes = [
  { path: '', component: Landing },
  { path: 'login', component: Login },
  { path: 'registro-paciente', component: RegistroPaciente },
  { path: 'recuperar-password', component: RecuperarPassword },
  { path: 'dashboard-paciente', component: DashboardPaciente },
  { path: 'dashboard-doctor', component: DashboardDoctor },
  { path: 'dashboard-admin', component: DashboardAdmin },
  { path: 'dashboard-invitado', component: DashboardInvitado },
  { path: 'agenda', component: Agenda },
  { path: 'mis-citas', component: MisCitas },
  { path: 'ficha-clinica', component: FichaClinica },
  { path: 'modificar-perfil', component: ModificarPerfil },
  { path: 'dashboard-doctor/agenda', component: DashboardDoctorAgenda },
  { path: 'dashboard-doctor/pacientes', component: DashboardDoctorPacientes },
  { path: 'dashboard-doctor/fichas', component: DashboardDoctorFichas },
  { path: 'dashboard-doctor/atencion', component: DashboardDoctorAtencion },
  { path: '**', redirectTo: '' }
];
