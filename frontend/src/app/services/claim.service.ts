import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Claim, AdminClaim, CreateClaimDto, PagedResult } from '../models/models';

@Injectable({ providedIn: 'root' })
export class ClaimService {
  constructor(private http: HttpClient) {}

  getClaims(page: number = 1, pageSize: number = 10, status?: string): Observable<PagedResult<Claim>> {
    let url = `/api/claims?page=${page}&pageSize=${pageSize}`;
    if (status) url += `&status=${status}`;
    return this.http.get<PagedResult<Claim>>(url);
  }

  getClaimById(id: number): Observable<Claim> {
    return this.http.get<Claim>(`/api/claims/${id}`);
  }

  initiateClaim(payload: CreateClaimDto): Observable<Claim> {
    return this.http.post<Claim>('/api/claims', payload);
  }

  uploadDocument(claimId: string, file: File, description: string = 'Claim Document'): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('title', description);
    formData.append('documentType', 'ClaimSupport');

    return this.http.post(`/api/claims/${claimId}/documents`, formData);
  }

  // Admin Methods
  adminGetClaims(status?: string, page: number = 1, pageSize: number = 10): Observable<PagedResult<AdminClaim>> {
    let url = `/api/admin/claims?page=${page}&pageSize=${pageSize}`;
    if (status) url += `&status=${status}`;
    return this.http.get<PagedResult<AdminClaim>>(url);
  }

  adminReviewClaim(claimId: number, remarks: string): Observable<any> {
    return this.http.put(`/api/admin/claims/${claimId}/review`, { remarks });
  }

  adminApproveClaim(claimId: number, remarks: string): Observable<any> {
    return this.http.put(`/api/admin/claims/${claimId}/approve`, { remarks });
  }

  adminRejectClaim(claimId: number, remarks: string): Observable<any> {
    return this.http.put(`/api/admin/claims/${claimId}/reject`, { remarks });
  }
}
