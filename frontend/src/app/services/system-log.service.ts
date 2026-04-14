import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LogEntry, LogSearchParameters, LogMetadata, PagedLogResult } from '../models/models';

@Injectable({ providedIn: 'root' })
export class SystemLogService {
  private http = inject(HttpClient);
  private apiUrl = '/api/admin/system-logs';

  searchLogs(params: LogSearchParameters): Observable<PagedLogResult> {
    let httpParams = new HttpParams();
    // Send datetime as-is (local time) — backend compares with local log file dates
    if (params.from) httpParams = httpParams.set('from', params.from);
    if (params.to) httpParams = httpParams.set('to', params.to);
    if (params.level) httpParams = httpParams.set('level', params.level);
    if (params.service) httpParams = httpParams.set('service', params.service);
    if (params.userId) httpParams = httpParams.set('userId', params.userId);
    if (params.messageContains) httpParams = httpParams.set('messageContains', params.messageContains);
    if (params.onlyExceptions !== undefined) httpParams = httpParams.set('onlyExceptions', params.onlyExceptions.toString());
    if (params.page) httpParams = httpParams.set('page', params.page.toString());
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());
    return this.http.get<PagedLogResult>(`${this.apiUrl}/search`, { params: httpParams });
  }

  getMetadata(): Observable<LogMetadata> {
    return this.http.get<LogMetadata>(`${this.apiUrl}/metadata`);
  }
}
