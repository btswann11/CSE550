### Test: Message Sent Shows Status Indicators

## Overview
This test verifies that users receive accurate visual feedback on the status of their messages after sending. It focuses on validating the system’s ability to display proper status indicators throughout the message lifecycle. The following requirements are covered in this component:

| Requirement ID | Description                                                                                                                                              |
|----------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| FR6            | The sender shall be able to view the status of a message                                                                                                 |
| FR7            | Messages that are successfully submitted to the backend system shall have a visible "sent" tag                                                           |
| FR8            | Messages that are being processed through the system (sending, translation) shall be tagged as "In Progress"                                             |

### Steps:
1. Open a chat session as User A  
2. Type "Hello from User A" into the message input box  
3. Click the **Send** button and verify that the message is labeled **In Progress**  
4. Wait for backend confirmation  
5. Verify that the message status updates to **Sent**
