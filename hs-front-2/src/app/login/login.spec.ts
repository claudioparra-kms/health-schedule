import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { AuthService } from '../core/auth.service';
import { SesionResponse } from '../core/models';
import { Login } from './login';

const sesionPaciente: SesionResponse = {
  token: 'token-prueba',
  usuario: {
    id: 8,
    nombre: 'Gunther Sotelo',
    rut: '19264452-6',
    correo: 'gunther@correo.cl',
    rol: 'paciente',
    pacienteId: 5,
    doctorId: null,
    especialidad: null,
  },
};

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let component: Login;
  let router: Router;
  let authMock: {
    login: ReturnType<typeof vi.fn>;
    ingresarInvitado: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    authMock = {
      login: vi.fn(),
      ingresarInvitado: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.detectChanges();
  });

  it('crea el componente', () => {
    expect(component).toBeTruthy();
  });

  it('no llama al backend cuando el RUT es invalido', () => {
    component.rut = '12345678-9';
    component.password = 'paciente123';

    component.login();

    expect(authMock.login).not.toHaveBeenCalled();
    expect(component.mensajeError).toContain('RUT chileno válido');
  });

  it('no llama al backend cuando la contraseña tiene menos de 8 caracteres', () => {
    component.rut = '12345678-5';
    component.password = '1234';

    component.login();

    expect(authMock.login).not.toHaveBeenCalled();
    expect(component.mensajeError).toContain('al menos 8 caracteres');
  });

  it('normaliza el RUT, inicia sesion y redirige al panel del paciente', () => {
    authMock.login.mockReturnValue(of(sesionPaciente));
    component.rut = '19.264.452-6';
    component.password = 'paciente123';

    component.login();

    expect(authMock.login).toHaveBeenCalledWith('19264452-6', 'paciente123');
    expect(router.navigate).toHaveBeenCalledWith(['/dashboard-paciente']);
    expect(component.cargando).toBe(false);
    expect(component.mensajeError).toBe('');
  });

  it('muestra el mensaje entregado por el backend cuando el login falla', () => {
    authMock.login.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 401,
            error: { mensaje: 'RUT o contraseña incorrectos.' },
          }),
      ),
    );
    component.rut = '19264452-6';
    component.password = 'incorrecta';

    component.login();

    expect(component.cargando).toBe(false);
    expect(component.mensajeError).toBe('RUT o contraseña incorrectos.');
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('permite ingresar como invitado con un RUT valido', () => {
    authMock.ingresarInvitado.mockReturnValue(of(sesionPaciente));
    component.rutInvitado = '12.345.678-5';

    component.guestLogin();

    expect(authMock.ingresarInvitado).toHaveBeenCalledWith('12345678-5');
    expect(router.navigate).toHaveBeenCalledWith(['/dashboard-invitado']);
    expect(component.cargandoInvitado).toBe(false);
  });
});
