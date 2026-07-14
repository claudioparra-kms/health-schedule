import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { AuthService } from '../core/auth.service';
import { SesionResponse } from '../core/models';
import { RegistroPaciente } from './registro-paciente';

const sesionCreada: SesionResponse = {
  token: 'token-registro',
  usuario: {
    id: 9,
    nombre: 'Nuevo Paciente',
    rut: '19264452-6',
    correo: 'nuevo@correo.cl',
    rol: 'paciente',
    pacienteId: 6,
    doctorId: null,
    especialidad: null,
  },
};

describe('RegistroPaciente', () => {
  let fixture: ComponentFixture<RegistroPaciente>;
  let component: RegistroPaciente;
  let router: Router;
  let authMock: { registrar: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    authMock = { registrar: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [RegistroPaciente],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(RegistroPaciente);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.detectChanges();
  });

  function completarFormularioValido(): void {
    component.rut = '19.264.452-6';
    component.nombre = '  Nuevo Paciente  ';
    component.correo = '  nuevo@correo.cl  ';
    component.telefono = '  +56912345678  ';
    component.password = 'segura123';
    component.confirmarPassword = 'segura123';
  }

  it('crea el componente', () => {
    expect(component).toBeTruthy();
  });

  it('rechaza un RUT invalido antes de llamar al backend', () => {
    completarFormularioValido();
    component.rut = '19264452-1';

    component.registrar();

    expect(authMock.registrar).not.toHaveBeenCalled();
    expect(component.mensajeError).toBe('El RUT ingresado no es válido.');
  });

  it('exige nombre y apellido', () => {
    completarFormularioValido();
    component.nombre = 'Gunther';

    component.registrar();

    expect(authMock.registrar).not.toHaveBeenCalled();
    expect(component.mensajeError).toBe('Ingresa tu nombre y apellido.');
  });

  it('rechaza un correo con formato invalido', () => {
    completarFormularioValido();
    component.correo = 'correo-invalido';

    component.registrar();

    expect(authMock.registrar).not.toHaveBeenCalled();
    expect(component.mensajeError).toBe('Ingresa un correo válido.');
  });

  it('rechaza contraseñas distintas', () => {
    completarFormularioValido();
    component.confirmarPassword = 'otraClave123';

    component.registrar();

    expect(authMock.registrar).not.toHaveBeenCalled();
    expect(component.mensajeError).toBe('Las contraseñas no coinciden.');
  });

  it('envia los datos normalizados y redirige al panel cuando el registro es correcto', () => {
    authMock.registrar.mockReturnValue(of(sesionCreada));
    completarFormularioValido();

    component.registrar();

    expect(authMock.registrar).toHaveBeenCalledWith({
      rut: '19264452-6',
      nombre: 'Nuevo Paciente',
      correo: 'nuevo@correo.cl',
      telefono: '+56912345678',
      password: 'segura123',
    });
    expect(router.navigate).toHaveBeenCalledWith(['/dashboard-paciente']);
    expect(component.cargando).toBe(false);
  });

  it('muestra el error del backend cuando el RUT o correo ya existe', () => {
    authMock.registrar.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { mensaje: 'El RUT o correo ya se encuentra registrado.' },
          }),
      ),
    );
    completarFormularioValido();

    component.registrar();

    expect(component.cargando).toBe(false);
    expect(component.mensajeError).toBe('El RUT o correo ya se encuentra registrado.');
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
