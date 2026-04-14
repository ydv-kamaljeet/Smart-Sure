# Ollama RAG Chatbot (Flask + HTML)

A simple local RAG chatbot that:
- uses your installed **`llama3.2:1b`** model in Ollama,
- lets you **upload `.txt` or `.md` files**,
- chunks and indexes the file content,
- retrieves the most relevant chunks for a question,
- sends that context to Ollama to answer.

## Features
- Python Flask backend
- HTML/CSS/JS chat UI
- Upload text files to the system
- Local document indexing stored in `data/index.json`
- Local RAG flow using simple vector-like cosine scoring over token counts
- Uses Ollama model: `llama3.2:1b`

## Project structure

```text
ollama_rag_app/
│-- app.py
│-- requirements.txt
│-- README.md
│-- uploads/
│-- data/
│-- templates/
│   └── index.html
└── static/
    ├── styles.css
    └── script.js
```

## Requirements
- Python 3.10+
- Ollama installed and running
- Model available locally:

```bash
ollama list
```

You should see:

```text
llama3.2:1b
```

## Start Ollama
In a separate terminal, make sure Ollama is running and the model is available:

```bash
ollama run llama3.2:1b
```

## Run the app
From the project folder:

### Windows
```bash
py -m venv .venv
.venv\Scripts\activate
python -m pip install --upgrade pip
pip install -r requirements.txt
python app.py
```

### macOS / Linux
```bash
python3 -m venv .venv
source .venv/bin/activate
python -m pip install --upgrade pip
pip install -r requirements.txt
python app.py
```

Open in browser:

```text
http://127.0.0.1:5000
```

## How it works
1. Upload a `.txt` or `.md` file.
2. The app stores the file in `uploads/`.
3. The file is chunked into smaller passages.
4. Each chunk is tokenized and indexed.
5. When you ask a question, the app retrieves the top matching chunks.
6. Those chunks are passed to `llama3.2:1b` through the Ollama API.
7. The answer is shown in the chat UI.

## Notes
- This version is intentionally lightweight and easy to run.
- Retrieval is local and does **not** require a separate vector database.
- If you want, this can be upgraded later to use:
  - embedding models,
  - FAISS / ChromaDB,
  - PDF support,
  - streaming responses,
  - multi-file filtering,
  - source highlighting.

## API endpoints
- `GET /` → UI
- `POST /upload` → upload and index file
- `POST /ask` → ask question using RAG
- `GET /documents` → list indexed docs
- `GET /health` → health check
