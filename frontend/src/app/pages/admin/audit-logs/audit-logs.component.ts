import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DashboardService } from '../../../services/dashboard.service';
import { LocaleService } from '../../../services/locale.service';
import { AuditLog, PagedResult } from '../../../models/models';

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './audit-logs.component.html',
  styleUrls: ['./audit-logs.component.css']
})
export class AuditLogComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  locale = inject(LocaleService);
  protected Math = Math;

  logs: AuditLog[] = [];
  page = 1;
  pageSize = 10;
  totalCount = 0;
  loading = true;

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(): void {
    this.loading = true;
    this.dashboardService.getAuditLogs(this.page, this.pageSize).subscribe({
      next: (res: PagedResult<AuditLog>) => {
        this.logs = res.items;
        this.totalCount = res.totalCount;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  changePage(p: number): void {
    this.page = p;
    this.loadLogs();
  }

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }
}
