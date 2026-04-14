import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PolicyService } from '../../../services/policy.service';
import { LocaleService } from '../../../services/locale.service';
import { ConvertCurrencyPipe } from '../../../pipes/convert-currency.pipe';
import { AdminPolicy, PagedResult } from '../../../models/models';

@Component({
  selector: 'app-manage-policies',
  standalone: true,
  imports: [CommonModule, FormsModule, ConvertCurrencyPipe],
  templateUrl: './manage-policies.component.html',
  styleUrls: ['./manage-policies.component.css']
})
export class ManagePoliciesComponent implements OnInit {
  private policyService = inject(PolicyService);
  locale = inject(LocaleService);

  policies: AdminPolicy[] = [];
  searchTerm = '';
  statusFilter = '';
  page = 1;
  pageSize = 10;
  totalCount = 0;
  loading = true;

  ngOnInit(): void {
    this.loadPolicies();
  }

  loadPolicies(): void {
    this.loading = true;
    this.policyService.adminGetPolicies(this.searchTerm, this.page, this.pageSize, this.statusFilter).subscribe({
      next: (res: PagedResult<AdminPolicy>) => {
        this.policies = res.items;
        this.totalCount = res.totalCount;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  cancelPolicy(id: string): void {
    if (confirm('Are you sure you want to cancel this policy? This cannot be undone.')) {
      this.policyService.adminCancelPolicy(id).subscribe({
        next: () => this.loadPolicies(),
        error: (err: any) => alert('Failed to cancel policy: ' + (err.error?.errorMessage || 'Unknown error'))
      });
    }
  }

  changePage(p: number): void {
    this.page = p;
    this.loadPolicies();
  }

  onSearch(): void {
    this.page = 1;
    this.loadPolicies();
  }

  onStatusFilter(): void {
    this.page = 1;
    this.loadPolicies();
  }

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }
}
