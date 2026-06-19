import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
@Component({selector:'app-dashboard-paciente', standalone:true, imports:[RouterLink], templateUrl:'./dashboard-paciente.html', styleUrls:['./dashboard-paciente.css']})
export class DashboardPaciente { usuario:any = JSON.parse(localStorage.getItem('usuario') || '{}'); }
