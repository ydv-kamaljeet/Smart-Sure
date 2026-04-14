import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PolicyService } from '../../services/policy.service';
import { LocaleService } from '../../services/locale.service';
import { ConvertCurrencyPipe } from '../../pipes/convert-currency.pipe';
import { Policy } from '../../models/models';

@Component({
  selector: 'app-policies',
  standalone: true,
  imports: [CommonModule, FormsModule, ConvertCurrencyPipe],
  template: `
    <!-- Filters -->
    <div class="d-flex flex-wrap gap-2 mb-4">
      <div class="input-group" style="max-width:320px;">
        <span class="input-group-text bg-white"><i class="bi bi-search text-muted"></i></span>
        <input type="text" class="form-control border-start-0" placeholder="Search by policy # or vehicle..."
               [(ngModel)]="searchTerm" (ngModelChange)="applyFilters()">
      </div>
      <select class="form-select" style="max-width:160px;" [(ngModel)]="statusFilter" (ngModelChange)="applyFilters()">
        <option value="All">All Status</option>
        <option value="Active">Active</option>
        <option value="Cancelled">Cancelled</option>
      </select>
    </div>

    <div *ngIf="loading" class="loading-spinner">
      <div class="spinner-border text-primary"></div>
    </div>

    <div *ngIf="error" class="alert alert-warning small">
      <i class="bi bi-info-circle me-2"></i>{{ error }}
    </div>

    <div *ngIf="!loading && filtered.length === 0" class="empty-state">
      <i class="bi bi-file-earmark-x text-muted"></i>
      <p class="mt-2 fw-medium text-muted">No policies found</p>
      <p class="small text-muted">Try adjusting your filters or purchase a new policy.</p>
    </div>

    <div class="row g-3">
      <div class="col-sm-6 col-xl-4" *ngFor="let p of filtered">
        <div class="policy-card h-100" style="cursor:pointer;" (click)="viewDetail(p.id)">
          <div class="d-flex align-items-start justify-content-between mb-3">
            <div class="d-flex align-items-center gap-2">
              <div class="d-flex align-items-center justify-content-center rounded-3"
                   style="width:40px;height:40px;background:#eff6ff;">
                <i [class]="'bi fs-5 ' + typeIcon(p.subType?.insuranceTypeId)" style="color:#1a56db;"></i>
              </div>
              <div>
                <div class="fw-semibold" style="font-size:0.875rem;">{{ p.subType?.name || 'Policy' }}</div>
                <code class="text-muted" style="font-size:0.75rem;">{{ p.policyNumber || 'POL-' + p.id.slice(0,8) }}</code>
              </div>
            </div>
            <span [class]="'badge rounded-pill ' + badgeClass(p.status)">{{ p.status }}</span>
          </div>

          <div *ngIf="p.vehicleDetails" class="mb-2">
            <small class="text-muted d-block">Vehicle</small>
            <span class="fw-medium small">{{ p.vehicleDetails.year }} {{ p.vehicleDetails.make }} {{ p.vehicleDetails.model }}</span>
            <span class="text-muted small ms-2">({{ p.vehicleDetails.licensePlate }})</span>
          </div>

          <div *ngIf="p.homeDetails" class="mb-2">
            <small class="text-muted d-block">Property Address</small>
            <span class="fw-medium small">{{ p.homeDetails.propertyAddress }}</span>
          </div>

          <div class="row g-2 mt-1">
            <div class="col-12">
              <div class="p-2 rounded-2" style="background:#f8fafc;">
                <small class="text-muted d-block">Annual Premium</small>
                <span class="fw-bold text-primary">{{ p.premiumAmount | convertCurrency:'1.2-2' }}</span>
              </div>
            </div>
          </div>

          <div class="d-flex justify-content-between mt-3 pt-2" style="border-top:1px solid #f1f5f9;">
            <div>
              <small class="text-muted d-block">Start</small>
              <small class="fw-medium">{{ p.startDate | date:'MMM d, y':locale.timezone:locale.locale }}</small>
            </div>
            <div class="text-end">
              <small class="text-muted d-block">End</small>
              <small class="fw-medium">{{ p.endDate | date:'MMM d, y':locale.timezone:locale.locale }}</small>
            </div>
          </div>

          <div class="mt-3 text-center">
            <span class="text-primary small"><i class="bi bi-eye me-1"></i>View Details</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Policy Detail Modal -->
    <div *ngIf="selectedPolicy" class="modal d-block" tabindex="-1" style="background:rgba(0,0,0,0.45);">
      <div class="modal-dialog modal-dialog-centered modal-lg">
        <div class="modal-content rounded-4 border-0">
          <div class="modal-header border-0 pb-0">
            <div>
              <h5 class="modal-title fw-bold mb-0">Policy Details</h5>
              <code class="text-muted small">{{ selectedPolicy.policyNumber }}</code>
            </div>
            <button class="btn-close" (click)="selectedPolicy = null"></button>
          </div>
          <div class="modal-body pt-2">
            <div *ngIf="detailLoading" class="text-center py-4">
              <div class="spinner-border text-primary"></div>
            </div>
            <div *ngIf="!detailLoading">
              <div class="d-flex gap-2 mb-4">
                <span [class]="'badge rounded-pill fs-6 ' + badgeClass(selectedPolicy.status)">{{ selectedPolicy.status }}</span>
                <span class="badge bg-light text-dark fs-6">{{ selectedPolicy.subType?.name }}</span>
              </div>
              <div class="row g-3 mb-4">
                <div class="col-6 col-md-3">
                  <div class="p-3 bg-light rounded-3 text-center">
                    <small class="text-muted d-block">Annual Premium</small>
                    <span class="fw-bold text-primary">{{ selectedPolicy.premiumAmount | convertCurrency }}</span>
                  </div>
                </div>
                <div class="col-6 col-md-3">
                  <div class="p-3 bg-light rounded-3 text-center">
                    <small class="text-muted d-block">{{ selectedPolicy.subType?.insuranceTypeId === 1 ? 'IDV' : 'Sum Insured' }}</small>
                    <span class="fw-bold text-success">{{ selectedPolicy.insuredDeclaredValue | convertCurrency }}</span>
                  </div>
                </div>
                <div class="col-6 col-md-3">
                  <div class="p-3 bg-light rounded-3 text-center">
                    <small class="text-muted d-block">Start Date</small>
                    <span class="fw-semibold small">{{ selectedPolicy.startDate | date:'MMM d, y':locale.timezone:locale.locale }}</span>
                  </div>
                </div>
                <div class="col-6 col-md-3">
                  <div class="p-3 bg-light rounded-3 text-center">
                    <small class="text-muted d-block">End Date</small>
                    <span class="fw-semibold small">{{ selectedPolicy.endDate | date:'MMM d, y':locale.timezone:locale.locale }}</span>
                  </div>
                </div>
              </div>

              <div *ngIf="selectedPolicy.vehicleDetails" class="mb-3">
                <h6 class="fw-semibold mb-2"><i class="bi bi-car-front me-2 text-primary"></i>Vehicle Details</h6>
                <div class="row g-2">
                  <div class="col-6"><small class="text-muted d-block">Make</small><span class="fw-medium">{{ selectedPolicy.vehicleDetails.make }}</span></div>
                  <div class="col-6"><small class="text-muted d-block">Model</small><span class="fw-medium">{{ selectedPolicy.vehicleDetails.model }}</span></div>
                  <div class="col-6"><small class="text-muted d-block">Year</small><span class="fw-medium">{{ selectedPolicy.vehicleDetails.year }}</span></div>
                  <div class="col-6"><small class="text-muted d-block">License Plate</small><span class="fw-medium">{{ selectedPolicy.vehicleDetails.licensePlate }}</span></div>
                  <div class="col-6"><small class="text-muted d-block">VIN</small><span class="fw-medium">{{ selectedPolicy.vehicleDetails.vin || '&#8212;' }}</span></div>
                  <div class="col-6"><small class="text-muted d-block">Annual Mileage</small><span class="fw-medium">{{ selectedPolicy.vehicleDetails.annualMileage | number }} km</span></div>
                </div>
              </div>

              <div *ngIf="selectedPolicy.homeDetails" class="mb-3">
                <h6 class="fw-semibold mb-2"><i class="bi bi-house me-2 text-primary"></i>Property Details</h6>
                <div class="row g-2">
                  <div class="col-12"><small class="text-muted d-block">Address</small><span class="fw-medium">{{ selectedPolicy.homeDetails.propertyAddress }}</span></div>
                  <div class="col-6"><small class="text-muted d-block">Property Value</small><span class="fw-medium">{{ selectedPolicy.homeDetails.propertyValue | convertCurrency }}</span></div>
                  <div class="col-6"><small class="text-muted d-block">Year Built</small><span class="fw-medium">{{ selectedPolicy.homeDetails.yearBuilt }}</span></div>
                  <div class="col-6"><small class="text-muted d-block">Construction Type</small><span class="fw-medium">{{ selectedPolicy.homeDetails.constructionType }}</span></div>
                  <div class="col-6"><small class="text-muted d-block">Security System</small><span class="fw-medium">{{ selectedPolicy.homeDetails.hasSecuritySystem ? 'Yes' : 'No' }}</span></div>
                  <div class="col-6"><small class="text-muted d-block">Fire Alarm</small><span class="fw-medium">{{ selectedPolicy.homeDetails.hasFireAlarm ? 'Yes' : 'No' }}</span></div>
                </div>
              </div>
            </div>
          </div>
          <div class="modal-footer border-0">
            <button class="btn btn-outline-secondary rounded-pill" (click)="selectedPolicy = null">Close</button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class PoliciesComponent implements OnInit {
  policies: Policy[] = [];
  filtered: Policy[] = [];
  loading = true;
  error = '';
  searchTerm = '';
  statusFilter = 'All';
  selectedPolicy: any = null;
  detailLoading = false;

  constructor(private policyService: PolicyService, public locale: LocaleService) {}

  ngOnInit(): void {
    this.policyService.getPolicies().subscribe({
      next: (res) => { this.policies = res.items; this.applyFilters(); this.loading = false; },
      error: () => {
        this.error = 'Could not load policies — backend not connected. Showing sample data.';
        this.policies = [
          { id: '1', policyNumber: 'POL-082411', status: 'Active', premiumAmount: 129.99, insuredDeclaredValue: 6499, startDate: '2024-01-01', endDate: '2025-01-01', subType: { id: 1, name: 'Premium Auto', basePremium: 100, insuranceTypeId: 1 }, vehicleDetails: { make: 'Toyota', model: 'Camry', year: 2020, listedPrice: 800000, licensePlate: 'ABC-1234' } },
          { id: '2', policyNumber: 'POL-082412', status: 'Active', premiumAmount: 89.99, insuredDeclaredValue: 89990, startDate: '2024-03-01', endDate: '2025-03-01', subType: { id: 2, name: 'Standard Home', basePremium: 80, insuranceTypeId: 2 }, homeDetails: { propertyAddress: '123 Main St', propertyValue: 250000 } },
        ];
        this.applyFilters();
        this.loading = false;
      }
    });
  }

  viewDetail(id: string): void {
    this.detailLoading = true;
    this.selectedPolicy = { policyNumber: '...' };
    this.policyService.getPolicyById(id).subscribe({
      next: (p) => { this.selectedPolicy = p; this.detailLoading = false; },
      error: () => { this.detailLoading = false; }
    });
  }

  applyFilters(): void {
    let list = this.policies;
    if (this.searchTerm) {
      const q = this.searchTerm.toLowerCase();
      list = list.filter(p =>
        (p.policyNumber?.toLowerCase().includes(q) ?? false) ||
        (p.subType?.name.toLowerCase().includes(q) ?? false) ||
        (p.vehicleDetails?.make.toLowerCase().includes(q) ?? false) ||
        (p.vehicleDetails?.model.toLowerCase().includes(q) ?? false)
      );
    }
    if (this.statusFilter !== 'All') list = list.filter(p => p.status === this.statusFilter);
    this.filtered = list;
  }

  badgeClass(status: string): string {
    const map: Record<string, string> = {
      'Active': 'badge-active',
      'Pending': 'badge-pending',
      'Expired': 'badge-expired',
      'Cancelled': 'badge-rejected'
    };
    return map[status] ?? 'bg-secondary';
  }

  typeIcon(typeId: number | undefined): string {
    const map: Record<number, string> = { 1: 'bi-car-front', 2: 'bi-house', 3: 'bi-heart-pulse', 4: 'bi-activity' };
    return map[typeId ?? 0] ?? 'bi-shield';
  }
}
