import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { ChartData, ChartOptions, ChartType } from 'chart.js';
import { DashboardService } from '../../services/dashboard.service';
import { AuthService } from '../../services/auth.service';
import { LocaleService } from '../../services/locale.service';
import { ConvertCurrencyPipe } from '../../pipes/convert-currency.pipe';
import { DashboardData, Policy, Claim, AdminDashboardData, AuditLog } from '../../models/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, BaseChartDirective, ConvertCurrencyPipe],
  template: `
    <div *ngIf="loading" class="loading-spinner">
      <div class="spinner-border text-primary" style="width:2.5rem;height:2.5rem;"></div>
    </div>

    <div *ngIf="!loading">
      
      <!-- ADMIN VIEW -->
      <div *ngIf="isAdmin && adminData">
        <h4 class="mb-4 fw-bold">System Administration</h4>
        
        <!-- Row 1: Key metrics -->
        <div class="row g-3 mb-3">
          <div class="col-sm-4">
            <div class="stat-card">
              <div class="stat-icon bg-blue-lite"><i class="bi bi-people text-primary"></i></div>
              <div><div class="stat-label">Active Users</div><div class="stat-value">{{ adminData.activeUsers }}</div></div>
            </div>
          </div>
          <div class="col-sm-4">
            <div class="stat-card">
              <div class="stat-icon bg-green-lite"><i class="bi bi-cash-stack text-success"></i></div>
              <div><div class="stat-label">Total Revenue</div><div class="stat-value">{{ adminData.totalRevenue | convertCurrency }}</div></div>
            </div>
          </div>
          <div class="col-sm-4">
            <div class="stat-card">
              <div class="stat-icon bg-orange-lite"><i class="bi bi-hourglass-split text-warning"></i></div>
              <div><div class="stat-label">Pending Claims</div><div class="stat-value">{{ adminData.pendingClaims }}</div></div>
            </div>
          </div>
        </div>
        <!-- Row 2: Secondary metrics -->
        <div class="row g-3 mb-4">
          <div class="col-sm-4">
            <div class="stat-card">
              <div class="stat-icon" style="background:rgba(16,185,129,0.1)"><i class="bi bi-check-circle text-success"></i></div>
              <div><div class="stat-label">Approved Claims</div><div class="stat-value">{{ adminData.approvedClaims }}</div></div>
            </div>
          </div>
          <div class="col-sm-4">
            <div class="stat-card">
              <div class="stat-icon" style="background:rgba(99,102,241,0.1)"><i class="bi bi-file-earmark-diff" style="color:#6366f1"></i></div>
              <div><div class="stat-label">Total Claims</div><div class="stat-value">{{ adminData.totalClaims }}</div></div>
            </div>
          </div>
          <div class="col-sm-4">
            <div class="stat-card">
              <div class="stat-icon" style="background:rgba(139,92,246,0.1)"><i class="bi bi-shield-check" style="color:#7c3aed"></i></div>
              <div><div class="stat-label">Total Policies</div><div class="stat-value">{{ adminData.totalPolicies }}</div></div>
            </div>
          </div>
        </div>

        <div class="row g-4">
           <div class="col-xl-8">
              <div class="table-container">
                <div class="px-3 py-3 border-bottom"><h6 class="mb-0 fw-semibold">Recent Audit Logs</h6></div>
                <div class="table-responsive">
                  <table class="table table-hover mb-0 small">
                    <thead><tr><th>Timestamp</th><th>User</th><th>Action</th><th>Entity</th><th>Details</th></tr></thead>
                    <tbody>
                      <tr *ngFor="let log of auditLogs">
                        <td class="text-muted">{{ log.timestamp | date:'short':locale.timezone:locale.locale }}</td>
                        <td>{{ log.userName || 'System' }}</td>
                        <td><span class="badge bg-light text-dark border">{{ log.action }}</span></td>
                        <td>{{ log.entityName }}</td>
                        <td class="text-truncate" style="max-width:200px;">{{ log.details }}</td>
                      </tr>
                      <tr *ngIf="auditLogs.length === 0">
                        <td colspan="5" class="text-center text-muted py-4">No audit logs yet.</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
           </div>
           <div class="col-xl-4">
              <div class="table-container p-3">
                <h6 class="fw-semibold mb-3">Quick Actions</h6>
                <div class="d-grid gap-2">
                  <a routerLink="/admin/claims" class="btn btn-outline-warning btn-sm text-start">
                    <i class="bi bi-hourglass-split me-2"></i>Review Pending Claims
                    <span class="badge bg-warning text-dark ms-1">{{ adminData.pendingClaims }}</span>
                  </a>
                  <a routerLink="/admin/users" class="btn btn-outline-primary btn-sm text-start">
                    <i class="bi bi-people me-2"></i>Manage Users
                  </a>
                  <a routerLink="/admin/policies" class="btn btn-outline-secondary btn-sm text-start">
                    <i class="bi bi-file-earmark-text me-2"></i>View All Policies
                  </a>
                  <a routerLink="/admin/insurance" class="btn btn-outline-success btn-sm text-start">
                    <i class="bi bi-shield-plus me-2"></i>Insurance Catalog
                  </a>
                </div>
              </div>
           </div>
        </div>
      </div>

      <!-- CUSTOMER VIEW -->
      <div *ngIf="!isAdmin && data">
        <div class="row g-3 mb-4">
          <div class="col-6 col-xl-3">
            <div class="stat-card">
              <div class="stat-icon" style="background:#eff6ff;">
                <i class="bi bi-shield-check" style="color:#1a56db;font-size:1.3rem;"></i>
              </div>
              <div>
                <div class="stat-label">Active Policies</div>
                <div class="stat-value">{{ data.activePolicies }}</div>
              </div>
            </div>
          </div>
          <div class="col-6 col-xl-3">
            <div class="stat-card">
              <div class="stat-icon" style="background:#f0fdf4;">
                <i class="bi bi-graph-up-arrow" style="color:#0d9488;font-size:1.3rem;"></i>
              </div>
              <div>
                <div class="stat-label">Total Premium</div>
                <div class="stat-value">{{ data.totalPremium | convertCurrency:'1.2-2' }}</div>
              </div>
            </div>
          </div>
          <div class="col-6 col-xl-3">
            <div class="stat-card">
              <div class="stat-icon" style="background:#fffbeb;">
                <i class="bi bi-exclamation-circle" style="color:#d97706;font-size:1.3rem;"></i>
              </div>
              <div>
                <div class="stat-label">Pending Claims</div>
                <div class="stat-value">{{ data.pendingClaims }}</div>
              </div>
            </div>
          </div>
          <div class="col-6 col-xl-3">
            <div class="stat-card">
              <div class="stat-icon" style="background:#f5f3ff;">
                <i class="bi bi-file-earmark-text" style="color:#7c3aed;font-size:1.3rem;"></i>
              </div>
              <div>
                <div class="stat-label">Total Claims</div>
                <div class="stat-value">{{ data.totalClaims }}</div>
              </div>
            </div>
          </div>
        </div>

        <!-- Charts and tables stay here as in existing UI -->
        <div class="row g-3 mb-4">
          <div class="col-md-6">
            <div class="chart-card">
              <h6><i class="bi bi-pie-chart me-2 text-primary"></i>Policies by Status</h6>
              <div style="max-height:220px;position:relative;">
                <canvas baseChart [data]="pieChartData" [options]="pieChartOptions" type="pie"></canvas>
              </div>
            </div>
          </div>
          <div class="col-md-6">
            <div class="chart-card">
              <h6><i class="bi bi-bar-chart me-2 text-primary"></i>Claims by Status</h6>
              <div style="max-height:220px;position:relative;">
                <canvas baseChart [data]="barChartData" [options]="barChartOptions" type="bar"></canvas>
              </div>
            </div>
          </div>
        </div>

        <div class="table-container mb-4">
          <div class="d-flex align-items-center justify-content-between px-3 py-3 border-bottom">
            <h6 class="mb-0 fw-semibold">Recent Claims</h6>
            <a routerLink="/claims" class="small text-primary fw-medium text-decoration-none">View all</a>
          </div>
          <div class="table-responsive">
            <table class="table table-hover mb-0">
              <thead><tr><th>Claim #</th><th>Description</th><th>Status</th><th class="text-end">Amount</th></tr></thead>
              <tbody>
                <tr *ngFor="let c of data.recentClaims">
                  <td><code class="small">{{ c.claimNumber || 'CLM-' + c.id }}</code></td>
                  <td>{{ c.description }}</td>
                  <td><span [class]="'badge rounded-pill ' + claimBadgeClass(c.status)">{{ c.status }}</span></td>
                  <td class="text-end fw-medium">{{ c.claimAmount | convertCurrency }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .bg-blue-lite { background: rgba(37, 99, 235, 0.1); }
    .bg-green-lite { background: rgba(16, 185, 129, 0.1); }
    .bg-orange-lite { background: rgba(245, 158, 11, 0.1); }
  `]
})
export class DashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private authService = inject(AuthService);
  locale = inject(LocaleService);

  data: DashboardData | null = null;
  adminData: AdminDashboardData | null = null;
  auditLogs: AuditLog[] = [];
  loading = true;
  isAdmin = false;

  pieChartData: ChartData<'pie'> = { labels: [], datasets: [] };
  pieChartOptions: ChartOptions<'pie'> = {
    responsive: true, maintainAspectRatio: false,
    plugins: { legend: { position: 'bottom' } }
  };

  barChartData: ChartData<'bar'> = { labels: [], datasets: [] };
  barChartOptions: ChartOptions<'bar'> = {
    responsive: true, maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: { y: { beginAtZero: true, ticks: { stepSize: 1 } } }
  };

  ngOnInit(): void {
    const user = this.authService.getUser();
    this.isAdmin = user?.roles?.includes('Admin') ?? false;

    if (this.isAdmin) {
      this.loadAdminDashboard();
    } else {
      this.loadCustomerDashboard();
    }
  }

  loadAdminDashboard(): void {
    this.dashboardService.getAdminDashboard().subscribe({
      next: (d) => {
        this.adminData = d;
        this.dashboardService.getAuditLogs(1, 10).subscribe(logs => {
          this.auditLogs = logs.items;
          this.loading = false;
        });
      },
      error: () => { this.loading = false; }
    });
  }

  loadCustomerDashboard(): void {
    this.dashboardService.getDashboard().subscribe({
      next: (data) => {
        this.data = data;
        this.buildCharts(data);
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }

  buildCharts(d: DashboardData): void {
    const pieColors = ['#1a56db', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6'];
    this.pieChartData = {
      labels: d.policyByStatus.map(x => x.status),
      datasets: [{ data: d.policyByStatus.map(x => x.count), backgroundColor: pieColors }]
    };

    const statusColors: Record<string, string> = {
      'Pending': '#f59e0b', 'In Review': '#3b82f6',
      'Approved': '#10b981', 'Rejected': '#ef4444'
    };
    this.barChartData = {
      labels: d.claimByStatus.map(x => x.status),
      datasets: [{
        data: d.claimByStatus.map(x => x.count),
        backgroundColor: d.claimByStatus.map(x => statusColors[x.status] ?? '#6366f1'),
        borderRadius: 6
      }]
    };
  }

  claimBadgeClass(status: string): string {
    const map: Record<string, string> = {
      'Pending': 'badge-pending', 'In Review': 'badge-in-review',
      'Approved': 'badge-approved', 'Rejected': 'badge-rejected',
      'Submitted': 'badge-pending'
    };
    return map[status] ?? 'bg-secondary';
  }
}
