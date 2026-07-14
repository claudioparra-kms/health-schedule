import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject, of } from 'rxjs';
import { HealthService } from '../core/health.service';
import { CitaDoctor } from '../core/models';
import { DashboardDoctorAgenda } from './dashboard-doctor-agenda';

describe('DashboardDoctorAgenda', () => {
  let fixture: ComponentFixture<DashboardDoctorAgenda>;
  let agenda$: Subject<CitaDoctor[]>;
  let cambiarEstado$: Subject<{ mensaje: string }>;

  beforeEach(async () => {
    agenda$ = new Subject<CitaDoctor[]>();
    cambiarEstado$ = new Subject<{ mensaje: string }>();

    await TestBed.configureTestingModule({
      imports: [DashboardDoctorAgenda],
      providers: [
        provideRouter([]),
        {
          provide: HealthService,
          useValue: {
            getAgendaDoctor: () => agenda$.asObservable(),
            cambiarEstadoCita: () => cambiarEstado$.asObservable(),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardDoctorAgenda);
    fixture.detectChanges();
  });

  it('muestra la agenda y permite cambiar el estado de una cita', async () => {
    expect(fixture.nativeElement.textContent).toContain('Cargando agenda…');

    agenda$.next([
      {
        id: 7,
        fechaInicio: '2026-07-13T10:00:00',
        fechaFin: '2026-07-13T10:30:00',
        estado: 'pendiente',
        motivo: 'Control cardiaco',
        pacienteId: 1,
        paciente: 'Camila González',
        rutPaciente: '12345678-5',
      },
    ]);
    agenda$.complete();

    await fixture.whenStable();

    let contenido = fixture.nativeElement.textContent as string;
    expect(contenido).toContain('Camila González');
    expect(contenido).toContain('Control cardiaco');

    const component = fixture.componentInstance;
    component.cambiarEstado(component.citas[0], 'confirmada');
    cambiarEstado$.next({ mensaje: 'Cita confirmada' });
    cambiarEstado$.complete();

    fixture.detectChanges();
    contenido = fixture.nativeElement.textContent as string;
    expect(contenido).toContain('Cita confirmada');
    expect(component.citas[0].estado).toBe('confirmada');
  });
});
