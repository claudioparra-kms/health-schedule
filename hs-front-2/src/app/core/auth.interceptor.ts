import { HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  let token: string | null = null;
  try {
    const raw = localStorage.getItem('hs_session');
    token = raw ? (JSON.parse(raw) as { token?: string }).token ?? null : null;
  } catch {
    token = null;
  }

  const authenticatedRequest = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(authenticatedRequest).pipe(
    catchError((error: unknown) => {
      const status = (error as { status?: number }).status;
      if (status === 401 && token) {
        localStorage.removeItem('hs_session');
        window.location.assign('/login');
      }
      return throwError(() => error);
    }),
  );
};
