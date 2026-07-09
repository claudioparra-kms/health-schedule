import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { HealthService } from '../core/health.service';
import { PacienteDoctor } from '../core/models';
import { DashboardDoctorPacientes } from './dashboard-doctor-pacientes';

describe('DashboardDoctorPacientes', () => {
  let fixture: ComponentFixture<DashboardDoctorPacientes>;
  let pacientes$: Subject<PacienteDoctor[]>;

  beforeEach(async () => {
    pacientes$ = new Subject<PacienteDoctor[]>();

    await TestBed.configureTestingModule({
      imports: [DashboardDoctorPacientes],
      providers: [
        provideRouter([]),
        {
          provide: HealthService,
          useValue: {
            getPacientesDoctor: () => pacientes$.asObservable(),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardDoctorPacientes);
    fixture.detectChanges();
  });

  it('muestra los pacientes apenas termina la respuesta HTTP, sin un segundo clic', async () => {
    expect(fixture.nativeElement.textContent).toContain('Cargando pacientes');

    pacientes$.next([
      {
        id: 1,
        nombre: 'Camila González',
        rut: '12345678-5',
        correo: 'camila@demo.cl',
        telefono: '+56911111111',
        prevision: 'Fonasa',
        totalCitas: 2,
        ultimaCita: '2026-07-01T10:00:00',
        proximaCita: '2026-07-13T10:00:00',
        alergias: 'Alergia estacional',
      },
    ]);
    pacientes$.complete();

    await fixture.whenStable();

    const contenido = fixture.nativeElement.textContent as string;
    expect(contenido).not.toContain('Cargando pacientes');
    expect(contenido).toContain('Camila González');
    expect(contenido).toContain('1 paciente(s)');
  });
});
