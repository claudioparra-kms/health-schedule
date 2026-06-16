import { Routes } from '@angular/router';
import { Login } from './login/login';
import { DashboardAdmin } from './dashboard-admin/dashboard-admin';
import { DashboardDoctor } from './dashboard-doctor/dashboard-doctor';
import { DashboardPaciente } from './dashboard-paciente/dashboard-paciente';
import { DashboardInvitado } from './dashboard-invitado/dashboard-invitado';
import { RegistroPaciente } from './registro-paciente/registro-paciente';

export const routes: Routes = [
  { path: '', component: Login },
  { path: 'dashboard-admin', component: DashboardAdmin },
  { path: 'dashboard-doctor', component: DashboardDoctor },
  { path: 'dashboard-paciente', component: DashboardPaciente },
  { path: 'dashboard-invitado', component: DashboardInvitado },
  { path: 'registro-paciente', component: RegistroPaciente }
];