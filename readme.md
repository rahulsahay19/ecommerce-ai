# 🛍️ AI-Enabled E-Commerce App — Phase 1  
### “From Search Bar to Smart Assistant – Build Your Own Amazon-Style AI Shopping Experience”

---

## 🌟 Overview  

Welcome to **Phase 1** of the *AI-Enabled E-Commerce Project* — a hands-on journey where we blend **.NET Microservices**, **Angular**, **PostgreSQL (pgvector)**, and **AI models** like **Azure OpenAI** & **Ollama** to build an intelligent, conversational shopping experience.  

Here, your application evolves from a plain catalog into a **smart, chat-driven store** that understands human language, performs semantic search, and responds conversationally — just like modern AI shopping assistants.

---

## 🧭 High-Level Flow  

> “The user types naturally, the AI understands semantically, and your backend responds intelligently.”

User → Angular Chat UI → .NET Microservice → Embedding Provider → PostgreSQL + pgvector → Smart Results


You’ll learn to orchestrate the complete AI search lifecycle:
1. Accept user input via chat or search bar  
2. Transform text into embeddings using **Azure OpenAI** or **Ollama**  
3. Retrieve semantically similar products from **pgvector**  
4. Return results and conversational summaries back to Angular  

---

## 🧩 Architecture Diagram  

![Image](https://github.com/user-attachments/assets/d6696459-9948-45f4-a0ff-0ce9f404206e) 

### 🧠 Key Components Explained  

| Layer | Description |
|-------|--------------|
| 🧍‍♂️ **Angular Chat UI** | Chat-style interface where users can search, ask, and explore products naturally. |
| 💬 **Angular Frontend** | Bridges the UI and backend using REST APIs like `/chat` and `/semantic/search`. |
| ⚙️ **.NET Microservices** | Core intelligence engine handling keyword + semantic + hybrid search logic. |
| 🗄️ **PostgreSQL + pgvector** | Vector database storing embeddings and product data for fast similarity lookups. |
| 🧠 **Embedding Provider** | Converts human text into vectors using **Ollama (768-dim)** or **Azure OpenAI (1536-dim)**. |
| 💬 **Chat Service** | Generates AI responses by combining search results with LLM-powered contextual answers. |

---

## 🛠️ Tech Stack  

| Category | Technology |
|-----------|-------------|
| **Frontend** | Angular 20 + Signals, TypeScript, SCSS |
| **Backend** | .NET 9 Microservices (CQRS style) |
| **Database** | PostgreSQL + pgvector extension |
| **AI Models** | Ollama (Local LLM), Azure OpenAI (Cloud GPT-4 family) |
| **Integration** | REST APIs (`/chat`, `/semantic/search`) |
| **Deployment Ready** | Docker Compose + Swagger + Azure App Services |

---

## 💬 Core Features  

### 🔍 1. Keyword Search (Traditional)  
Search by name, brand, or category — backed by PostgreSQL queries.  

### 🧠 2. Semantic Search (AI-Driven)  
Search with natural language like  
> “Show me footballs under ₹2000 for kids.”  
and get contextually accurate results.  

### ⚗️ 3. Hybrid Search (Best of Both)  
Combines keyword + semantic scores for the most relevant output.  

### 💬 4. Conversational Chatbot  
An AI assistant that not only fetches results but *talks back* with meaningful product summaries.  

### 🧩 5. Switchable Embedding Provider  
Choose between **Ollama (Local)** or **Azure OpenAI (Cloud)** dynamically using configuration.  

---

## 🔧 Configuration  

Update your `appsettings.json` for embedding and database connections:

```json
{
  "EmbeddingProvider": "AzureOpenAI", // options: "Ollama", "OpenAI", "AzureOpenAI"

  "Ollama": {
    "BaseUrl": "http://localhost:11434/",
    "EmbeddingModel": "nomic-embed-text",
    "ChatModel": "tinyllama",
    "Dimensions": "768",
    "MaxTokens": "150",
    "Temperature": "0.7"
  },

  "OpenAI": {
    "BaseUrl": "https://api.openai.com/v1/",
    "ApiKey": "key",
    "EmbeddingModel": "text-embedding-3-small",
    "ChatModel": "gpt-4o-mini",
    "Dimensions": "1536",
    "MaxTokens": "500",
    "Temperature": "0.7"
  },

  "AzureOpenAI": {
    "EndPoint": "https://aoi-rahul-learn.openai.azure.com/",
    "ApiKey": "key",
    "EmbeddingDeployment": "text-embedding-3-small", // deployment name in Azure AI Foundry
    "ChatDeployment": "gpt-4o-mini", // deployment name in Azure AI Foundry
    "ApiVersion": "2024-05-01-preview",
    "Dimensions": "1536",
    "MaxTokens": "500",
    "Temperature": "0.7"
  },

  "OcelotGateway": {
    "BaseUrl": "http://localhost:8010/"
  },

  "ConnectionStrings": {
    "PgVector": "Host=localhost;Port=5433;Database=aivector;Username=aiuser;Password=aiPass123"
  }
}
```

📸 Application Flow Showcase

Traditional Keyword Search

![Image](https://github.com/user-attachments/assets/2601237d-b770-4d3c-9b41-86b112cdbb77)

Semantic Query Results

![Image](https://github.com/user-attachments/assets/456140a5-fb48-4e49-8a3e-c23f79af1f27)

Hybrid Query Results

![Image](https://github.com/user-attachments/assets/2bf28c6a-ed56-4746-9f03-0471cb245c88)

![Image](https://github.com/user-attachments/assets/983b9dfe-e879-4bd3-8f1a-1dae6b7bb6ce)

Chatbot Response with Product Cards

![Image](https://github.com/user-attachments/assets/38d9df38-fcb8-4e22-b422-55b9b77e2e64)

![Image](https://github.com/user-attachments/assets/8077aa1c-d578-47aa-bc13-a91f36dd94ad)

![Image](https://github.com/user-attachments/assets/40fda5c8-232f-4220-8ba7-23bc0423715f)

![Image](https://github.com/user-attachments/assets/e5d2e336-4719-40cd-892a-40abdf15dfdf)


🧰 Project Structure

Server Side

![Image](https://github.com/user-attachments/assets/2620ef6e-f7f3-4ce2-8f53-7b961f00550b)

Client Side

![Image](https://github.com/user-attachments/assets/4dff511e-5095-4657-b5f3-2233071a802f)

🚀 What’s Next – Phase 2 Preview
Phase 2: Intelligent Assistant & Web Integration 🔮

In the next phase, we’ll take this foundation further with:

🎙️ Voice Assistant integration (Speech ↔ Text ↔ Speech)

🌐 Web Search fallback (using Bing / Google API)

🧠 Contextual memory and personalized recommendations

💬 Multi-turn conversational flows and smart ranking

Stay tuned — the goal is to make your app behave like a real AI shopping assistant!

MIT License
Copyright (c) 2025 Rahul Sahay

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction...
