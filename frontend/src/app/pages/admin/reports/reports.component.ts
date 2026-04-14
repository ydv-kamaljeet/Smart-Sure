import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { PagedResult } from '../../../models/models';

interface Report {
  id: string;
  title: string;
  type: string;
  status: string;
  fileUrl: string;
  createdAt: string;
}

const REPORT_TYPES = [
  {
    type: 'claims',
    label: 'Claims Summary',
    description: 'All claims with status breakdown, amounts and incident dates',
    icon: 'bi-file-earmark-bar-graph',
    color: 'text-primary',
    bg: 'bg-blue-lite',
  },
  {
    type: 'policies',
    label: 'Policy Overview',
    description: 'All policies with premium, insurance type and status',
    icon: 'bi-shield-check',
    color: 'text-warning',
    bg: 'bg-orange-lite',
  },
  {
    type: 'revenue',
    label: 'Total Revenue',
    description: 'Revenue by insurance type with per-policyholder breakdown',
    icon: 'bi-cash-stack',
    color: 'text-success',
    bg: 'bg-green-lite',
  },
  {
    type: 'audit',
    label: 'Audit Log',
    description: 'All admin actions, entity changes and system events',
    icon: 'bi-journal-text',
    color: 'text-purple',
    bg: 'bg-purple-lite',
  },
];

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="mb-4">
      <h5 class="fw-bold mb-0">Reports</h5>
      <p class="text-muted small mb-0">Select a report type, then click Generate to create and download the PDF</p>
    </div>

    <!-- Step 1 — Select report type -->
    <div class="row g-3 mb-4">
      <div class="col-sm-6 col-xl-3" *ngFor="let t of reportTypes">
        <div class="stat-card h-100"
             style="cursor:pointer; transition: box-shadow .15s;"
             [class.selected-card]="selected?.type === t.type"
             (click)="selectType(t)">
          <div class="stat-icon" [ngClass]="t.bg">
            <i class="bi" [ngClass]="[t.icon, t.color]"></i>
          </div>
          <div class="flex-grow-1">
            <div class="fw-semibold small">{{ t.label }}</div>
            <div class="text-muted" style="font-size:0.75rem;">{{ t.description }}</div>
          </div>
          <div class="mt-2">
            <span *ngIf="selected?.type !== t.type"
                  class="badge bg-light text-secondary border small">Click to select</span>
            <span *ngIf="selected?.type === t.type"
                  class="badge bg-primary text-white small">
              <i class="bi bi-check2 me-1"></i>Selected
            </span>
          </div>
        </div>
      </div>
    </div>

    <!-- Step 2 — Configure & Generate -->
    <div class="table-container mb-4" *ngIf="selected">
      <div class="px-4 py-3 border-bottom">
        <h6 class="mb-0 fw-semibold">
          <i class="bi me-2" [ngClass]="selected.icon"></i>{{ selected.label }}
        </h6>
      </div>
      <div class="p-4">
        <div class="row g-3 align-items-end">
          <div class="col-md-6">
            <label class="form-label small fw-medium">Report Title <span class="text-muted">(optional)</span></label>
            <input class="form-control form-control-sm"
                   [(ngModel)]="customTitle"
                   [placeholder]="selected.label + ' — ' + today">
          </div>
          <div class="col-md-auto">
            <button class="btn btn-gradient btn-sm px-4"
                    (click)="generate()"
                    [disabled]="generating">
              <span *ngIf="generating" class="spinner-border spinner-border-sm me-2"></span>
              <i *ngIf="!generating" class="bi bi-gear me-2"></i>
              {{ generating ? 'Generating PDF...' : 'Generate Report' }}
            </button>
          </div>
          <div class="col-md-auto" *ngIf="readyBlob">
            <button class="btn btn-success btn-sm px-4" (click)="download()">
              <i class="bi bi-download me-2"></i>Download PDF
            </button>
          </div>
        </div>

        <!-- success banner -->
        <div *ngIf="readyBlob" class="alert alert-success small py-2 mt-3 mb-0 d-flex align-items-center gap-2">
          <i class="bi bi-check-circle-fill"></i>
          PDF ready — click <strong>Download PDF</strong> to save it.
        </div>

        <!-- error -->
        <div *ngIf="errorMsg" class="alert alert-danger small py-2 mt-3 mb-0">
          <i class="bi bi-exclamation-triangle me-1"></i>{{ errorMsg }}
        </div>
      </div>
    </div>

    <!-- History Table -->
    <div class="table-container">
      <div class="px-3 py-3 border-bottom d-flex align-items-center justify-content-between">
        <h6 class="mb-0 fw-semibold">Report History</h6>
        <button class="btn btn-sm btn-light" (click)="loadHistory()" title="Refresh">
          <i class="bi bi-arrow-clockwise"></i>
        </button>
      </div>
      <div *ngIf="historyLoading" class="text-center py-5">
        <div class="spinner-border text-primary"></div>
      </div>
      <div class="table-responsive" *ngIf="!historyLoading">
        <table class="table table-hover mb-0 small">
          <thead>
            <tr>
              <th class="ps-3">Title</th>
              <th>Type</th>
              <th>Status</th>
              <th>Generated</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let r of history">
              <td class="ps-3 fw-medium">{{ r.title }}</td>
              <td><span class="badge bg-light text-dark border">{{ r.type }}</span></td>
              <td>
                <span class="badge rounded-pill" [ngClass]="{
                  'bg-success text-white': r.status === 'Completed' || r.status === 'Generated',
                  'bg-danger text-white':  r.status === 'Failed',
                  'bg-warning text-dark':  r.status === 'Pending'
                }">{{ r.status }}</span>
              </td>
              <td class="text-muted">{{ r.createdAt | date:'medium' }}</td>
            </tr>
            <tr *ngIf="history.length === 0">
              <td colspan="4" class="text-center text-muted py-4">No report history yet.</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  `,
  styles: [`
    .bg-purple-lite { background: rgba(139,92,246,0.1); }
    .text-purple    { color: #7c3aed; }
    .bg-blue-lite   { background: rgba(37,99,235,0.1); }
    .bg-green-lite  { background: rgba(16,185,129,0.1); }
    .bg-orange-lite { background: rgba(245,158,11,0.1); }
    .stat-card      { display: flex; flex-direction: column; }
    .selected-card  { box-shadow: 0 0 0 2px #1a56db; }
  `]
})
export class ReportsComponent implements OnInit {
  private http = inject(HttpClient);

  reportTypes = REPORT_TYPES;
  selected: typeof REPORT_TYPES[0] | null = null;
  customTitle = '';
  today = new Date().toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

  generating = false;
  readyBlob: Blob | null = null;
  errorMsg = '';

  history: Report[] = [];
  historyLoading = true;

  ngOnInit(): void {
    this.loadHistory();
  }

  selectType(t: typeof REPORT_TYPES[0]): void {
    this.selected = t;
    this.customTitle = '';
    this.readyBlob = null;
    this.errorMsg = '';
  }

  generate(): void {
    if (!this.selected || this.generating) return;
    this.generating = true;
    this.readyBlob = null;
    this.errorMsg = '';

    const title = this.customTitle.trim() || `${this.selected.label} — ${this.today}`;

    this.http.post(
      '/api/admin/reports/generate',
      { type: this.selected.type, title },
      { responseType: 'blob' }
    ).subscribe({
      next: (blob) => {
        this.readyBlob = blob;
        this.generating = false;
        // Record in history
        this.http.post('/api/admin/reports', {
          title,
          type: this.selected!.label,
          parameters: ''
        }).subscribe({ error: () => {} });
        this.loadHistory();
      },
      error: () => {
        this.errorMsg = `Failed to generate ${this.selected!.label}. Please try again.`;
        this.generating = false;
      }
    });
  }

  download(): void {
    if (!this.readyBlob || !this.selected) return;
    const url = URL.createObjectURL(this.readyBlob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${this.selected.type}-report-${new Date().toISOString().slice(0, 10)}.pdf`;
    a.click();
    URL.revokeObjectURL(url);
  }

  loadHistory(): void {
    this.historyLoading = true;
    this.http.get<PagedResult<Report>>('/api/admin/reports').subscribe({
      next: (res) => { this.history = res.items; this.historyLoading = false; },
      error: () => { this.historyLoading = false; }
    });
  }
}
