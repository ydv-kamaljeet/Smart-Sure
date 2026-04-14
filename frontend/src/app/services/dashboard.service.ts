import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, map, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { DashboardData, Policy, Claim, AdminDashboardData, AuditLog, PagedResult } from '../models/models';
import { PolicyService } from './policy.service';
import { ClaimService } from './claim.service';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);
  private policyService = inject(PolicyService);
  private claimService = inject(ClaimService);

  getDashboard(): Observable<DashboardData> {
    return forkJoin({
      policies: this.policyService.getPolicies().pipe(map(res => res.items), catchError(() => of([] as Policy[]))),
      claims: this.claimService.getClaims().pipe(map(res => res.items), catchError(() => of([] as Claim[])))
    }).pipe(
      map(({ policies, claims }): DashboardData => {
        const activePolicies = policies.filter(p => p.status === 'Active' || p.status === 'ACTIVE').length;
        const totalPremium = policies
          .filter(p => p.status === 'Active' || p.status === 'ACTIVE')
          .reduce((acc, p) => acc + (p.premiumAmount ?? 0), 0);
        const pendingClaims = claims.filter(c => ['Submitted', 'Pending', 'In Review'].includes(c.status)).length;
        
        return {
          activePolicies,
          totalPremium,
          pendingClaims,
          totalClaims: claims.length,
          policyByStatus: this.groupBy(policies, 'status'),
          claimByStatus: this.groupBy(claims, 'status'),
          claimsByMonth: this.getClaimsByMonth(claims),
          recentPolicies: policies.slice(0, 5),
          recentClaims: claims.slice(0, 5)
        };
      })
    );
  }

  getAdminDashboard(): Observable<AdminDashboardData> {
    return this.http.get<AdminDashboardData>('/api/admin/dashboard');
  }

  getAuditLogs(page: number = 1, pageSize: number = 10): Observable<PagedResult<AuditLog>> {
    return this.http.get<PagedResult<AuditLog>>(`/api/admin/audit-logs?page=${page}&pageSize=${pageSize}`);
  }

  private groupBy(items: any[], key: string): { status: string; count: number }[] {
    const counts = items.reduce((acc, item) => {
      const val = item[key] || 'Unknown';
      acc[val] = (acc[val] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);

    return Object.entries(counts).map(([status, count]): { status: string; count: number } => ({ 
      status, 
      count: count as number 
    }));
  }

  private getClaimsByMonth(claims: Claim[]): { month: string; amount: number }[] {
    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const monthlyData = claims.reduce((acc, claim) => {
      const date = new Date(claim.createdAt);
      const month = months[date.getMonth()];
      acc[month] = (acc[month] || 0) + claim.claimAmount;
      return acc;
    }, {} as Record<string, number>);

    return Object.entries(monthlyData).map(([month, amount]) => ({ month, amount }));
  }
}
