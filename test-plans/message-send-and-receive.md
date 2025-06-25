### Test: Message Sent Shows Status Indicators

This test covers requirements: FR6, FR7, FR8

### Steps:
1. Open a chat session as User A  
2. Type "Hello from User A" into the message input box  
3. Click the **Send** button  
4. Confirm that the message is initially labeled **In Progress**  
5. Wait for backend confirmation  
6. Confirm that the message status changes to **Sent**

### Expected Results:
- The message enters the system and displays the **In Progress** label after pressing Send  
- Once acknowledged by the backend, the label changes to **Sent**  
- The user sees status updates reflected in the chat interface

### Acceptance Criteria:
- 90% or more of messages must progress from **In Progress** to **Sent** within 5 seconds  
- No messages should skip status labels or go out of order  
- Less than 10% failure rate is acceptable due to backend instability or edge cases

### Notes:
This test assumes message input and backend status handling are already implemented. This test does not cover message reception or translation.
