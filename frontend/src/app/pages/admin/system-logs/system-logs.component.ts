import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SystemLogService } from '../../../services/system-log.service';
import { LogEntry, LogSearchParameters, LogMetadata } from '../../../models/models';

@Component({
  selector: 'app-system-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './system-logs.component.html',
  styleUrls: ['./system-logs.component.css']
})
export class SystemLogsComponent implements OnInit {
  private logService = inject(SystemLogService);

  logs: LogEntry[] = [];
  metadata: LogMetadata = { services: [], levels: [] };
  params: LogSearchParameters = {
    level: '', service: '', messageContains: '', userId: '', onlyExceptions: false
  };

  loading = false;
  expandedIndex: number | null = null;

  // pagination
  page = 1;
  pageSize = 50;
  totalCount = 0;

  get totalPages(): number { return Math.ceil(this.totalCount / this.pageSize); }

  ngOnInit(): void {
    this.loadMetadata();
    this.search();
  }

  loadMetadata(): void {
    this.logService.getMetadata().subscribe({
      next: res => this.metadata = res,
      error: err => console.error('Error loading log metadata', err)
    });
  }

  search(): void {
    this.loading = true;
    this.expandedIndex = null;
    this.logService.searchLogs({ ...this.params, page: this.page, pageSize: this.pageSize }).subscribe({
      next: res => {
        this.logs = res.items;
        this.totalCount = res.totalCount;
        this.loading = false;
      },
      error: err => { console.error(err); this.loading = false; }
    });
  }

  reset(): void {
    this.params = { level: '', service: '', messageContains: '', userId: '', onlyExceptions: false };
    this.page = 1;
    this.search();
  }

  onFilterChange(): void {
    this.page = 1;
    this.search();
  }

  changePage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.page = p;
    this.search();
  }

  toggleExpand(index: number): void {
    this.expandedIndex = this.expandedIndex === index ? null : index;
  }

  getLevelBadgeClass(level: string): string {
    switch (level.toUpperCase()) {
      case 'ERR': case 'ERROR': case 'CRT': case 'FATAL': return 'bg-danger';
      case 'WRN': case 'WARNING': return 'bg-warning text-dark';
      case 'INF': case 'INFORMATION': return 'bg-primary';
      default: return 'bg-secondary';
    }
  }
}
