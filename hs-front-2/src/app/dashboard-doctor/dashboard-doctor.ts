import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CommonModule } from '@angular/common';

@Component({
	selector: 'app-dashboard-doctor',
	standalone: true,
	imports: [RouterLink, CommonModule],
	templateUrl: './dashboard-doctor.html',
	styleUrls: ['./dashboard-doctor.css']
})
export class DashboardDoctor implements OnInit {
	usuario: any = JSON.parse(localStorage.getItem('usuario') || '{}');
	citas: any[] = [];

	constructor(private http: HttpClient) {}

	ngOnInit() {
		const doctorId = localStorage.getItem('doctor_id') || this.usuario?.id;
		if (!doctorId) return;

		this.http.get<any[]>(`http://localhost:5220/Citas/Doctor/${doctorId}`)
			.subscribe({
				next: data => this.citas = data,
				error: err => console.log('Error al cargar citas doctor:', err)
			});
	}

	get nombre(): string {
		return this.usuario?.nombre || localStorage.getItem('nombre') || 'Doctor';
	}

	private normalizeDates(list: any[]) {
		return list.map(c => ({ ...c, _fecha: new Date(c.fechaInicio) }));
	}

	get citasHoy(): any[] {
		const start = new Date(); start.setHours(0, 0, 0, 0);
		const end = new Date(start); end.setDate(end.getDate() + 1);
		return this.normalizeDates(this.citas)
			.filter(c => c._fecha >= start && c._fecha < end && c.estado !== 'cancelada' && c.estado !== 'no_asiste')
			.sort((a, b) => a._fecha.getTime() - b._fecha.getTime());
	}

	get citasHoyCount(): number {
		return this.citasHoy.length;
	}

	get porConfirmar(): any[] {
		return this.normalizeDates(this.citas)
			.filter(c => c.estado === 'pendiente')
			.sort((a, b) => a._fecha.getTime() - b._fecha.getTime());
	}

	get porConfirmarCount(): number {
		return this.porConfirmar.length;
	}
}
