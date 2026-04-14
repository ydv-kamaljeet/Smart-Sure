import { Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { LocaleService, COUNTRY_LOCALES } from '../../services/locale.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, CommonModule, RouterLink],
  template: `
    <div class="auth-bg">
      <div class="animated-bg">
        <i class="bi bi-shield-check float-icon icon-1"></i>
        <i class="bi bi-house float-icon icon-2"></i>
        <i class="bi bi-car-front float-icon icon-3"></i>
        <i class="bi bi-umbrella float-icon icon-4"></i>
        <i class="bi bi-heart float-icon icon-5"></i>
        <i class="bi bi-file-medical float-icon icon-6"></i>
        <i class="bi bi-shield float-icon icon-7"></i>
        <i class="bi bi-lightning float-icon icon-8"></i>
      </div>
      <div class="auth-card mx-3">
        <div class="text-center mb-4">
          <div class="mx-auto mb-3 d-flex align-items-center justify-content-center"
               style="width:60px;height:60px;border-radius:16px;background-color:#064e3b;">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2.5">
              <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
            </svg>
          </div>
          <h2 class="fw-bold mb-1" style="font-size:1.5rem;">Welcome back</h2>
          <p class="text-muted small">Sign in to your SmartSure account</p>
        </div>

        <!-- Generic error -->
        <div *ngIf="error && !emailNotVerified" class="alert alert-danger py-2 px-3 mb-3" role="alert">
          <i class="bi bi-exclamation-circle me-2"></i>{{ error }}
        </div>

        <!-- Email not verified banner -->
        <div *ngIf="emailNotVerified" class="alert alert-warning py-2 px-3 mb-3">
          <i class="bi bi-envelope-exclamation me-2"></i>
          Your email is not verified.
          <div class="mt-2">
            <button class="btn btn-sm btn-warning" [disabled]="resendLoading" (click)="resendVerification()">
              <span *ngIf="resendLoading" class="spinner-border spinner-border-sm me-1"></span>
              {{ resendLoading ? 'Sending...' : 'Resend verification email' }}
            </button>
          </div>
          <div *ngIf="resendSuccess" class="mt-2 small text-success fw-medium">
            <i class="bi bi-check-circle me-1"></i>Verification email sent — check your inbox.
          </div>
        </div>

        <form (ngSubmit)="onSubmit()" #f="ngForm">
          <div class="mb-3">
            <label class="form-label fw-medium small">Email address</label>
            <input type="email" class="form-control" [(ngModel)]="email" name="email" required
                   placeholder="you@example.com" autocomplete="email">
          </div>
          <div class="mb-3">
            <label class="form-label fw-medium small">Password</label>
            <div class="input-group">
              <input [type]="showPassword ? 'text' : 'password'" class="form-control"
                     [(ngModel)]="password" name="password" required
                     placeholder="••••••••" autocomplete="current-password">
              <button type="button" class="btn btn-outline-secondary" (click)="showPassword = !showPassword">
                <i [class]="showPassword ? 'bi bi-eye-slash' : 'bi bi-eye'"></i>
              </button>
            </div>
          </div>

          <!-- Country / Region picker -->
          <div class="mb-3">
            <label class="form-label fw-medium small">Country / Region</label>
            <div class="input-group">
              <span class="input-group-text fs-5 px-3">{{ selectedFlag }}</span>
              <select class="form-select" [(ngModel)]="selectedCountry" name="country" (ngModelChange)="onCountryChange($event)">
                <option *ngFor="let c of countries" [value]="c.country">{{ c.flag }} {{ c.country }}</option>
              </select>
            </div>
            <div class="mt-1 small text-muted">
              Currency: <strong>{{ selectedCurrency }}</strong> &nbsp;·&nbsp; Timezone: <strong>{{ selectedTimezone }}</strong>
            </div>
          </div>

          <div class="d-flex justify-content-end mb-3">
            <a routerLink="/forgot-password" class="small text-primary fw-medium text-decoration-none">
              Forgot password?
            </a>
          </div>
          <button type="submit" class="btn btn-gradient w-100 py-2" [disabled]="loading">
            <span *ngIf="loading" class="spinner-border spinner-border-sm me-2" role="status"></span>
            {{ loading ? 'Signing in...' : 'Sign in' }}
          </button>
        </form>

        <div class="d-flex align-items-center my-3">
          <hr class="flex-grow-1"><span class="px-2 text-muted small">or</span><hr class="flex-grow-1">
        </div>

        <a href="http://localhost:5001/api/auth/google" class="btn btn-outline-secondary w-100 py-2 d-flex align-items-center justify-content-center gap-2">
          <svg width="18" height="18" viewBox="0 0 48 48">
            <path fill="#EA4335" d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5z"/>
            <path fill="#4285F4" d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65z"/>
            <path fill="#FBBC05" d="M10.53 28.59c-.48-1.45-.76-2.99-.76-4.59s.27-3.14.76-4.59l-7.98-6.19C.92 16.46 0 20.12 0 24c0 3.88.92 7.54 2.56 10.78l7.97-6.19z"/>
            <path fill="#34A853" d="M24 48c6.48 0 11.93-2.13 15.89-5.81l-7.73-6c-2.18 1.48-4.97 2.31-8.16 2.31-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48z"/>
            <path fill="none" d="M0 0h48v48H0z"/>
          </svg>
          Continue with Google
        </a>

        <p class="text-center small text-muted mt-4 mb-0">
          Don't have an account?
          <a routerLink="/register" class="text-primary fw-semibold text-decoration-none">Create one</a>
        </p>
      </div>
    </div>
  `
})
export class LoginComponent implements OnInit {
  email = '';
  password = '';
  showPassword = false;
  loading = false;
  error = '';
  emailNotVerified = false;
  resendLoading = false;
  resendSuccess = false;

  countries = COUNTRY_LOCALES;
  selectedCountry = '';
  selectedFlag = '';
  selectedCurrency = '';
  selectedTimezone = '';

  constructor(private auth: AuthService, private router: Router, private locale: LocaleService) {}

  ngOnInit(): void {
    const current = this.locale.getLocale();
    this.selectedCountry = current.country;
    this.updateDisplay(current.country);
  }

  onCountryChange(country: string): void {
    this.locale.setCountry(country);
    this.updateDisplay(country);
  }

  private updateDisplay(country: string): void {
    const found = COUNTRY_LOCALES.find(c => c.country === country);
    if (found) {
      this.selectedFlag = found.flag;
      this.selectedCurrency = `${found.currencySymbol} (${found.currencyCode})`;
      this.selectedTimezone = found.timezone;
    }
  }

  onSubmit(): void {
    if (!this.email || !this.password) {
      this.error = 'Please enter your email and password.';
      return;
    }

    this.error = '';
    this.emailNotVerified = false;
    this.resendSuccess = false;
    this.loading = true;

    this.auth.login(this.email, this.password).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        const msg: string = err?.error?.errorMessage ?? err?.error?.message ?? 'Login failed. Please check your credentials.';
        if (msg.toLowerCase().includes('verify your email')) {
          this.emailNotVerified = true;
        } else {
          this.error = msg;
        }
        this.loading = false;
      }
    });
  }

  resendVerification(): void {
    this.resendLoading = true;
    this.resendSuccess = false;
    this.auth.resendVerification(this.email).subscribe({
      next: () => { this.resendSuccess = true; this.resendLoading = false; },
      error: () => { this.resendLoading = false; }
    });
  }
}
