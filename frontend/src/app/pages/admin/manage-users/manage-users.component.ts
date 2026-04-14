import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { PagedResult } from '../../../models/models';

interface AdminUser {
  id: number;
  userId: string;
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
  lastLogin: string;
  createdAt: string;
}

@Component({
  selector: 'app-manage-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h5 class="fw-bold mb-0">Manage Users</h5>
    </div>

    <div class="table-container">
      <div class="px-3 py-3 border-bottom d-flex gap-2 flex-wrap">
        <input class="form-control form-control-sm" style="max-width:220px;"
               placeholder="Search name or email..." [(ngModel)]="searchTerm" (input)="onSearch()">
        <select class="form-select form-select-sm" style="max-width:150px;" [(ngModel)]="roleFilter" (change)="onSearch()">
          <option value="">All Roles</option>
          <option value="Policyholder">Policyholder</option>
          <option value="Admin">Admin</option>
        </select>
      </div>

      <div *ngIf="loading" class="text-center py-5">
        <div class="spinner-border text-primary"></div>
      </div>

      <div class="table-responsive" *ngIf="!loading">
        <table class="table table-hover mb-0 small">
          <thead>
            <tr>
              <th>Name</th>
              <th>Email</th>
              <th>Role</th>
              <th>Status</th>
              <th>Last Login</th>
              <th>Joined</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let u of users">
              <td class="fw-medium">{{ u.fullName }}</td>
              <td class="text-muted">{{ u.email }}</td>
              <td><span class="badge" [class]="u.role === 'Admin' ? 'bg-primary' : 'bg-secondary'">{{ u.role }}</span></td>
              <td>
                <span class="badge rounded-pill" [class]="u.isActive ? 'bg-success' : 'bg-danger'">
                  {{ u.isActive ? 'Active' : 'Inactive' }}
                </span>
              </td>
              <td class="text-muted">{{ u.lastLogin | date:'mediumDate' }}</td>
              <td class="text-muted">{{ u.createdAt | date:'mediumDate' }}</td>
              <td>
                <div class="d-flex gap-1">
                  <button class="btn btn-sm btn-outline-primary" (click)="openRoleModal(u)"
                          title="Change Role">
                    <i class="bi bi-person-gear"></i>
                  </button>
                  <button class="btn btn-sm btn-outline-danger" (click)="deleteUser(u.userId)"
                          [disabled]="u.role === 'Admin'" title="Deactivate">
                    <i class="bi bi-person-x"></i>
                  </button>
                </div>
              </td>
            </tr>
            <tr *ngIf="users.length === 0">
              <td colspan="7" class="text-center text-muted py-4">No users found.</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <div class="d-flex justify-content-between align-items-center px-3 py-2 border-top small text-muted" *ngIf="totalCount > pageSize">
        <span>Showing {{ (page - 1) * pageSize + 1 }}–{{ min(page * pageSize, totalCount) }} of {{ totalCount }}</span>
        <div class="d-flex gap-1">
          <button class="btn btn-sm btn-outline-secondary" [disabled]="page === 1" (click)="changePage(page - 1)">‹</button>
          <button class="btn btn-sm btn-outline-secondary" [disabled]="page >= totalPages" (click)="changePage(page + 1)">›</button>
        </div>
      </div>
    </div>

    <!-- Change Role Modal -->
    <div class="modal fade show d-block" style="background:rgba(0,0,0,0.5)" *ngIf="showRoleModal && selectedUser">
      <div class="modal-dialog modal-sm">
        <div class="modal-content">
          <div class="modal-header">
            <h6 class="modal-title fw-semibold">Change Role</h6>
            <button class="btn-close" (click)="showRoleModal = false"></button>
          </div>
          <div class="modal-body">
            <p class="small text-muted mb-3">User: <strong>{{ selectedUser.fullName }}</strong></p>
            <p class="small text-muted mb-3">Current role: <span class="badge bg-secondary">{{ selectedUser.role }}</span></p>
            <label class="form-label small fw-medium">Assign New Role</label>
            <select class="form-select form-select-sm" [(ngModel)]="newRole">
              <option value="Policyholder">Policyholder</option>
              <option value="Admin">Admin</option>
            </select>
          </div>
          <div class="modal-footer">
            <button class="btn btn-secondary btn-sm" (click)="showRoleModal = false">Cancel</button>
            <button class="btn btn-primary btn-sm" (click)="saveRole()" [disabled]="saving">
              <span *ngIf="saving" class="spinner-border spinner-border-sm me-1"></span>
              Save
            </button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class ManageUsersComponent implements OnInit {
  private http = inject(HttpClient);

  users: AdminUser[] = [];
  searchTerm = '';
  roleFilter = '';
  page = 1;
  pageSize = 10;
  totalCount = 0;
  loading = true;

  showRoleModal = false;
  selectedUser: AdminUser | null = null;
  newRole = 'Policyholder';
  saving = false;

  ngOnInit(): void { this.loadUsers(); }

  loadUsers(): void {
    this.loading = true;
    let url = `/api/admin/users?page=${this.page}&pageSize=${this.pageSize}`;
    if (this.searchTerm) url += `&searchTerm=${encodeURIComponent(this.searchTerm)}`;
    if (this.roleFilter) url += `&role=${this.roleFilter}`;
    this.http.get<PagedResult<AdminUser>>(url).subscribe({
      next: (res) => { this.users = res.items; this.totalCount = res.totalCount; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  openRoleModal(user: AdminUser): void {
    this.selectedUser = user;
    this.newRole = user.role === 'Admin' ? 'Policyholder' : 'Admin';
    this.showRoleModal = true;
  }

  saveRole(): void {
    if (!this.selectedUser) return;
    this.saving = true;
    this.http.put(`/api/auth/users/${this.selectedUser.userId}/roles`, { roleName: this.newRole }).subscribe({
      next: () => { this.showRoleModal = false; this.saving = false; this.loadUsers(); },
      error: (err) => { alert(err.error?.errorMessage ?? 'Failed to change role'); this.saving = false; }
    });
  }

  deleteUser(userId: string): void {
    if (!confirm('Deactivate this user?')) return;
    this.http.delete(`/api/admin/users/${userId}`).subscribe({
      next: () => this.loadUsers(),
      error: (err) => alert('Failed: ' + (err.error?.errorMessage ?? 'Unknown error'))
    });
  }

  onSearch(): void { this.page = 1; this.loadUsers(); }
  changePage(p: number): void { this.page = p; this.loadUsers(); }
  get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize); }
  min(a: number, b: number): number { return Math.min(a, b); }
}
