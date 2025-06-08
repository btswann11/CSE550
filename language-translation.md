# Language Translation Design
---
## Overview:
---
Our messaging web app enables users to register with a username, select their preferred language, and join a session. Messages are automatically translated on the backend and displayed side-by-side in their original and translated form once successfully sent.
## Translation Flow Diagram (Sender -> Receiver)
---

```mermaid
sequenceDiagram
	participant User
	participant UI as Frontend
	participant LangSel as LanguageSelector
	participant Backend
	participant Translator as TranslationService
	participant MQ as MessageQueue
	participant Receiver
	
	%% Registration & session join
	User->>UI: Enter username & pick local language
	UI->>LangSel: Load supported languages
	LangSel-->>UI: Return language list
	UI->>Backend: Register user & join session
	
	%% Auto-translation on send
	User->>UI: Type message in textbox
	User->>UI: Click “Send”
	UI->>Backend: POST (message, srcLang, tgtLang, userId, sessionId)
	Backend->>Translator: Translate(text, srcLang→tgtLang)
	Translator-->>Backend: Translated text
	Backend->>MQ: Enqueue message

	%% Delivery & UI display
    Backend->>UI: Push status update ("Sent" or "Failed")
    alt Status = "Delivered"
        MQ-->>Receiver: Deliver translated text
        UI->>UI: Display original & translated messages
        UI->>UI: Show status -> "Sent"
    else Status = "Failed"
        UI->>UI: Show status -> "Failed"
        UI->>User: Offer "Retry"
    end
```

When a user registers, the frontend records their username and chosen language. Upon sending a message, the UI forwards the raw text and language metadata to the backend, which decouples translation from the UI layer by invoking a third-party translation service and enqueuing both original and translated payloads in a queue.

After translation and queue insertion, the backend emits a status update, prompting the frontend to render the original and translated text together or display a retry option. The receiver then receives the translated message from the queue in order, ensuring a consistent and natural conversation flow in their selected language.
