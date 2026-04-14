import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AuthResponse, User } from '../models/models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'auth_token';
  private readonly REFRESH_TOKEN_KEY = 'auth_refresh_token';
  private readonly USER_KEY = 'auth_user';

  constructor(private http: HttpClient) {}

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/login', { email, password }).pipe(
      tap(res => this.storeAuth(res))
    );
  }

  register(payload: { fullName: string; email: string; password: string }): Observable<any> {
    return this.http.post<any>('/api/auth/register', payload);
  }

  resendVerification(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`/api/auth/resend-verification?email=${email}`, {});
  }

  forgotPassword(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`/api/auth/forgot-password?email=${email}`, {});
  }

  verifyOtp(email: string, otpCode: string): Observable<{ resetToken: string }> {
    return this.http.post<{ resetToken: string }>('/api/auth/verify-otp', { email, otpCode });
  }

  resetPassword(email: string, newPassword: string, resetToken: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>('/api/auth/reset-password', { email, newPassword }, {
      headers: { Authorization: `Bearer ${resetToken}` }
    });
  }

  // Calls the refresh endpoint with the stored refresh token — returns a new access token
  refreshAccessToken(): Observable<{ accessToken: string }> {
    const refreshToken = this.getRefreshToken();
    return this.http.post<{ accessToken: string }>('/api/auth/refresh', { refreshToken }).pipe(
      tap(res => localStorage.setItem(this.TOKEN_KEY, res.accessToken))
    );
  }

  getProfile(): Observable<User> {
    return this.http.get<User>('/api/auth/me');
  }

  updateProfile(payload: Partial<User>): Observable<User> {
    return this.http.put<User>('/api/auth/me', payload);
  }

  changePassword(currentPassword: string, newPassword: string): Observable<{ message: string }> {
    return this.http.put<{ message: string }>('/api/auth/change-password', { currentPassword, newPassword });
  }

  storeAuth(res: AuthResponse): void {
    localStorage.setItem(this.TOKEN_KEY, res.accessToken);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, res.refreshToken);
    localStorage.setItem(this.USER_KEY, JSON.stringify({
      userId: '',
      fullName: res.fullName,
      email: res.email,
      roles: res.roles,
      phone: res.phone,
      address: res.address
    }));
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  getUser(): User | null {
    const u = localStorage.getItem(this.USER_KEY);
    return u ? JSON.parse(u) as User : null;
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  getUserInitials(): string {
    const u = this.getUser();
    if (!u || !u.fullName) return '';
    return u.fullName.split(' ').map(n => n[0]).join('').toUpperCase();
  }

  getUserFullName(): string {
    const u = this.getUser();
    return u?.fullName ?? '';
  }
}
