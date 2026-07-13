import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { AuthService } from '../core/auth.service';
import { HealthService } from '../core/health.service';
import { CitaDoctor, ResumenDoctor } from '../core/models';
import { DashboardDoctor } from './dashboard-doctor';

describe('DashboardDoctor', () => {
  let fixture: ComponentFixture<DashboardDoctor>;
  let resumen$: Subject<ResumenDoctor>;
  let agenda$: Subject<CitaDoctor[]>;

  beforeEach(async () => {
    resumen$ = new Subject<ResumenDoctor>();
    agenda$ = new Subject<CitaDoctor[]>();

    await TestBed.configureTestingModule({
      imports: [DashboardDoctor],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            usuario: { nombre: 'Doctor Demo', especialidad: 'Cardiología' },
            logout: () => new Subject<void>().asObservable(),
          },
        },
        {
          provide: HealthService,
          useValue: {
            getResumenDoctor: () => resumen$.asObservable(),
            getAgendaDoctor: () => agenda$.asObservable(),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardDoctor);
    fixture.detectChanges();
  });

  it('muestra el nombre del doctor y el resumen', async () => {
    expect(fixture.nativeElement.textContent).toContain('Cargando agenda profesional…');

    resumen$.next({ citasHoy: 2, proximas: 1, pendientes: 1, pacientes: 3 });
    agenda$.next([]);
    resumen$.complete();
    agenda$.complete();

    await fixture.whenStable();

    const contenido = fixture.nativeElement.textContent as string;
    expect(contenido).toContain('Doctor Demo');
    expect(contenido).toContain('Citas de hoy');
    expect(contenido).toContain('2');
  });
});