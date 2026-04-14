import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AiAskResponse {
  answer: string;
  sources: AiSource[];
  error?: string;
}

export interface AiSource {
  filename: string;
  chunk_no: number;
  text: string;
  score: number;
}

export interface AiDocument {
  id: string;
  filename: string;
  stored_name: string;
  chunk_count: number;
}

export interface AiUploadResponse {
  message: string;
  filename: string;
  chunks: number;
}

export interface AiHealthResponse {
  status: string;
  model: string;
}

@Injectable({ providedIn: 'root' })
export class AiService {

  constructor(private http: HttpClient) {}

  /** Ask a question — the Flask RAG service retrieves context and generates an answer */
  ask(question: string): Observable<AiAskResponse> {
    return this.http.post<AiAskResponse>('/api/ai/ask', { question });
  }

  /** Upload a .txt or .md file for the AI to index and learn from */
  uploadDocument(file: File): Observable<AiUploadResponse> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<AiUploadResponse>('/api/ai/upload', formData);
  }

  /** List all indexed documents */
  getDocuments(): Observable<AiDocument[]> {
    return this.http.get<AiDocument[]>('/api/ai/documents');
  }

  /** Delete a document and its indexed chunks */
  deleteDocument(docId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`/api/ai/documents/${docId}`);
  }

  /** Health check — verify Ollama + Flask are running */
  healthCheck(): Observable<AiHealthResponse> {
    return this.http.get<AiHealthResponse>('/api/ai/health');
  }
}
