import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PolicyService } from '../../../services/policy.service';
import { InsuranceType, InsuranceSubType } from '../../../models/models';

@Component({
  selector: 'app-manage-insurance',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h5 class="fw-bold mb-0">Manage Insurance Catalog</h5>
      <button class="btn btn-primary btn-sm" (click)="openTypeModal()">
        <i class="bi bi-plus-lg me-1"></i> Add Insurance Type
      </button>
    </div>

    <div *ngIf="loading" class="text-center py-5">
      <div class="spinner-border text-primary"></div>
    </div>

    <div *ngIf="!loading" class="row g-3">
      <div class="col-12" *ngFor="let type of insuranceTypes">
        <div class="table-container">
          <!-- Type Header -->
          <div class="px-3 py-3 border-bottom d-flex justify-content-between align-items-center">
            <div>
              <span class="fw-semibold">{{ type.name }}</span>
              <span class="text-muted small ms-2">{{ type.description }}</span>
              <span class="badge ms-2" [class]="type.isActive ? 'bg-success' : 'bg-secondary'">
                {{ type.isActive ? 'Active' : 'Inactive' }}
              </span>
            </div>
            <div class="d-flex gap-2">
              <button class="btn btn-sm btn-outline-secondary" (click)="openSubTypeModal(type)">
                <i class="bi bi-plus me-1"></i>Add SubType
              </button>
              <button class="btn btn-sm btn-outline-primary" (click)="editType(type)">
                <i class="bi bi-pencil"></i>
              </button>
              <button class="btn btn-sm btn-outline-danger" (click)="deleteType(type.id)">
                <i class="bi bi-trash"></i>
              </button>
            </div>
          </div>

          <!-- SubTypes Table -->
          <div class="table-responsive">
            <table class="table table-hover mb-0 small">
              <thead>
                <tr><th>SubType Name</th><th>Description</th><th>Base Premium</th><th>Status</th><th>Actions</th></tr>
              </thead>
              <tbody>
                <tr *ngFor="let sub of type.subTypes">
                  <td class="fw-medium">{{ sub.name }}</td>
                  <td class="text-muted">{{ sub.description }}</td>
                  <td>₹{{ sub.basePremium | number:'1.2-2' }}</td>
                  <td>
                    <span class="badge rounded-pill" [class]="sub.isActive ? 'bg-success' : 'bg-secondary'">
                      {{ sub.isActive ? 'Active' : 'Inactive' }}
                    </span>
                  </td>
                  <td>
                    <div class="d-flex gap-1">
                      <button class="btn btn-sm btn-outline-primary" (click)="editSubType(sub)">
                        <i class="bi bi-pencil"></i>
                      </button>
                      <button class="btn btn-sm btn-outline-danger" (click)="deleteSubType(sub.id)">
                        <i class="bi bi-trash"></i>
                      </button>
                    </div>
                  </td>
                </tr>
                <tr *ngIf="!type.subTypes || type.subTypes.length === 0">
                  <td colspan="5" class="text-center text-muted py-3">No subtypes yet.</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>

    <!-- Insurance Type Modal -->
    <div class="modal fade show d-block" style="background:rgba(0,0,0,0.5)" *ngIf="showTypeModal">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h6 class="modal-title fw-semibold">{{ editingType ? 'Edit' : 'Add' }} Insurance Type</h6>
            <button class="btn-close" (click)="closeModals()"></button>
          </div>
          <div class="modal-body">
            <div class="mb-3">
              <label class="form-label small fw-medium">Name</label>
              <input class="form-control" [(ngModel)]="typeForm.name" placeholder="e.g. Vehicle Insurance">
            </div>
            <div class="mb-3">
              <label class="form-label small fw-medium">Description</label>
              <textarea class="form-control" [(ngModel)]="typeForm.description" rows="2"></textarea>
            </div>
            <div class="form-check" *ngIf="editingType">
              <input class="form-check-input" type="checkbox" [(ngModel)]="typeForm.isActive" id="typeActive">
              <label class="form-check-label small" for="typeActive">Active</label>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-secondary btn-sm" (click)="closeModals()">Cancel</button>
            <button class="btn btn-primary btn-sm" (click)="saveType()" [disabled]="saving">
              <span *ngIf="saving" class="spinner-border spinner-border-sm me-1"></span>
              {{ editingType ? 'Update' : 'Create' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- SubType Modal -->
    <div class="modal fade show d-block" style="background:rgba(0,0,0,0.5)" *ngIf="showSubTypeModal">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h6 class="modal-title fw-semibold">{{ editingSubType ? 'Edit' : 'Add' }} SubType</h6>
            <button class="btn-close" (click)="closeModals()"></button>
          </div>
          <div class="modal-body">
            <div class="mb-3">
              <label class="form-label small fw-medium">Name</label>
              <input class="form-control" [(ngModel)]="subTypeForm.name" placeholder="e.g. Comprehensive">
            </div>
            <div class="mb-3">
              <label class="form-label small fw-medium">Description</label>
              <textarea class="form-control" [(ngModel)]="subTypeForm.description" rows="2"></textarea>
            </div>
            <div class="mb-3">
              <label class="form-label small fw-medium">Base Premium ($)</label>
              <input class="form-control" type="number" [(ngModel)]="subTypeForm.basePremium" min="0">
            </div>
            <div class="form-check" *ngIf="editingSubType">
              <input class="form-check-input" type="checkbox" [(ngModel)]="subTypeForm.isActive" id="subActive">
              <label class="form-check-label small" for="subActive">Active</label>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-secondary btn-sm" (click)="closeModals()">Cancel</button>
            <button class="btn btn-primary btn-sm" (click)="saveSubType()" [disabled]="saving">
              <span *ngIf="saving" class="spinner-border spinner-border-sm me-1"></span>
              {{ editingSubType ? 'Update' : 'Create' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class ManageInsuranceComponent implements OnInit {
  private policyService = inject(PolicyService);

  insuranceTypes: (InsuranceType & { subTypes: InsuranceSubType[] })[] = [];
  loading = true;
  saving = false;

  showTypeModal = false;
  showSubTypeModal = false;
  editingType: InsuranceType | null = null;
  editingSubType: InsuranceSubType | null = null;
  selectedTypeId: number | null = null;

  typeForm = { name: '', description: '', isActive: true };
  subTypeForm = { name: '', description: '', basePremium: 0, isActive: true };

  ngOnInit(): void { this.loadTypes(); }

  loadTypes(): void {
    this.loading = true;
    this.policyService.getInsuranceTypesAdmin().subscribe({
      next: async (types) => {
        this.insuranceTypes = [];
        for (const t of types) {
          const subs = await this.policyService.getInsuranceSubTypesAdmin(t.id).toPromise() ?? [];
          this.insuranceTypes.push({ ...t, subTypes: subs });
        }
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }

  openTypeModal(): void {
    this.editingType = null;
    this.typeForm = { name: '', description: '', isActive: true };
    this.showTypeModal = true;
  }

  editType(type: InsuranceType & { subTypes: InsuranceSubType[] }): void {
    this.editingType = type;
    this.typeForm = { name: type.name, description: type.description ?? '', isActive: type.isActive };
    this.showTypeModal = true;
  }

  saveType(): void {
    this.saving = true;
    const obs = this.editingType
      ? this.policyService.updateInsuranceType(this.editingType.id, this.typeForm)
      : this.policyService.createInsuranceType({ name: this.typeForm.name, description: this.typeForm.description });

    obs.subscribe({
      next: () => { this.closeModals(); this.loadTypes(); },
      error: (err) => { alert(err.error?.errorMessage ?? 'Failed'); this.saving = false; }
    });
  }

  deleteType(id: number): void {
    if (!confirm('Deactivate this insurance type?')) return;
    this.policyService.deleteInsuranceType(id).subscribe({
      next: () => this.loadTypes(),
      error: (err) => alert(err.error?.errorMessage ?? 'Failed')
    });
  }

  openSubTypeModal(type: InsuranceType): void {
    this.editingSubType = null;
    this.selectedTypeId = type.id;
    this.subTypeForm = { name: '', description: '', basePremium: 0, isActive: true };
    this.showSubTypeModal = true;
  }

  editSubType(sub: InsuranceSubType): void {
    this.editingSubType = sub;
    this.selectedTypeId = sub.insuranceTypeId;
    this.subTypeForm = { name: sub.name, description: sub.description ?? '', basePremium: sub.basePremium, isActive: sub.isActive };
    this.showSubTypeModal = true;
  }

  saveSubType(): void {
    this.saving = true;
    const obs = this.editingSubType
      ? this.policyService.updateInsuranceSubType(this.editingSubType.id, this.subTypeForm)
      : this.policyService.createInsuranceSubType({ insuranceTypeId: this.selectedTypeId!, ...this.subTypeForm });

    obs.subscribe({
      next: () => { this.closeModals(); this.loadTypes(); },
      error: (err) => { alert(err.error?.errorMessage ?? 'Failed'); this.saving = false; }
    });
  }

  deleteSubType(id: number): void {
    if (!confirm('Deactivate this subtype?')) return;
    this.policyService.deleteInsuranceSubType(id).subscribe({
      next: () => this.loadTypes(),
      error: (err) => alert(err.error?.errorMessage ?? 'Failed')
    });
  }

  closeModals(): void {
    this.showTypeModal = false;
    this.showSubTypeModal = false;
    this.saving = false;
  }
}
