import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-dashboard-paciente',
  standalone: true,
  imports: [RouterLink, CommonModule],
  templateUrl: './dashboard-paciente.html',
  styleUrls: ['./dashboard-paciente.css']
})
export class DashboardPaciente implements OnInit {
  nombre = localStorage.getItem('nombre') || 'Paciente';
  citas: any[] = [];

  constructor(private http: HttpClient) {}

  ngOnInit() {
    const pacienteId = localStorage.getItem('paciente_id');
    if (!pacienteId) return;

    this.http.get<any[]>(`http://localhost:5220/Citas/Paciente/${pacienteId}`)
      .subscribe({
        next: (data) => this.citas = data,
        error: (err) => console.log('Error al cargar citas:', err)
      });
  }

  get citasProximas(): number {
    const ahora = new Date();
    return this.citas.filter(c =>
      new Date(c.fechaInicio) >= ahora &&
      c.estado !== 'cancelada' &&
      c.estado !== 'no_asiste'
    ).length;
  }

  get nextAppointment(): any | null {
    const ahora = new Date();
    const proximas = this.citas
      .map(c => ({ ...c, _fecha: new Date(c.fechaInicio) }))
      .filter(c => c._fecha >= ahora && c.estado !== 'cancelada' && c.estado !== 'no_asiste')
      .sort((a, b) => a._fecha.getTime() - b._fecha.getTime());

    return proximas.length ? proximas[0] : null;
  }
}