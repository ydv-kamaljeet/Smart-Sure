async function refreshDocuments() {
  const res = await fetch('/documents');
  const docs = await res.json();
  const docList = document.getElementById('docList');

  if (!docs.length) {
    docList.innerHTML = '<p class="muted">No documents uploaded yet.</p>';
    return;
  }

  docList.innerHTML = docs.map(doc => `
    <div class="doc-item">
      <div>${escapeHtml(doc.filename)}</div>
      <small>${doc.chunk_count} chunks</small>
    </div>
  `).join('');
}

function escapeHtml(text) {
  const div = document.createElement('div');
  div.innerText = text;
  return div.innerHTML;
}

function addMessage(role, text, sources = []) {
  const chatBox = document.getElementById('chatBox');
  const wrapper = document.createElement('div');
  wrapper.className = `message ${role}`;

  const bubble = document.createElement('div');
  bubble.className = 'bubble';
  bubble.textContent = text;

  if (role === 'bot' && sources.length) {
    const sourcesDiv = document.createElement('div');
    sourcesDiv.className = 'sources';
    const title = document.createElement('div');
    title.innerHTML = '<strong>Retrieved context</strong>';
    sourcesDiv.appendChild(title);

    sources.forEach(src => {
      const item = document.createElement('div');
      item.textContent = `${src.filename} | chunk ${src.chunk_no} | score ${src.score}`;
      sourcesDiv.appendChild(item);
    });

    bubble.appendChild(sourcesDiv);
  }

  wrapper.appendChild(bubble);
  chatBox.appendChild(wrapper);
  chatBox.scrollTop = chatBox.scrollHeight;
}

document.getElementById('uploadForm').addEventListener('submit', async (e) => {
  e.preventDefault();
  const fileInput = document.getElementById('fileInput');
  const uploadStatus = document.getElementById('uploadStatus');

  if (!fileInput.files.length) {
    uploadStatus.textContent = 'Please choose a file.';
    return;
  }

  const formData = new FormData();
  formData.append('file', fileInput.files[0]);
  uploadStatus.textContent = 'Uploading and indexing...';

  try {
    const res = await fetch('/upload', { method: 'POST', body: formData });
    const data = await res.json();
    if (!res.ok) throw new Error(data.error || 'Upload failed');
    uploadStatus.textContent = `${data.message} (${data.chunks} chunks)`;
    fileInput.value = '';
    await refreshDocuments();
  } catch (err) {
    uploadStatus.textContent = err.message;
  }
});

document.getElementById('chatForm').addEventListener('submit', async (e) => {
  e.preventDefault();
  const input = document.getElementById('questionInput');
  const question = input.value.trim();
  if (!question) return;

  addMessage('user', question);
  input.value = '';
  addMessage('bot', 'Thinking...');

  try {
    const res = await fetch('/ask', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ question })
    });
    const data = await res.json();

    const chatBox = document.getElementById('chatBox');
    chatBox.removeChild(chatBox.lastElementChild);

    if (!res.ok) throw new Error(data.error || 'Request failed');
    addMessage('bot', data.answer, data.sources || []);
  } catch (err) {
    const chatBox = document.getElementById('chatBox');
    chatBox.removeChild(chatBox.lastElementChild);
    addMessage('bot', `Error: ${err.message}`);
  }
});
