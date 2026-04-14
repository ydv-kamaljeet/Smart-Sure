import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ClaimService } from '../../../services/claim.service';
import { PolicyService } from '../../../services/policy.service';
import { Policy, CreateClaimDto, Claim, PagedResult } from '../../../models/models';

@Component({
  selector: 'app-initiate-claim',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './initiate-claim.component.html',
  styleUrls: ['./initiate-claim.component.css']
})
export class InitiateClaimComponent implements OnInit {
  private claimService = inject(ClaimService);
  private policyService = inject(PolicyService);
  private router = inject(Router);

  policies: Policy[] = [];
  selectedPolicy: Policy | null = null;
  loading = true;
  submitting = false;
  error: string | null = null;
  selectedFile: File | null = null;

  today = new Date().toISOString().split('T')[0];
  minIncidentDate = '';

  claimData: CreateClaimDto = {
    policyId: '',
    incidentDate: new Date().toISOString().split('T')[0],
    description: '',
    claimAmount: 0
  };

  ngOnInit(): void {
    this.policyService.getPolicies(1, 100).subscribe({
      next: (res: PagedResult<Policy>) => {
        this.policies = res.items.filter((p: Policy) => p.status === 'Active' || p.status === 'ACTIVE');
        this.loading = false;
        if (this.policies.length > 0) {
          this.claimData.policyId = this.policies[0].id;
          this.onPolicyChange();
        }
      },
      error: () => {
        this.error = 'Failed to load policies. Please ensure you have an active policy.';
        this.loading = false;
      }
    });
  }

  onPolicyChange(): void {
    this.selectedPolicy = this.policies.find(p => p.id === this.claimData.policyId) ?? null;
    this.minIncidentDate = this.selectedPolicy?.startDate
      ? new Date(this.selectedPolicy.startDate).toISOString().split('T')[0]
      : '';
    this.claimData.claimAmount = 0;
  }

  isDateAfterToday(date: string): boolean {
    return !!date && date > this.today;
  }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.selectedFile = file;
    }
  }

  onSubmit(): void {
    if (!this.selectedFile) {
      this.error = 'Please upload a support document (PDF/Image).';
      return;
    }

    this.submitting = true;
    this.error = null;

    this.claimService.initiateClaim(this.claimData).subscribe({
      next: (claim: Claim) => {
        this.claimService.uploadDocument(claim.id.toString(), this.selectedFile!, 'Claim Support Document').subscribe({
          next: () => {
            this.router.navigate(['/dashboard']);
          },
          error: (err: any) => {
            this.error = 'Claim initiated, but document upload failed. You can upload it later from the claim details.';
            this.submitting = false;
          }
        });
      },
      error: (err: any) => {
        this.error = err.error?.errorMessage || 'Failed to initiate claim. Please try again.';
        this.submitting = false;
      }
    });
  }
}
