import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ClaimService } from '../../services/claim.service';
import { PolicyService } from '../../services/policy.service';
import { LocaleService } from '../../services/locale.service';
import { ConvertCurrencyPipe } from '../../pipes/convert-currency.pipe';
import { Claim, Policy } from '../../models/models';

@Component({
  selector: 'app-claims',
  standalone: true,
  imports: [CommonModule, FormsModule, ConvertCurrencyPipe],
  template: `
    <!-- Header -->
    <div class="d-flex align-items-center justify-content-between mb-4">
      <div class="d-flex gap-2 flex-wrap">
        <button *ngFor="let tab of tabs"
                [class]="'btn btn-sm ' + (activeTab === tab ? 'btn-primary' : 'btn-outline-secondary')"
                (click)="activeTab = tab; applyFilter()">
          {{ tab }}
          <span *ngIf="countByStatus(tab) > 0" class="badge bg-white text-primary ms-1">{{ countByStatus(tab) }}</span>
        </button>
      </div>
    </div>

    <!-- Loading -->
    <div *ngIf="loading" class="loading-spinner">
      <div class="spinner-border text-primary"></div>
    </div>

    <!-- Error -->
    <div *ngIf="error" class="alert alert-warning small mb-3">
      <i class="bi bi-info-circle me-2"></i>{{ error }}
    </div>

    <!-- Table -->
    <div *ngIf="!loading" class="table-container">
      <div class="table-responsive">
        <table class="table table-hover mb-0">
          <thead>
            <tr>
              <th>Claim #</th>
              <th>Policy #</th>
              <th>Type</th>
              <th>Incident Date</th>
              <th class="text-end">Amount</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let c of filtered" style="cursor:pointer;" (click)="openDetail(c)">
              <td><code class="small">{{ c.claimNumber || 'CLM-' + c.id }}</code></td>
              <td><code class="small">{{ getPolicyNumber(c.policyId) }}</code></td>
              <td>Insurance</td>
              <td>{{ c.incidentDate | date:'MMM d, y':locale.timezone:locale.locale }}</td>
              <td class="text-end fw-medium">{{ c.claimAmount | convertCurrency }}</td>
              <td><span [class]="'badge rounded-pill ' + badgeClass(c.status)">{{ c.status }}</span></td>
              <td>
                <button class="btn btn-sm btn-light" (click)="$event.stopPropagation(); openDetail(c)">
                  <i class="bi bi-eye"></i>
                </button>
              </td>
            </tr>
            <tr *ngIf="filtered.length === 0">
              <td colspan="7" class="text-center py-5">
                <div class="empty-state">
                  <i class="bi bi-file-earmark-x text-muted" style="font-size:2rem;"></i>
                  <p class="mt-2 mb-0 text-muted">No claims found</p>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Detail Modal -->
    <div *ngIf="selectedClaim" class="modal d-block" tabindex="-1" style="background:rgba(0,0,0,0.4);">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content rounded-4">
          <div class="modal-header border-bottom-0">
            <h5 class="modal-title fw-bold">Claim Details</h5>
            <button type="button" class="btn-close" (click)="selectedClaim = null"></button>
          </div>
          <div class="modal-body">
            <div class="mb-3 d-flex align-items-center gap-3">
              <span [class]="'badge rounded-pill fs-6 ' + badgeClass(selectedClaim.status)">
                {{ selectedClaim.status }}
              </span>
              <code class="text-muted">{{ selectedClaim.claimNumber }}</code>
            </div>
            <div class="row g-3 mb-3">
              <div class="col-6">
                <small class="text-muted d-block">Policy #</small>
                <span class="fw-medium">{{ getPolicyNumber(selectedClaim.policyId) }}</span>
              </div>
              <div class="col-6">
                <small class="text-muted d-block">Status</small>
                <span class="fw-medium">{{ selectedClaim.status }}</span>
              </div>
              <div class="col-6">
                <small class="text-muted d-block">Incident Date</small>
                <span class="fw-medium">{{ selectedClaim.incidentDate | date:'MMM d, y':locale.timezone:locale.locale }}</span>
              </div>
              <div class="col-6">
                <small class="text-muted d-block">Filed On</small>
                <span class="fw-medium">{{ selectedClaim.createdAt | date:'MMM d, y':locale.timezone:locale.locale }}</span>
              </div>
              <div class="col-12">
                <small class="text-muted d-block">Claim Amount</small>
                <span class="fw-bold text-primary fs-5">{{ selectedClaim.claimAmount | convertCurrency }}</span>
              </div>
              <div class="col-12">
                <small class="text-muted d-block mb-1">Description</small>
                <p class="mb-0 small">{{ selectedClaim.description }}</p>
              </div>
            </div>
          </div>
          <div class="modal-footer border-top-0">
            <button class="btn btn-outline-secondary" (click)="selectedClaim = null">Close</button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class ClaimsComponent implements OnInit {
  claims: Claim[] = [];
  filtered: Claim[] = [];
  policies: Policy[] = [];
  loading = true;
  error = '';
  activeTab = 'All';
  tabs = ['All', 'Pending', 'In Review', 'Approved', 'Rejected'];
  selectedClaim: Claim | null = null;

  getPolicyNumber(policyId: string): string {
    return this.policies.find(p => p.id === policyId)?.policyNumber ?? policyId.slice(0, 8).toUpperCase();
  }

  constructor(private claimService: ClaimService, private policyService: PolicyService, public locale: LocaleService) {}

  ngOnInit(): void {
    this.loadData();
    this.policyService.getPolicies().subscribe({
      next: (res) => this.policies = res.items,
      error: () => this.policies = []
    });
  }

  loadData(): void {
    this.claimService.getClaims().subscribe({
      next: (res) => { this.claims = res.items; this.applyFilter(); this.loading = false; },
      error: () => {
        this.error = 'Could not load claims — showing sample data.';
        this.claims = [
          { id: 1, claimNumber: 'CLM-005678', policyId: '1', description: 'Rear-end collision on highway', status: 'In Review', claimAmount: 3500, incidentDate: '2024-02-15', createdAt: '2024-02-16' },
          { id: 2, claimNumber: 'CLM-005679', policyId: '2', description: 'Burst pipe in kitchen', status: 'Approved', claimAmount: 1200, incidentDate: '2024-01-10', createdAt: '2024-01-11' },
          { id: 3, claimNumber: 'CLM-005680', policyId: '1', description: 'Vehicle stolen from parking lot', status: 'Pending', claimAmount: 25000, incidentDate: '2024-03-01', createdAt: '2024-03-02' },
          { id: 4, claimNumber: 'CLM-005681', policyId: '2', description: 'Kitchen fire damage', status: 'Rejected', claimAmount: 8000, incidentDate: '2023-11-20', createdAt: '2023-11-21' },
        ];
        this.applyFilter();
        this.loading = false;
      }
    });
  }

  applyFilter(): void {
    const tabMap: Record<string, string[]> = {
      'All': [],
      'Pending': ['Pending', 'Submitted'],
      'In Review': ['In Review', 'UnderReview', 'Under Review'],
      'Approved': ['Approved'],
      'Rejected': ['Rejected']
    };
    if (this.activeTab === 'All') {
      this.filtered = this.claims;
    } else {
      const allowed = tabMap[this.activeTab] || [this.activeTab];
      this.filtered = this.claims.filter(c => allowed.includes(c.status));
    }
  }

  countByStatus(tab: string): number {
    if (tab === 'All') return 0;
    const tabMap: Record<string, string[]> = {
      'Pending': ['Pending', 'Submitted'],
      'In Review': ['In Review', 'UnderReview', 'Under Review'],
      'Approved': ['Approved'],
      'Rejected': ['Rejected']
    };
    const allowed = tabMap[tab] || [tab];
    return this.claims.filter(c => allowed.includes(c.status)).length;
  }

  openDetail(claim: Claim): void {
    this.selectedClaim = claim;
  }

  badgeClass(status: string): string {
    const map: Record<string, string> = {
      'Pending': 'badge-pending',
      'Submitted': 'badge-pending',
      'In Review': 'badge-in-review',
      'UnderReview': 'badge-in-review',
      'Under Review': 'badge-in-review',
      'Approved': 'badge-approved',
      'Rejected': 'badge-rejected',
      'Draft': 'bg-secondary'
    };
    return map[status] ?? 'bg-secondary';
  }
}
