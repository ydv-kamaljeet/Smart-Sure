import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  // Attach access token to every outgoing request
  const token = auth.getToken();
  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      // If 401 and we have a refresh token — try to silently refresh the access token
      // Skip refresh for auth endpoints to avoid infinite loops
      if (error.status === 401 && auth.getRefreshToken() && 
          !req.url.includes('/api/auth/refresh') &&
          !req.url.includes('/api/auth/login') &&
          !req.url.includes('/api/auth/register')) {
        return auth.refreshAccessToken().pipe(
          switchMap(res => {
            // Retry the original request with the new access token
            const retried = req.clone({ setHeaders: { Authorization: `Bearer ${res.accessToken}` } });
            return next(retried);
          }),
          catchError(refreshError => {
            // Refresh token also failed (expired or revoked) — force logout
            auth.logout();
            router.navigate(['/login']);
            return throwError(() => refreshError);
          })
        );
      }

      return throwError(() => error);
    })
  );
};
