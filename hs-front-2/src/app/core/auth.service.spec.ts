import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom, of, throwError } from 'rxjs';
import {
  afterEach,
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from 'vitest';

import { API_URL } from './api.config';
import { AuthService } from './auth.service';
import { SesionResponse } from './models';

const STORAGE_KEY = 'hs_session';

/*
 * Cuenta simulada utilizada por las pruebas.
 *
 * Nombre: Camila González
 * RUT: 12345678-5
 * Correo: paciente@hs.local
 * Contraseña del login simulado: paciente123
 */
const sesionCamila: SesionResponse = {
  token: 'token-paciente-camila',
  usuario: {
    id: 1,
    nombre: 'Camila González',
    rut: '12345678-5',
    correo: 'paciente@hs.local',
    rol: 'paciente',
    pacienteId: 1,
    doctorId: null,
    especialidad: null,
  },
};

/*
 * Crea una implementación completa de localStorage en memoria.
 * De esta forma no dependemos del localStorage proporcionado
 * por Node, jsdom o el navegador de pruebas.
 */
function crearLocalStorageMock(): Storage {
  const datos = new Map<string, string>();

  return {
    get length(): number {
      return datos.size;
    },

    clear: vi.fn((): void => {
      datos.clear();
    }),

    getItem: vi.fn((clave: string): string | null => {
      return datos.get(clave) ?? null;
    }),

    key: vi.fn((indice: number): string | null => {
      return Array.from(datos.keys())[indice] ?? null;
    }),

    removeItem: vi.fn((clave: string): void => {
      datos.delete(clave);
    }),

    setItem: vi.fn((clave: string, valor: string): void => {
      datos.set(clave, String(valor));
    }),
  } as Storage;
}

describe('AuthService', () => {
  let service: AuthService;
  let postMock: ReturnType<typeof vi.fn>;
  let storageMock: Storage;

  beforeEach(() => {
    /*
     * Reemplazamos el localStorage defectuoso del entorno
     * antes de construir AuthService, porque el servicio lee
     * la sesión durante su inicialización.
     */
    storageMock = crearLocalStorageMock();
    vi.stubGlobal('localStorage', storageMock);

    postMock = vi.fn();

    const httpClientMock = {
      post: postMock,
    } as unknown as HttpClient;

    service = new AuthService(httpClientMock);
  });

  afterEach(() => {
    /*
     * Restaura los objetos globales después de cada prueba.
     * No llamamos localStorage.clear() ni removeItem() aquí.
     */
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it('envia el login por POST y guarda la sesion recibida', async () => {
    postMock.mockReturnValue(of(sesionCamila));

    const respuesta = await firstValueFrom(
      service.login('12345678-5', 'paciente123'),
    );

    expect(postMock).toHaveBeenCalledWith(
      `${API_URL}/auth/login`,
      {
        rut: '12345678-5',
        password: 'paciente123',
      },
    );

    expect(respuesta).toEqual(sesionCamila);
    expect(service.session).toEqual(sesionCamila);
    expect(service.usuario?.nombre).toBe('Camila González');
    expect(service.usuario?.rut).toBe('12345678-5');
    expect(service.token).toBe('token-paciente-camila');

    const sesionGuardada = storageMock.getItem(STORAGE_KEY);

    expect(sesionGuardada).not.toBeNull();
    expect(JSON.parse(sesionGuardada ?? '{}')).toEqual(sesionCamila);
  });

  it('envia el registro por POST y deja iniciada la sesion', async () => {
    const payload = {
      rut: '12345678-5',
      nombre: 'Camila González',
      correo: 'paciente@hs.local',
      telefono: '+56912345678',
      password: 'paciente123',
    };

    postMock.mockReturnValue(of(sesionCamila));

    const respuesta = await firstValueFrom(
      service.registrar(payload),
    );

    expect(postMock).toHaveBeenCalledWith(
      `${API_URL}/auth/registro`,
      payload,
    );

    expect(respuesta).toEqual(sesionCamila);
    expect(service.session).toEqual(sesionCamila);
    expect(service.usuario?.correo).toBe('paciente@hs.local');
    expect(service.hasRole('paciente')).toBe(true);
    expect(service.hasRole('doctor')).toBe(false);
    expect(service.hasRole('admin')).toBe(false);

    expect(storageMock.getItem(STORAGE_KEY)).not.toBeNull();
  });

  it('elimina la sesion local al cerrar sesion', async () => {
    postMock.mockReturnValueOnce(of(sesionCamila));

    await firstValueFrom(
      service.login('12345678-5', 'paciente123'),
    );

    expect(service.session).toEqual(sesionCamila);
    expect(storageMock.getItem(STORAGE_KEY)).not.toBeNull();

    postMock.mockReturnValueOnce(
      of({
        mensaje: 'Sesion cerrada correctamente',
      }),
    );

    await firstValueFrom(service.logout());

    expect(postMock).toHaveBeenLastCalledWith(
      `${API_URL}/auth/logout`,
      {},
    );

    expect(service.session).toBeNull();
    expect(service.usuario).toBeNull();
    expect(service.token).toBeNull();
    expect(storageMock.getItem(STORAGE_KEY)).toBeNull();
  });

  it('tambien limpia la sesion cuando el backend falla durante el logout', async () => {
    postMock.mockReturnValueOnce(of(sesionCamila));

    await firstValueFrom(
      service.login('12345678-5', 'paciente123'),
    );

    expect(service.session).toEqual(sesionCamila);

    const errorBackend = new HttpErrorResponse({
      status: 500,
      statusText: 'Internal Server Error',
      error: {
        mensaje: 'Error interno del backend',
      },
    });

    postMock.mockReturnValueOnce(
      throwError(() => errorBackend),
    );

    await expect(
      firstValueFrom(service.logout()),
    ).rejects.toBeInstanceOf(HttpErrorResponse);

    /*
     * AuthService utiliza finalize(), por lo que limpia
     * la sesión incluso si el logout responde con error.
     */
    expect(service.session).toBeNull();
    expect(service.usuario).toBeNull();
    expect(service.token).toBeNull();
    expect(storageMock.getItem(STORAGE_KEY)).toBeNull();
  });
});