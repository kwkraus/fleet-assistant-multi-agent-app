# Developer Specification: Next.js AI Conversational Chatbot (AI Foundry Edition)

## Overview

Build a production-ready AI chatbot with **Next.js** (App Router), **Vercel AI SDK** for chat state/streaming, and UI built with **shadcn/ui**/**assistant-ui** and **Tailwind CSS**.
**LLM requests MUST be routed to Azure AI Foundry Agent Service.**
**No database persistence** of chat history; chat messages are session-only (memory resets on reload).

---

## 1. **Technology Stack**

* **Frontend/Full-stack:** Next.js (App Router)
* **AI Integration:**

  * Vercel AI SDK (for state management/streaming UX)
  * **Azure AI Foundry Agent Service** as the LLM provider (calls via server API route)
* **UI:** shadcn/ui, assistant-ui
* **Styling:** Tailwind CSS
* **Deployment:** Vercel
* **(No DB integration required at this time)**

---

## 2. **Key Requirements**

### Functional

* Real-time AI chat UI with streaming responses (use `useChat` from Vercel AI SDK)
* All LLM requests go through **Azure AI Foundry Agent Service** API

  * (Back-end model selection is managed by Foundry; UI/frontend does not hard-code model choice)
* No persistence of chat history (refreshing browser wipes messages)
* Customizable, accessible chat interface (markdown, code rendering, mobile friendly)
* Secure handling of AI Foundry API keys (server-side only; never exposed to client)

### Non-Functional

* Fully responsive, accessible UI
* Vercel-deployable with no post-deployment config
* Easy future extension for DB or additional providers if desired

---

## 3. **Project Structure (Relevant Parts)**

*(Omitting DB files—no persistence)*

```
/app
  /api
    /chat
      route.ts    # API route: proxies messages to AI Foundry Agent Service
  /components
    Chat.tsx      # Main chat UI
    MessageList.tsx
    MessageInput.tsx
  /lib
    ai.ts         # AI Foundry API helpers (optional, for code org)
  /styles
    globals.css
  page.tsx        # Chatbot main page
...
```

---

## 4. **Development Workflow**

### a. **Initialize Project**

```bash
npx create-next-app@latest --experimental-app
cd <project-name>
npm install ai @ai-sdk/react tailwindcss postcss autoprefixer
npx tailwindcss init -p
# Add shadcn/ui and/or assistant-ui as needed
```

### b. **LLM API Route: Proxy to AI Foundry**

**/app/api/chat/route.ts**

* Accept POST requests with the chat message(s)
* Forward payload to **AI Foundry Agent Service** via its REST API
* Stream response back to client using Vercel AI SDK helpers (if needed)
* **API key or credentials must be handled server-side via env vars**

**Example (Pseudocode):**

```ts
// app/api/chat/route.ts

export const runtime = 'edge';

export async function POST(req: Request) {
  const { messages } = await req.json();

  // Construct request for AI Foundry Agent Service
  const foundryResponse = await fetch(process.env.FOUNDRY_AGENT_ENDPOINT, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${process.env.FOUNDRY_API_KEY}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ messages })
  });

  // Optionally: stream the response (if Foundry supports streaming), or just forward it
  // If streaming, wrap with Vercel's StreamingTextResponse or similar utility
  return new Response(foundryResponse.body, {
    headers: { 'Content-Type': 'text/event-stream' }
  });
}
```

> Adjust as per actual AI Foundry API signature/streaming details.

### c. **Frontend (Chat UI)**

* Use `useChat` from Vercel AI SDK, pointed at `/api/chat`
* UI and UX: build with shadcn/ui/assistant-ui + Tailwind CSS
* **No DB or session management** beyond default browser state
* All message history is ephemeral (browser session only)

### d. **Styling & UI**

* Configure Tailwind and shadcn/ui per their docs

---

## 5. **Security Considerations**

* AI Foundry API keys **never** leave the server
* All user input is relayed to server-side route for AI Foundry communication
* **No data persistence**—session is in-browser memory only

---

## 6. **Deployment**

* Set AI Foundry endpoint and API key as env variables in Vercel dashboard (`FOUNDRY_AGENT_ENDPOINT`, `FOUNDRY_API_KEY`)
* Push to GitHub, connect to Vercel for continuous deployment

---

## 7. **References**

* [Vercel AI SDK Docs](https://sdk.vercel.ai/docs)
* [Azure AI Foundry Agent Service Docs](https://learn.microsoft.com/en-us/azure/ai-services/agent/) (or your org’s endpoint)
* [shadcn/ui](https://ui.shadcn.com/), [assistant-ui](https://assistant-ui.vercel.app/)

---

## 8. **Quickstart Checklist**

* [ ] Scaffold Next.js app
* [ ] Install Vercel AI SDK, Tailwind, shadcn/ui, assistant-ui
* [ ] Implement `/api/chat/route.ts` as **proxy to Foundry Agent Service**
* [ ] Set up frontend with `useChat` targeting `/api/chat`
* [ ] Style and QA UI/UX
* [ ] Configure env vars in Vercel for secure deployment

---

## 9. **Limitations**

* **No persistent chat history**—session only; refresh = loss of messages
* **LLM model choice/management is owned by AI Foundry**, not this UI

---

**Need code samples, actual API signatures for Foundry, or want to discuss streaming implementation?**
Just say the word!
