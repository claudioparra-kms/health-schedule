import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, finalize, tap } from 'rxjs';
import { API_URL } from './api.config';
import { Rol, SesionResponse, UsuarioSesion } from './models';

const STORAGE_KEY = 'hs_session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly sessionSubject = new BehaviorSubject<SesionResponse | null>(this.readSession());
  readonly session$ = this.sessionSubject.asObservable();

  constructor(private readonly http: HttpClient) {}

  get session(): SesionResponse | null {
    return this.sessionSubject.value;
  }

  get usuario(): UsuarioSesion | null {
    return this.session?.usuario ?? null;
  }

  get token(): string | null {
    return this.session?.token ?? null;
  }

  login(rut: string, password: string): Observable<SesionResponse> {
    return this.http
      .post<SesionResponse>(`${API_URL}/auth/login`, { rut, password })
      .pipe(tap((session) => this.saveSession(session)));
  }

  ingresarInvitado(rut: string): Observable<SesionResponse> {
    return this.http
      .post<SesionResponse>(`${API_URL}/auth/invitado`, { rut })
      .pipe(tap((session) => this.saveSession(session)));
  }

  registrar(payload: {
    rut: string;
    nombre: string;
    correo: string;
    telefono: string;
    password: string;
  }): Observable<SesionResponse> {
    return this.http
      .post<SesionResponse>(`${API_URL}/auth/registro`, payload)
      .pipe(tap((session) => this.saveSession(session)));
  }

  recuperarPassword(rutOCorreo: string): Observable<{
    mensaje: string;
    passwordTemporal: string;
    modoLocal: boolean;
  }> {
    return this.http.post<{
      mensaje: string;
      passwordTemporal: string;
      modoLocal: boolean;
    }>(`${API_URL}/auth/recuperar-password`, { rutOCorreo });
  }

  logout(): Observable<unknown> {
    return this.http
      .post(`${API_URL}/auth/logout`, {})
      .pipe(finalize(() => this.clearSession()));
  }

  clearSession(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.sessionSubject.next(null);
  }

  hasRole(...roles: Rol[]): boolean {
    const role = this.usuario?.rol;
    return role !== undefined && roles.includes(role);
  }

  private saveSession(session: SesionResponse): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    this.sessionSubject.next(session);
  }

  private readSession(): SesionResponse | null {
    try {
      const value = localStorage.getItem(STORAGE_KEY);
      return value ? (JSON.parse(value) as SesionResponse) : null;
    } catch {
      localStorage.removeItem(STORAGE_KEY);
      return null;
    }
  }
}
