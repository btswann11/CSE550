# Text Messaging Design

## Overview

Our messaging web app enables users to register with a username, select their preferred language, and join a session to message another user in real time. Messages are automatically translated on the backend and displayed side-by-side in their original and translated form once successfully sent.

## Message Flow Diagram (Sender ➝ Receiver)

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
	Backend->>Backend: Append timestamp to message
	Backend->>Translator: Translate(text, srcLang→tgtLang)
	Translator-->>Backend: Translated text
	Backend->>MQ: Enqueue message (ordered by timestamp)
	
	%% Delivery & UI display
	Backend->>UI: Push status update ("Sent" or "Failed")
	alt Status = "Delivered"
		MQ-->>Receiver: Deliver translated text (in timestamp order)
		UI->>UI: Display original & translated messages
		UI->>UI: Show status -> "Sent"
	else Status = "Failed"
		UI->>UI: Show status -> "Failed"
		UI->>User: Offer "Retry"
	end
```
When a user registers, the frontend records their username and selected language. When sending a message, the frontend captures the raw text, language metadata, and a precise timestamp. This timestamp is attached as part of the message metadata and is critical for preserving the correct message order, even when multiple messages are sent rapidly or processed at different speeds.

The frontend forwards this data to the backend, which decouples translation from the UI layer by invoking a third-party translation service. Once the translation is received, the backend enqueues both the original and translated message payloads into a message queue. This queue maintains ordering based on timestamps, ensuring that messages are delivered and displayed in the intended sequence.

After queue insertion, the backend emits a status update to the sender’s frontend. This update prompts the UI to render the original message alongside its translated counterpart or, in case of a failure, provide a retry option. On the receiver’s side, messages are pulled from the queue in timestamp order and displayed in their selected language, maintaining a smooth and coherent conversation experience.