import { Component, OnInit, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AiService, AiDocument, AiSource } from '../../services/ai.service';
import { AuthService } from '../../services/auth.service';

interface ChatMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  sources?: AiSource[];
  timestamp: Date;
  loading?: boolean;
}

@Component({
  selector: 'app-ai-assistant',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="ai-container">
      <!-- Left: Chat Panel -->
      <div class="chat-panel">
        <div class="chat-header">
          <div class="chat-header-left">
            <div class="ai-avatar">
              <i class="bi bi-robot"></i>
            </div>
            <div>
              <h5 class="mb-0">SmartSure AI Assistant</h5>
              <span class="status-indicator" [class.online]="isOnline" [class.offline]="!isOnline">
                <span class="status-dot"></span>
                {{ isOnline ? 'Online' : 'Offline — start Ollama' }}
              </span>
            </div>
          </div>
          <div class="chat-header-actions">
            <button *ngIf="messages.length > 1" class="btn btn-link text-danger p-0 me-3" (click)="clearChat()" title="Clear Chat History">
              <i class="bi bi-trash-fill"></i>
            </button>
            <button *ngIf="isAdmin" class="btn btn-sm btn-outline-secondary d-lg-none" (click)="showDocs = !showDocs">
              <i class="bi bi-files"></i>
            </button>
          </div>
        </div>

        <div class="chat-messages" #chatContainer>
          <div *ngFor="let msg of messages" class="message-row" [class.user-row]="msg.role === 'user'" [class.assistant-row]="msg.role === 'assistant'" [class.system-row]="msg.role === 'system'">
            <!-- Avatar -->
            <div class="msg-avatar" *ngIf="msg.role === 'assistant'">
              <i class="bi bi-robot"></i>
            </div>
            <div class="msg-avatar user-avatar-bubble" *ngIf="msg.role === 'user'">
              <i class="bi bi-person-fill"></i>
            </div>

            <div class="msg-bubble" [class.user-bubble]="msg.role === 'user'" [class.assistant-bubble]="msg.role === 'assistant'" [class.system-bubble]="msg.role === 'system'">
              <!-- Loading dots -->
              <div *ngIf="msg.loading" class="typing-indicator">
                <span></span><span></span><span></span>
              </div>
              <!-- Content -->
              <div *ngIf="!msg.loading" class="msg-content" [innerHTML]="formatMessage(msg.content)"></div>
              <!-- Sources accordion -->
              <div *ngIf="msg.sources && msg.sources.length > 0" class="sources-section">
                <button class="sources-toggle" (click)="msg._showSources = !msg._showSources">
                  <i class="bi" [class.bi-chevron-right]="!msg._showSources" [class.bi-chevron-down]="msg._showSources"></i>
                  {{ msg.sources.length }} source{{ msg.sources.length > 1 ? 's' : '' }}
                </button>
                <div *ngIf="msg._showSources" class="sources-list">
                  <div *ngFor="let src of msg.sources" class="source-chip">
                    <i class="bi bi-file-earmark-text"></i>
                    <span class="source-name">{{ src.filename }}</span>
                    <span class="source-chunk">chunk {{ src.chunk_no }}</span>
                    <span class="source-score">{{ (src.score * 100).toFixed(0) }}%</span>
                  </div>
                </div>
              </div>
              <div class="msg-time">{{ msg.timestamp | date:'shortTime' }}</div>
            </div>
          </div>
        </div>

        <!-- Input area -->
        <div class="chat-input-area">
          <div class="chat-input-wrapper">
            <input
              #questionInput
              type="text"
              class="chat-input"
              placeholder="Ask us anything..."
              [(ngModel)]="question"
              (keydown.enter)="sendQuestion()"
              [disabled]="isLoading"
              id="ai-question-input"
            />
            <button
              class="send-btn"
              (click)="sendQuestion()"
              [disabled]="isLoading || !question.trim()"
              id="ai-send-btn"
            >
              <i class="bi" [class.bi-send-fill]="!isLoading" [class.bi-hourglass-split]="isLoading"></i>
            </button>
          </div>
          <p class="input-hint">Powered by Ollama · llama3.2:1b · Local RAG</p>
        </div>
      </div>

      <!-- Right: Document Panel (Admin Only) -->
      <div *ngIf="isAdmin" class="docs-panel" [class.show-mobile]="showDocs">
        <div class="docs-header">
          <h6><i class="bi bi-archive me-2"></i>Knowledge Base</h6>
          <button class="btn btn-sm btn-outline-secondary d-lg-none" (click)="showDocs = false">
            <i class="bi bi-x-lg"></i>
          </button>
        </div>

        <!-- Upload (Admin Only) -->
        <div *ngIf="isAdmin" class="upload-zone" (click)="fileInput.click()" (dragover)="onDragOver($event)" (drop)="onDrop($event)">
          <input #fileInput type="file" accept=".txt,.md" (change)="onFileSelected($event)" hidden id="ai-file-input" />
          <div *ngIf="!isUploading">
            <i class="bi bi-cloud-arrow-up upload-icon"></i>
            <p class="upload-text">Drop <code>.txt</code> or <code>.md</code> files here</p>
            <p class="upload-hint">or click to browse</p>
          </div>
          <div *ngIf="isUploading" class="text-center">
            <div class="spinner-border spinner-border-sm text-primary" role="status"></div>
            <p class="upload-text mt-2">Indexing...</p>
          </div>
        </div>

        <div *ngIf="isAdmin && uploadMessage" class="alert alert-sm mt-2" [class.alert-success]="!uploadError" [class.alert-danger]="uploadError" role="alert">
          {{ uploadMessage }}
        </div>

        <!-- Document list -->
        <div class="doc-list">
          <div *ngIf="documents.length === 0" class="empty-docs">
            <i class="bi bi-inbox"></i>
            <p>No documents indexed yet</p>
          </div>
          <div *ngFor="let doc of documents" class="doc-item">
            <div class="doc-info">
              <i class="bi bi-file-earmark-text doc-icon"></i>
              <div>
                <span class="doc-name">{{ doc.filename }}</span>
                <span class="doc-meta">{{ doc.chunk_count }} chunks</span>
              </div>
            </div>
            <!-- Delete (Admin Only) -->
            <button *ngIf="isAdmin" class="btn btn-sm doc-delete" (click)="deleteDoc(doc)" title="Remove document" id="ai-delete-doc-{{doc.id}}">
              <i class="bi bi-trash3"></i>
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    /* (Styles remain unchanged) */
    :host { display: block; height: calc(100vh - 64px); overflow: hidden; }

    .ai-container {
      display: flex;
      height: 100%;
      gap: 0;
      background: #f8fafc;
    }

    .chat-panel {
      flex: 1;
      display: flex;
      flex-direction: column;
      min-width: 0;
      background: #ffffff;
      border-right: 1px solid #e2e8f0;
    }

    .chat-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1rem 1.5rem;
      border-bottom: 1px solid #e2e8f0;
      background: linear-gradient(135deg, #f0fdfa, #ecfdf5);
    }

    .chat-header-left {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .chat-header h5 {
      font-size: 1rem;
      font-weight: 700;
      color: #1e293b;
    }

    .ai-avatar {
      width: 42px;
      height: 42px;
      border-radius: 14px;
      background: linear-gradient(135deg, #064e3b, #0d9488);
      display: flex;
      align-items: center;
      justify-content: center;
      color: #fff;
      font-size: 1.25rem;
      box-shadow: 0 4px 12px rgba(13,148,136,0.3);
    }

    .status-indicator {
      font-size: 0.75rem;
      display: flex;
      align-items: center;
      gap: 0.35rem;
    }

    .status-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      display: inline-block;
    }

    .status-indicator.online { color: #059669; }
    .status-indicator.online .status-dot { background: #10b981; box-shadow: 0 0 6px #10b981; }
    .status-indicator.offline { color: #dc2626; }
    .status-indicator.offline .status-dot { background: #ef4444; }

    .chat-messages {
      flex: 1;
      overflow-y: auto;
      padding: 1.5rem;
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .message-row {
      display: flex;
      gap: 0.625rem;
      max-width: 85%;
      animation: fadeInUp 0.3s ease;
    }

    .user-row { align-self: flex-end; flex-direction: row-reverse; }
    .assistant-row { align-self: flex-start; }
    .system-row { align-self: center; max-width: 70%; }

    @keyframes fadeInUp {
      from { opacity: 0; transform: translateY(8px); }
      to { opacity: 1; transform: translateY(0); }
    }

    .msg-avatar {
      width: 32px;
      height: 32px;
      border-radius: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 0.9rem;
      flex-shrink: 0;
      background: linear-gradient(135deg, #064e3b, #0d9488);
      color: #fff;
    }

    .user-avatar-bubble {
      background: linear-gradient(135deg, #1e40af, #3b82f6);
    }

    .msg-bubble {
      padding: 0.75rem 1rem;
      border-radius: 16px;
      font-size: 0.875rem;
      line-height: 1.6;
      position: relative;
      word-break: break-word;
    }

    .user-bubble {
      background: linear-gradient(135deg, #0f766e, #0d9488);
      color: #fff;
      border-bottom-right-radius: 4px;
    }

    .assistant-bubble {
      background: #f1f5f9;
      color: #1e293b;
      border: 1px solid #e2e8f0;
      border-bottom-left-radius: 4px;
    }

    .system-bubble {
      background: #fef3c7;
      color: #92400e;
      text-align: center;
      font-size: 0.8rem;
      border-radius: 8px;
    }

    .msg-content { white-space: pre-wrap; }
    .msg-time {
      font-size: 0.65rem;
      margin-top: 0.35rem;
      opacity: 0.5;
    }

    .typing-indicator {
      display: flex;
      gap: 4px;
      padding: 4px 0;
    }

    .typing-indicator span {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: #94a3b8;
      animation: bounce 1.4s infinite ease-in-out both;
    }

    .typing-indicator span:nth-child(1) { animation-delay: -0.32s; }
    .typing-indicator span:nth-child(2) { animation-delay: -0.16s; }

    @keyframes bounce {
      0%, 80%, 100% { transform: scale(0.6); opacity: 0.4; }
      40% { transform: scale(1); opacity: 1; }
    }

    .sources-section {
      margin-top: 0.5rem;
      border-top: 1px solid rgba(0,0,0,0.06);
      padding-top: 0.35rem;
    }

    .sources-toggle {
      background: none;
      border: none;
      font-size: 0.75rem;
      color: #0f766e;
      font-weight: 600;
      padding: 0;
      display: flex;
      align-items: center;
      gap: 0.25rem;
      cursor: pointer;
    }

    .sources-list {
      margin-top: 0.35rem;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .source-chip {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      font-size: 0.7rem;
      background: rgba(15,118,110,0.06);
      padding: 0.25rem 0.5rem;
      border-radius: 6px;
      color: #334155;
    }

    .source-name { font-weight: 600; }
    .source-chunk { color: #64748b; }
    .source-score {
      margin-left: auto;
      background: #d1fae5;
      color: #065f46;
      padding: 0 0.375rem;
      border-radius: 4px;
      font-weight: 700;
    }

    .chat-input-area {
      padding: 1rem 1.5rem;
      border-top: 1px solid #e2e8f0;
      background: #f8fafc;
    }

    .chat-input-wrapper {
      display: flex;
      align-items: center;
      background: #ffffff;
      border: 2px solid #e2e8f0;
      border-radius: 14px;
      padding: 0.25rem 0.25rem 0.25rem 1rem;
      transition: border-color 0.2s;
    }

    .chat-input-wrapper:focus-within {
      border-color: #0d9488;
      box-shadow: 0 0 0 3px rgba(13,148,136,0.1);
    }

    .chat-input {
      flex: 1;
      border: none;
      outline: none;
      font-size: 0.9rem;
      background: transparent;
      color: #1e293b;
    }

    .chat-input::placeholder { color: #94a3b8; }

    .send-btn {
      width: 40px;
      height: 40px;
      border-radius: 10px;
      border: none;
      background: linear-gradient(135deg, #064e3b, #0d9488);
      color: #fff;
      font-size: 1rem;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.2s;
      flex-shrink: 0;
    }

    .send-btn:hover:not(:disabled) {
      transform: scale(1.05);
      box-shadow: 0 4px 12px rgba(13,148,136,0.35);
    }

    .send-btn:disabled { opacity: 0.4; }

    .input-hint {
      font-size: 0.65rem;
      color: #94a3b8;
      text-align: center;
      margin: 0.5rem 0 0;
    }

    .docs-panel {
      width: 320px;
      display: flex;
      flex-direction: column;
      background: #ffffff;
      border-left: 1px solid #e2e8f0;
    }

    .docs-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1rem 1.25rem;
      border-bottom: 1px solid #e2e8f0;
    }

    .docs-header h6 {
      margin: 0;
      font-size: 0.9rem;
      font-weight: 700;
      color: #1e293b;
    }

    .upload-zone {
      margin: 1rem;
      padding: 1.5rem;
      border: 2px dashed #cbd5e1;
      border-radius: 12px;
      text-align: center;
      cursor: pointer;
      transition: all 0.2s;
      background: #f8fafc;
    }

    .upload-zone:hover {
      border-color: #0d9488;
      background: #f0fdfa;
    }

    .upload-icon {
      font-size: 2rem;
      color: #0d9488;
      display: block;
      margin-bottom: 0.5rem;
    }

    .upload-text { font-size: 0.8rem; color: #475569; margin: 0; }
    .upload-hint { font-size: 0.7rem; color: #94a3b8; margin: 0.25rem 0 0; }

    .alert-sm {
      margin: 0 1rem;
      padding: 0.5rem 0.75rem;
      font-size: 0.8rem;
      border-radius: 8px;
    }

    .doc-list {
      flex: 1;
      overflow-y: auto;
      padding: 0.75rem 1rem;
    }

    .empty-docs {
      text-align: center;
      padding: 2rem 1rem;
      color: #94a3b8;
    }

    .empty-docs i { font-size: 2rem; display: block; margin-bottom: 0.5rem; }
    .empty-docs p { font-size: 0.8rem; margin: 0; }

    .doc-item {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0.625rem 0.75rem;
      border-radius: 10px;
      margin-bottom: 0.375rem;
      border: 1px solid #f1f5f9;
      transition: all 0.15s;
    }

    .doc-item:hover {
      background: #f0fdfa;
      border-color: #ccfbf1;
    }

    .doc-info {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      min-width: 0;
    }

    .doc-icon { font-size: 1.1rem; color: #0f766e; flex-shrink: 0; }
    .doc-name {
      font-size: 0.8rem;
      font-weight: 600;
      color: #334155;
      display: block;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      max-width: 160px;
    }

    .doc-meta {
      font-size: 0.65rem;
      color: #94a3b8;
    }

    .doc-delete {
      color: #94a3b8;
      border: none;
      background: none;
      padding: 0.25rem;
      border-radius: 6px;
      transition: all 0.15s;
    }

    .doc-delete:hover { color: #dc2626; background: #fee2e2; }

    @media (max-width: 991.98px) {
      .docs-panel {
        display: none;
        position: fixed;
        right: 0;
        top: 64px;
        bottom: 0;
        z-index: 1001;
        box-shadow: -8px 0 24px rgba(0,0,0,0.12);
        width: 300px;
      }

      .docs-panel.show-mobile {
        display: flex;
      }
    }
  `]
})
export class AiAssistantComponent implements OnInit, AfterViewChecked {
  @ViewChild('chatContainer') chatContainer!: ElementRef;
  @ViewChild('questionInput') questionInput!: ElementRef;

  messages: (ChatMessage & { _showSources?: boolean })[] = [];
  question = '';
  isLoading = false;
  isOnline = false;
  isAdmin = false;
  documents: AiDocument[] = [];
  showDocs = false;

  isUploading = false;
  uploadMessage = '';
  uploadError = false;

  private shouldScroll = false;

  constructor(
    private aiService: AiService,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    this.checkHealth();
    this.loadDocuments();
    
    const user = this.auth.getUser();
    this.isAdmin = user?.roles?.includes('Admin') ?? false;

    this.loadHistory();
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  loadHistory(): void {
    const user = this.auth.getUser();
    if (!user) return;

    const storageKey = `smartsure_ai_chat_${user.email}`;
    const saved = localStorage.getItem(storageKey);
    
    if (saved) {
      try {
        this.messages = JSON.parse(saved);
        this.messages.forEach(m => m._showSources = false);
        this.shouldScroll = true;
      } catch (e) {
        this.addSystemMessage('Welcome to SmartSure AI Assistant Space, how may I help you?');
      }
    } else {
      this.addSystemMessage('Welcome to SmartSure AI Assistant Space, how may I help you?');
    }
  }

  saveHistory(): void {
    const user = this.auth.getUser();
    if (!user) return;

    const storageKey = `smartsure_ai_chat_${user.email}`;
    const toSave = this.messages.filter(m => !m.loading);
    localStorage.setItem(storageKey, JSON.stringify(toSave));
  }

  clearChat(): void {
    const user = this.auth.getUser();
    if (!user) return;

    const storageKey = `smartsure_ai_chat_${user.email}`;
    if (confirm('Are you sure you want to clear your chat history?')) {
      this.messages = [];
      localStorage.removeItem(storageKey);
      this.addSystemMessage('Welcome to SmartSure AI Assistant Space, how may I help you?');
    }
  }

  checkHealth(): void {
    this.aiService.healthCheck().subscribe({
      next: () => this.isOnline = true,
      error: () => this.isOnline = false,
    });
  }

  loadDocuments(): void {
    this.aiService.getDocuments().subscribe({
      next: docs => this.documents = docs,
      error: () => {}
    });
  }

  sendQuestion(): void {
    const q = this.question.trim();
    if (!q || this.isLoading) return;

    this.messages.push({
      id: crypto.randomUUID(),
      role: 'user',
      content: q,
      timestamp: new Date(),
    });

    this.question = '';
    this.isLoading = true;
    this.shouldScroll = true;
    this.saveHistory();

    const loadingId = crypto.randomUUID();
    this.messages.push({
      id: loadingId,
      role: 'assistant',
      content: '',
      timestamp: new Date(),
      loading: true,
    });
    this.shouldScroll = true;

    this.aiService.ask(q).subscribe({
      next: res => {
        const idx = this.messages.findIndex(m => m.id === loadingId);
        if (idx !== -1) {
          this.messages[idx] = {
            id: loadingId,
            role: 'assistant',
            content: res.answer,
            sources: res.sources,
            timestamp: new Date(),
          };
        }
        this.isLoading = false;
        this.shouldScroll = true;
        this.saveHistory();
      },
      error: err => {
        const idx = this.messages.findIndex(m => m.id === loadingId);
        if (idx !== -1) {
          this.messages[idx] = {
            id: loadingId,
            role: 'assistant',
            content: err.error?.error || 'Something went wrong. Is Ollama running?',
            timestamp: new Date(),
          };
        }
        this.isLoading = false;
        this.shouldScroll = true;
        this.saveHistory();
      }
    });
  }

  onFileSelected(event: Event): void {
    if (!this.isAdmin) return;
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      this.uploadFile(input.files[0]);
      input.value = '';
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
  }

  onDrop(event: DragEvent): void {
    if (!this.isAdmin) return;
    event.preventDefault();
    event.stopPropagation();
    if (event.dataTransfer?.files?.length) {
      this.uploadFile(event.dataTransfer.files[0]);
    }
  }

  uploadFile(file: File): void {
    this.isUploading = true;
    this.uploadMessage = '';
    this.uploadError = false;

    this.aiService.uploadDocument(file).subscribe({
      next: res => {
        this.uploadMessage = res.message;
        this.isUploading = false;
        this.loadDocuments();
        this.addSystemMessage(`📄 "${res.filename}" indexed (${res.chunks} chunks).`);
        this.saveHistory();
      },
      error: err => {
        this.uploadMessage = err.error?.error || 'Upload failed.';
        this.uploadError = true;
        this.isUploading = false;
      }
    });
  }

  deleteDoc(doc: AiDocument): void {
    if (!this.isAdmin) return;
    this.aiService.deleteDocument(doc.id).subscribe({
      next: () => {
        this.documents = this.documents.filter(d => d.id !== doc.id);
        this.addSystemMessage(`🗑️ Removed "${doc.filename}" from knowledge base.`);
        this.saveHistory();
      },
      error: () => {
        this.addSystemMessage(`Failed to remove "${doc.filename}".`);
      }
    });
  }

  formatMessage(content: string): string {
    return content
      .replace(/</g, '&lt;').replace(/>/g, '&gt;')
      .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
      .replace(/`(.*?)`/g, '<code>$1</code>')
      .replace(/\n/g, '<br>');
  }

  private addSystemMessage(content: string): void {
    this.messages.push({
      id: crypto.randomUUID(),
      role: 'system',
      content,
      timestamp: new Date(),
    });
    this.shouldScroll = true;
  }

  private scrollToBottom(): void {
    try {
      this.chatContainer.nativeElement.scrollTop = this.chatContainer.nativeElement.scrollHeight;
    } catch {}
  }
}
