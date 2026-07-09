import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../core/auth.service';
import { HealthService } from '../core/health.service';
import { DashboardPaciente } from './dashboard-paciente';

describe('DashboardPaciente', () => {
  let component: DashboardPaciente;
  let fixture: ComponentFixture<DashboardPaciente>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardPaciente],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            session: { nombre: 'Paciente Demo', rol: 'paciente' },
            logout: () => of(void 0),
          },
        },
        {
          provide: HealthService,
          useValue: {
            getPerfil: () => of({ nombre: 'Paciente Demo', correo: 'demo@correo.cl' }),
            getMisCitas: () => of([]),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardPaciente);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
