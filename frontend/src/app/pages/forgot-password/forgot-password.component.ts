import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

type Step = 'email' | 'otp' | 'reset' | 'done';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [FormsModule, CommonModule, RouterLink],
  template: `
    <div class="auth-bg">
      <div class="auth-card mx-3">
        <div class="text-center mb-4">
          <div class="mx-auto mb-3 d-flex align-items-center justify-content-center"
               style="width:60px;height:60px;border-radius:16px;background:linear-gradient(135deg,#1a56db,#0d9488);">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2.5">
              <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
            </svg>
          </div>
          <h2 class="fw-bold mb-1" style="font-size:1.5rem;">Reset password</h2>
          <p class="text-muted small">
            <ng-container [ngSwitch]="step">
              <span *ngSwitchCase="'email'">Enter your email to receive an OTP</span>
              <span *ngSwitchCase="'otp'">Enter the OTP sent to your email</span>
              <span *ngSwitchCase="'reset'">Enter your new password</span>
            </ng-container>
          </p>
        </div>

        <!-- Done -->
        <div *ngIf="step === 'done'" class="text-center py-3">
          <i class="bi bi-check-circle-fill text-success" style="font-size:3rem;"></i>
          <h5 class="mt-3 fw-bold">Password reset!</h5>
          <p class="text-muted small">You can now sign in with your new password.</p>
          <a routerLink="/login" class="btn btn-gradient mt-2 px-4">Back to Sign In</a>
        </div>

        <ng-container *ngIf="step !== 'done'">
          <div *ngIf="error" class="alert alert-danger py-2 px-3 mb-3">
            <i class="bi bi-exclamation-circle me-2"></i>{{ error }}
          </div>

          <!-- Step 1: Email -->
          <form *ngIf="step === 'email'" (ngSubmit)="sendOtp()">
            <div class="mb-3">
              <label class="form-label fw-medium small">Email address</label>
              <input type="email" class="form-control" [(ngModel)]="email" name="email" required placeholder="you@example.com">
            </div>
            <button type="submit" class="btn btn-gradient w-100 py-2" [disabled]="loading">
              <span *ngIf="loading" class="spinner-border spinner-border-sm me-2"></span>
              {{ loading ? 'Sending...' : 'Send OTP' }}
            </button>
          </form>

          <!-- Step 2: OTP -->
          <form *ngIf="step === 'otp'" (ngSubmit)="verifyOtp()">
            <div class="mb-3">
              <label class="form-label fw-medium small">OTP Code</label>
              <input type="text" class="form-control" [(ngModel)]="otpCode" name="otpCode" required
                     placeholder="Enter 6-digit OTP" maxlength="6" style="letter-spacing:0.2em;font-size:1.2rem;">
              <div class="form-text">Sent to <strong>{{ email }}</strong></div>
            </div>
            <button type="submit" class="btn btn-gradient w-100 py-2" [disabled]="loading">
              <span *ngIf="loading" class="spinner-border spinner-border-sm me-2"></span>
              {{ loading ? 'Verifying...' : 'Verify OTP' }}
            </button>
            <button type="button" class="btn btn-link w-100 mt-2 small" (click)="step = 'email'">
              &larr; Back
            </button>
          </form>

          <!-- Step 3: New Password -->
          <form *ngIf="step === 'reset'" (ngSubmit)="resetPassword()">
            <div class="mb-3">
              <label class="form-label fw-medium small">New Password</label>
              <input type="password" class="form-control" [(ngModel)]="newPassword" name="newPassword"
                     required minlength="6" placeholder="Min. 6 characters">
            </div>
            <div class="mb-3">
              <label class="form-label fw-medium small">Confirm Password</label>
              <input type="password" class="form-control" [(ngModel)]="confirmPassword" name="confirmPassword"
                     required placeholder="Repeat new password">
            </div>
            <button type="submit" class="btn btn-gradient w-100 py-2" [disabled]="loading">
              <span *ngIf="loading" class="spinner-border spinner-border-sm me-2"></span>
              {{ loading ? 'Resetting...' : 'Reset Password' }}
            </button>
          </form>
        </ng-container>

        <p *ngIf="step === 'email'" class="text-center small text-muted mt-4 mb-0">
          Remember your password?
          <a routerLink="/login" class="text-primary fw-semibold text-decoration-none">Sign in</a>
        </p>
      </div>
    </div>
  `
})
export class ForgotPasswordComponent {
  step: Step = 'email';
  email = '';
  otpCode = '';
  newPassword = '';
  confirmPassword = '';
  resetToken = '';
  loading = false;
  error = '';

  constructor(private auth: AuthService) {}

  sendOtp(): void {
    this.error = '';
    this.loading = true;
    this.auth.forgotPassword(this.email).subscribe({
      next: () => { this.step = 'otp'; this.loading = false; },
      error: (err) => { this.error = err?.error?.message ?? 'Failed to send OTP.'; this.loading = false; }
    });
  }

  verifyOtp(): void {
    this.error = '';
    this.loading = true;
    this.auth.verifyOtp(this.email, this.otpCode).subscribe({
      next: (res) => { this.resetToken = res.resetToken; this.step = 'reset'; this.loading = false; },
      error: (err) => { this.error = err?.error?.message ?? 'Invalid or expired OTP.'; this.loading = false; }
    });
  }

  resetPassword(): void {
    if (this.newPassword !== this.confirmPassword) {
      this.error = 'Passwords do not match.';
      return;
    }
    this.error = '';
    this.loading = true;
    this.auth.resetPassword(this.email, this.newPassword, this.resetToken).subscribe({
      next: () => { this.step = 'done'; this.loading = false; },
      error: (err) => { this.error = err?.error?.message ?? 'Failed to reset password.'; this.loading = false; }
    });
  }
}
