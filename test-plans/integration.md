# Integration Tests
## Overview
Once all unit tests pass, we need to put everything together and ensure that all pieces work together properly

### Test 1: Start a chat
#### Steps:
1. Join site
2. Verify popup appears
3. Select Accept
4. Choose any Username and Language
5. Start a chat with any user
6. Send any number of messages
8. Back out from the chat and start a new chat with a different user
9. Send any number of messages
10. Back out from the chat and verify that 2 chats are available to choose from 

##### Expected Ouput:
Login Popup appears forcing user to accept terms.
Any unique username and language can be selected.
User can start a chat with another user.
Message can be entered in text box only and only sent when send pressed.
Message statuses appear when sending messages.
Leaving chat terminates the session.
A new chat can be entered with a second user.
Two chat sessions can be re-entered with no previously stored messages.

###### Requirements Validated:
FR1, FR2, FR3, FR4, FR5, FR13, NFR5
