import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ClaimService } from '../../../services/claim.service';
import { LocaleService } from '../../../services/locale.service';
import { ConvertCurrencyPipe } from '../../../pipes/convert-currency.pipe';
import { AdminClaim, PagedResult } from '../../../models/models';

interface ClaimDocument {
  id: number;
  claimId: number;
  documentType: string;
  fileName: string;
  fileUrl: string;
  uploadedAt: string;
}

@Component({
  selector: 'app-manage-claims',
  standalone: true,
  imports: [CommonModule, FormsModule, ConvertCurrencyPipe],
  templateUrl: './manage-claims.component.html',
  styleUrls: ['./manage-claims.component.css']
})
export class ManageClaimsComponent implements OnInit {
  private claimService = inject(ClaimService);
  private http = inject(HttpClient);
  locale = inject(LocaleService);

  claims: AdminClaim[] = [];
  statusFilter = '';
  page = 1;
  pageSize = 10;
  totalCount = 0;
  loading = true;

  selectedClaim: AdminClaim | null = null;
  documents: ClaimDocument[] = [];
  docsLoading = false;

  ngOnInit(): void {
    this.loadClaims();
  }

  loadClaims(): void {
    this.loading = true;
    this.claimService.adminGetClaims(this.statusFilter, this.page, this.pageSize).subscribe({
      next: (res: PagedResult<AdminClaim>) => {
        this.claims = res.items;
        this.totalCount = res.totalCount;
        this.loading = false;
      },
      error: () => { this.loading = false; }
    });
  }

  openDetail(claim: AdminClaim): void {
    this.selectedClaim = claim;
    this.documents = [];
    this.docsLoading = true;
    this.http.get<ClaimDocument[]>(`/api/claims/${claim.claimId}/documents`).subscribe({
      next: (docs) => { this.documents = docs; this.docsLoading = false; },
      error: () => { this.docsLoading = false; }
    });
  }

  closeDetail(): void {
    this.selectedClaim = null;
    this.documents = [];
  }

  processClaim(id: number, action: 'approve' | 'reject' | 'review'): void {
    const remark = prompt(`Enter remarks for ${action}:`);
    if (remark === null) return;

    const obs = action === 'approve'
      ? this.claimService.adminApproveClaim(id, remark)
      : action === 'reject'
        ? this.claimService.adminRejectClaim(id, remark)
        : this.claimService.adminReviewClaim(id, remark);

    obs.subscribe({
      next: () => { this.loadClaims(); this.closeDetail(); },
      error: (err) => alert('Operation failed: ' + (err.error?.errorMessage || 'Unknown error'))
    });
  }

  changePage(p: number): void { this.page = p; this.loadClaims(); }
  onFilterChange(): void { this.page = 1; this.loadClaims(); }
  get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize); }
}
