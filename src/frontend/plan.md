# Next.js + Vercel AI SDK + Azure AI Foundry Chatbot

**Blueprint for Iterative, Prompt-Driven Development**

A new sentence for Casey to review :)

---

## 1. Project Bootstrap

### 1.1. Initialize Project

```text
You are a full-stack engineer.  
Scaffold a new Next.js App Router project named `ai-chatbot` with TypeScript.  
Add Tailwind CSS with PostCSS and autoprefixer, and generate default config files.
```

---

### 1.2. Install Core Dependencies

```text
Install these npm dependencies in the project root:  
- Vercel AI SDK: `ai`, `@ai-sdk/react`
- UI libraries: `shadcn/ui`, `assistant-ui`
- Tailwind CSS, PostCSS, autoprefixer

Output: a single command block for all install steps.
```

---

## 2. Core LLM API Proxy

### 2.1. Create AI Foundry Proxy Route

```text
Create a Next.js API route at `/app/api/chat/route.ts`.  
Requirements:
- Accepts POST requests with `{ messages: [...] }`.
- Reads `FOUNDRY_AGENT_ENDPOINT` and `FOUNDRY_API_KEY` from env vars.
- Forwards the payload to Azure AI Foundry Agent Service.
- Forwards the response stream (or full response if streaming is unavailable).
- Never expose API keys to the client.

Add basic error handling. Do NOT persist any data.
```

---

### 2.2. Test API Route with Mock Data

```text
Add a temporary route test using a hard-coded test payload to verify connectivity with AI Foundry.  
Console log both the outgoing request and response.  
Comment out the test before moving on.
```

---

## 3. Chat UI Integration

### 3.1. Set Up Vercel AI SDK `useChat` Hook

```text
Set up a new React component `Chat.tsx` in `/app/components`.  
Use the `useChat` hook from Vercel AI SDK.  
Configure it to send messages to `/api/chat`.

Output: a basic chat UI (text input, message list) with streaming responses.
```

---

### 3.2. Build Basic Message List & Input Components

```text
Build `MessageList.tsx` and `MessageInput.tsx` components.
- `MessageList`: Displays all messages, styled for markdown/code output.
- `MessageInput`: Input box with send button, calls `onSend` prop.

Integrate both into `Chat.tsx`.
```

---

## 4. UI Styling & Enhancement

### 4.1. Style with Tailwind & shadcn/ui

```text
Apply Tailwind CSS and shadcn/ui components to all chat UI elements for a modern look.  
Ensure:
- Chat is mobile responsive
- Chat bubbles are visually distinct (user/AI)
- Support for markdown and code snippets
```

---

### 4.2. Add Accessibility Features

```text
Improve accessibility:
- ARIA roles/labels
- Keyboard navigation for input and send button
- Visually hidden labels for screen readers

Keep UI minimal but accessible.
```

---

## 5. Security & Config

### 5.1. Secure Env Vars and Remove Test Code

```text
- Remove any hard-coded test payloads or debug logs.
- Ensure all AI Foundry secrets are only available on the server (never sent to the client).
- Add a `.env.example` to the repo with placeholders for `FOUNDRY_AGENT_ENDPOINT` and `FOUNDRY_API_KEY`.
```

---

## 6. Deployment

### 6.1. Prepare for Vercel Deployment

```text
Update `next.config.js` as needed for deployment.  
Add deployment instructions to `README.md`, including setting env vars in the Vercel dashboard.  
Deploy to Vercel and verify:
- Streaming works
- API secrets are safe
- No chat history persists across refresh
```

---

## 7. QA and Refactor

### 7.1. End-to-End Testing and Code Review

```text
Test the chat UX for:
- Streaming speed and reliability
- Mobile and desktop usability
- Error handling when AI Foundry is down/unavailable
- Security: confirm secrets are server-only

Refactor code for clarity and maintainability.
```

---

## 8. Summary Table: Chunks & Steps

| Phase     | Chunk | Description                                     |
| --------- | ----- | ----------------------------------------------- |
| Bootstrap | 1.1   | Scaffold project, Tailwind setup                |
|           | 1.2   | Install dependencies                            |
| LLM Proxy | 2.1   | Create `/api/chat` proxy route                  |
|           | 2.2   | Test proxy route with mock                      |
| Chat UI   | 3.1   | Set up Vercel `useChat` in `Chat.tsx`           |
|           | 3.2   | Build `MessageList` & `MessageInput` components |
| UI & UX   | 4.1   | Apply Tailwind/shadcn/ui styles                 |
|           | 4.2   | Add accessibility features                      |
| Security  | 5.1   | Secure env, remove debug/test code              |
| Deploy    | 6.1   | Vercel deploy, README update                    |
| QA        | 7.1   | End-to-end tests, error handling, refactor      |

---

## 9. Sample LLM Prompt for an Implementation Step

```text
Write a Next.js API route at `/app/api/chat/route.ts` that:
- Accepts POST with `{ messages }`
- Reads `FOUNDRY_AGENT_ENDPOINT` and `FOUNDRY_API_KEY` from env
- Forwards request to Azure AI Foundry, returns streaming or full response
- Handles errors, never exposes secrets to client
```

---

## How to Use

- Copy each prompt into your codegen LLM (or Copilot) as you reach that step.
- Start with the first chunk and proceed sequentially.
- Test after each step before moving on—no orphaned code!
- Each prompt assumes prior steps are complete.

---

**Ready for prompt-driven development!**

