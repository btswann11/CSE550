# Multiple Person Messaging Tests

## Overview
This test design outlines the testing strategy for the Multiple Person Messaging functionality of the multilingual messaging web application. The following requirements are covered in this component:

| Requirement ID | Description                                                                                                                                              |
|----------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| FR3            | The users local screen shall show original message (source language) and the translated version (destination language)                                  |
| FR10           | Message can only be sent if more than one user is present in a session                                                                                  |
| FR15           | When a user leaves a chat the session shall terminate for the other user                                                                                |
| NFR1           | Messages shall be delivered to recipients within 5 seconds of sending                                                                                   |
| NFR3           | Messages shall be FIFO (first-in-first-out)                                                                                                             |

## Test Cases

### Test: Two-User Session Required for Messaging

This test covers requirement: FR10

### Steps:
1. Complete user setup as User A and join a chat session
2. Attempt to send a message while alone in the session
3. Have User B join the same session
4. Have User A send a message "Hello there"

### Expected Results:
- User A cannot send messages when alone in the session
- Message sending becomes available once User B joins
- Messages send successfully when two users are present

### Acceptance Criteria:
- 100% of single-user sessions must prevent message sending
- Message sending must be enabled immediately when a second user joins

### Notes:
This test verifies messaging is only possible with multiple users present.

---

### Test: Message Translation Display for Both Users

This test covers requirement: FR3

### Steps:
1. Set up User A with English and User B with Spanish in the same session
2. Have User A send "How are you today?"
3. Verify both users see the original English and Spanish translation
4. Have User B respond with "Estoy muy bien, gracias"
5. Verify both users see the original Spanish and English translation

### Expected Results:
- Both users see messages in original and translated versions
- Original and translated messages are clearly distinguishable
- Translation works in both directions

### Acceptance Criteria:
- 100% of messages must display both original and translated versions
- Translation accuracy must be maintained for supported language pairs

### Notes:
This test assumes functional translation service and different user language preferences.

---

### Test: Session Termination and Message Performance

This test covers requirements: FR15, NFR1, NFR3

### Steps:
1. Set up User A and User B in an active session
2. Have User A send 3 messages rapidly: "Message 1", "Message 2", "Message 3"
3. Record delivery times and verify FIFO ordering at User B
4. User A closes browser or disconnects
5. Verify User B receives session termination notification
6. Attempt to have User B send a message after termination

### Expected Results:
- Messages delivered within 5 seconds in correct order
- Session terminates immediately when User A leaves
- User B cannot send messages after termination

### Acceptance Criteria:
- 95% of messages delivered within 5 seconds
- 100% of messages maintain FIFO ordering
- 100% of disconnections trigger session termination within 10 seconds

### Notes:
This test combines performance and session management validation.