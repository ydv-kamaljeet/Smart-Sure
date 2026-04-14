import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Policy, AdminPolicy, CreatePolicyDto, PagedResult } from '../models/models';

@Injectable({ providedIn: 'root' })
export class PolicyService {
  constructor(private http: HttpClient) {}

  getPolicies(page: number = 1, pageSize: number = 10): Observable<PagedResult<Policy>> {
    return this.http.get<PagedResult<Policy>>(`/api/policy/policies?page=${page}&pageSize=${pageSize}`);
  }

  getPolicyById(id: string): Observable<Policy> {
    return this.http.get<Policy>(`/api/policy/policies/${id}`);
  }

  buyPolicy(payload: CreatePolicyDto): Observable<Policy> {
    return this.http.post<Policy>('/api/policy/policies', payload);
  }

  getInsuranceTypes(): Observable<any[]> {
    return this.http.get<any[]>('/api/policy/insurance-types');
  }

  getInsuranceTypesAdmin(): Observable<any[]> {
    return this.http.get<any[]>('/api/policy/insurance-types/admin');
  }

  getInsuranceSubTypes(typeId: number): Observable<any[]> {
    return this.http.get<any[]>(`/api/policy/insurance-types/${typeId}/subtypes`);
  }

  getInsuranceSubTypesAdmin(typeId: number): Observable<any[]> {
    return this.http.get<any[]>(`/api/policy/insurance-types/${typeId}/subtypes/admin`);
  }

  // Admin Methods
  adminGetPolicies(searchTerm?: string, page: number = 1, pageSize: number = 10, status?: string): Observable<PagedResult<AdminPolicy>> {
    let url = `/api/admin/policies?page=${page}&pageSize=${pageSize}`;
    if (searchTerm) url += `&searchTerm=${searchTerm}`;
    if (status) url += `&status=${status}`;
    return this.http.get<PagedResult<AdminPolicy>>(url);
  }

  adminCancelPolicy(policyId: string): Observable<any> {
    return this.http.put(`/api/policy/policies/${policyId}/cancel`, {});
  }

  // Insurance Type Admin CRUD
  createInsuranceType(dto: { name: string; description: string }): Observable<any> {
    return this.http.post('/api/policy/insurance-types', dto);
  }

  updateInsuranceType(id: number, dto: { name: string; description: string; isActive: boolean }): Observable<any> {
    return this.http.put(`/api/policy/insurance-types/${id}`, dto);
  }

  deleteInsuranceType(id: number): Observable<any> {
    return this.http.delete(`/api/policy/insurance-types/${id}`);
  }

  // Insurance SubType Admin CRUD
  createInsuranceSubType(dto: { insuranceTypeId: number; name: string; description: string; basePremium: number }): Observable<any> {
    return this.http.post('/api/policy/insurance-subtypes', dto);
  }

  updateInsuranceSubType(id: number, dto: { name: string; description: string; basePremium: number; isActive: boolean }): Observable<any> {
    return this.http.put(`/api/policy/insurance-subtypes/${id}`, dto);
  }

  deleteInsuranceSubType(id: number): Observable<any> {
    return this.http.delete(`/api/policy/insurance-subtypes/${id}`);
  }

  // Razorpay
  createRazorpayOrder(amount: number): Observable<{ orderId: string; amount: number; currency: string; keyId: string }> {
    return this.http.post<any>('/api/policy/policies/razorpay/create-order', { amount });
  }

  verifyRazorpayPayment(payload: {
    razorpayOrderId: string;
    razorpayPaymentId: string;
    razorpaySignature: string;
    subTypeId: number;
    vehicleDetails?: any;
    homeDetails?: any;
  }): Observable<{ policyId: string; message: string }> {
    return this.http.post<any>('/api/policy/policies/razorpay/verify-payment', payload);
  }
}
