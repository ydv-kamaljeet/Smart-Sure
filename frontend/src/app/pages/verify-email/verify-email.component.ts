import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="auth-bg">
      <div class="auth-card mx-3 text-center">
        <div class="mx-auto mb-3 d-flex align-items-center justify-content-center"
             style="width:60px;height:60px;border-radius:16px;background:linear-gradient(135deg,#1a56db,#0d9488);">
          <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2.5">
            <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
          </svg>
        </div>

        <!-- Loading -->
        <div *ngIf="status === 'loading'" class="py-3">
          <div class="spinner-border text-primary mb-3"></div>
          <p class="text-muted">Verifying your email...</p>
        </div>

        <!-- Success -->
        <div *ngIf="status === 'success'" class="py-3">
          <i class="bi bi-check-circle-fill text-success" style="font-size:3rem;"></i>
          <h5 class="mt-3 fw-bold">Email verified!</h5>
          <p class="text-muted small">Your account is now active. You can sign in.</p>
          <a routerLink="/login" class="btn btn-gradient mt-2 px-4">Sign in</a>
        </div>

        <!-- Error -->
        <div *ngIf="status === 'error'" class="py-3">
          <i class="bi bi-x-circle-fill text-danger" style="font-size:3rem;"></i>
          <h5 class="mt-3 fw-bold">Verification failed</h5>
          <p class="text-muted small">{{ errorMessage }}</p>
          <a routerLink="/login" class="btn btn-gradient mt-2 px-4">Back to Sign in</a>
        </div>
      </div>
    </div>
  `
})
export class VerifyEmailComponent implements OnInit {
  status: 'loading' | 'success' | 'error' = 'loading';
  errorMessage = 'The link may be invalid or expired. Please request a new one.';

  constructor(private route: ActivatedRoute, private http: HttpClient) {}

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.status = 'error';
      return;
    }
    this.http.get(`/api/auth/verify-email?token=${token}`).subscribe({
      next: () => this.status = 'success',
      error: (err) => {
        this.errorMessage = err?.error?.errorMessage ?? this.errorMessage;
        this.status = 'error';
      }
    });
  }
}
