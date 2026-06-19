import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
@Component({selector:'app-dashboard-invitado', standalone:true, imports:[RouterLink], templateUrl:'./dashboard-invitado.html', styleUrls:['./dashboard-invitado.css']})
export class DashboardInvitado {
  constructor(){
    const usuario = JSON.parse(localStorage.getItem('usuario') || '{}');
    if(usuario?.rol !== 'invitado'){
      localStorage.setItem('usuario', JSON.stringify({rol:'invitado', nombre:'Invitado'}));
    }
  }
}
