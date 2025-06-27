## Text Requirements Overview
This is the list of all current requirements used for the translated text messaging web app

| Requirement ID | Title | Description | Notes |
|--|--|--|--|
| FR1 | User Setup | The web application shall have a user setup, where the user picks a username as a unique identifier and their local language | |
| FR2 | Message Condition on User Presence | Message can only be sent if more than one user is present in a session | |
| FR3 | Session Termination | When a user leaves a chat the session shall terminate for the other user | |
| FR4 | Text Input Restriction | Text shall only be able to be inputted in the text box | |
| FR5 | Send on Button Press | Text shall only be sent when pressing the send button | |
| FR6 | View Message Status | The sender shall be able to view the status of a message | |
| FR7 | Sent Tag Visibility | Messages that are successfully submitted to the backend system shall have a visible "sent" tag | |
| FR8 | In Progress Tag | Messages that are being processed through the system (sending, translation) shall be tagged as "In Progress" | |
| FR9 | Failed Tag | Messages that failed to reach the backend system shall be tagged as "Failed" | |
| FR10 | Retry Failed Message | Users shall have the ability to retry sending a failed message | |
| FR11 | Show Translated Message | The users local screen shall show original message (source language) and the translated version (destination language) | |
| FR12 | Alphanumeric Data Only | The application shall only allow alphanumeric characters and strings to be sent | |
| FR13 | No Message Storage | The web application shall not store any user messages beyond the length of the session lifecycle | |
| FR14 | Warn Sensitive Info Sharing | The application shall warn the user not to share private or sensitive information | |
| FR15 | Prevent Unauthorized Session Access | The system shall prevent users from accessing an existing chat session unless they have been authenticated through proper setup. | |
| NFR1 | Message Delivery Time | Messages shall be delivered to recipients within 5 seconds of sending | |
| NFR2 | Message Size Limit | Message size shall be limited to 256 characters | |
| NFR3 | FIFO Message Ordering | Messages shall be FIFO (first-in-first-out) | |
| NFR4 | Secure Channels | Message transmission shall occur over secure channels | |
| NFR5 | Pre-Login Warning Screen | A pre-login screen shall be shown that includes warnings about data privacy and age limitations before allowing the user to proceed with login or setup. | |
| NFR6 | Backend Translation with Standards | Message translation shall be performed on the backend, decoupling the front-end implementation from core functionality, and the application shall use third-party translation services that comply with current industry standards (e.g., Azure AI Translator, OpenAI). | Planned to be removed due to being unable to be tested by QA |
| NFR7 | Infrastructure using IAC | Infrastructure shall be delcared using Iac (Terraform or Bicep) | Planned to be removed due to being unable to be tested by QA |
| NFR8 | Infrastructure using Azure | Infrastructure shall be hosted in Azure | Planned to be removed due to being unable to be tested by QA |
| NFR9 | CI/CD in Github | CI/CD shall be done using GitHub Actions | Planned to be removed due to being unable to be tested by QA |
