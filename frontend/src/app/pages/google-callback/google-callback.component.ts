import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-google-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="auth-bg">
      <div class="auth-card mx-3 text-center">
        <div *ngIf="!error">
          <div class="spinner-border text-primary mb-3" role="status"></div>
          <p class="text-muted">Signing you in with Google...</p>
        </div>
        <div *ngIf="error" class="alert alert-danger">
          <i class="bi bi-exclamation-circle me-2"></i>{{ error }}
          <div class="mt-3">
            <a href="/login" class="btn btn-sm btn-outline-primary">Back to login</a>
          </div>
        </div>
      </div>
    </div>
  `
})
export class GoogleCallbackComponent implements OnInit {
  error = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;

    // Check for error redirect from login page
    const errorMsg = params.get('google_error');
    if (errorMsg) {
      this.error = decodeURIComponent(errorMsg);
      return;
    }

    const encoded = params.get('data');
    if (!encoded) {
      this.error = 'Invalid Google authentication response.';
      return;
    }

    try {
      const json = atob(decodeURIComponent(encoded));
      const data = JSON.parse(json) as { accessToken: string; refreshToken?: string; email: string; fullName: string; roles: string[] };

      if (!data.accessToken || !data.email) {
        this.error = 'Incomplete authentication data received.';
        return;
      }

      this.auth.storeAuth({
        accessToken: data.accessToken,
        refreshToken: data.refreshToken ?? '',
        email: data.email,
        fullName: data.fullName ?? '',
        roles: data.roles ?? ['User']
      });

      this.router.navigate(['/dashboard']);
    } catch (e) {
      this.error = 'Failed to process Google authentication response.';
    }
  }
}
