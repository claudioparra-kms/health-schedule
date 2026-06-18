import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
@Component({selector:'app-dashboard-doctor',
    standalone:true,
    imports:[RouterLink],
    templateUrl:'./dashboard-doctor.html',
    styleUrls:['./dashboard-doctor.css']})
export class DashboardDoctor {
    usuario: any = JSON.parse(localStorage.getItem('usuario') || '{}'); }
