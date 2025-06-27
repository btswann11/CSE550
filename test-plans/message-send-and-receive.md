### Test: Message Sent Shows Status Indicators

## Overview
This test verifies that users receive accurate visual feedback on the status of their messages after sending. It focuses on validating the system’s ability to display proper status indicators throughout the message lifecycle. The following requirements are covered in this component:

| Requirement ID | Description                                                                                                                                              |
|----------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| FR2            | Message can only be sent if more than one user is present in a session                                                                                  |
| FR3           | When a user leaves a chat the session shall terminate for the other user                                                                                |
| FR4 | Text shall only be able to be inputted in the text box |
| FR5 | Text shall only be sent when pressing the send button|
| FR6            | The sender shall be able to view the status of a message                                                                                                 |
| FR7            | Messages that are successfully submitted to the backend system shall have a visible "sent" tag                                                           |
| FR8            | Messages that are being processed through the system (sending, translation) shall be tagged as "In Progress"                                             |
| FR9 | Messages that failed to reach the backend system shall be tagged as "Failed" |
| FR10 | Users shall have the ability to retry sending a failed message| 
| FR12 | The application shall only allow alphanumeric characters and strings to be sent |
| FR13​ |   The web application shall not store any user messages beyond the length of the session lifecycle |
| NFR1           | Messages shall be delivered to recipients within 5 seconds of sending                                                                                   |
| NFR2| Message size shall be limited to 256 characters |
| NFR3           | Messages shall be FIFO (first-in-first-out)                                                                                                             |

## Test Cases

### Test 1: Proper text input location
This test covers FR4

#### Steps

1. Open a chat session as User A
2. Start a chat with User B
3. Type any message into the message input box  
4. Ensure that text is not possible to be entered in any other part of the application

### Test 2: Successful Message Delivery State
This test covers FR2, FR5, FR6, FR7, FR8

#### Steps

1. Open a chat session as User A
2. Start new chat with 
3. Type "Hello from User A" into the message input box  
4. Click the **Send** button and verify that the message is labeled **In Progress**  
5. Wait for backend confirmation  
6. Verify that the message status updates to **Delivered**

### Test 3: Two-User Session Required for Messaging
This test covers requirement: FR2, FR4, FR5, FR9

#### Steps:
1. Complete user setup as User A and join a chat session
2. Have User B join the same session
3. Have User A send a message "Hello there"
4. Verify the message is sucessfully delivered
5. Have User B leave the session
6. Have User A send another message
7. Verify the message fails to deliver

### Test 3.1: Retry Failed Messaging
This test covers requirement: FR10

#### Steps:
1. Repeat steps from Test 3
2. Verify the failed message can be resent

### Test 3.2: Session Termination
This test covers requirement: FR3

#### Steps:
1. Repeat steps from Test 3
2. Verify the chat closes after a short delay (about 5 seconds)

### Test 3.3: Session messages are erased
This test covers requirement: FR13

#### Steps:
1. Repeat steps from Test 3.2
2. Start a chat with User B
3. Verify previous chats are not saved


### Test 4: Only allow alphanumeric data
 This test covers requirement: FR12
#### Steps:
1. Copy a GIF, picture, or word document and paste it into the text box
2. Click Send
3. Verify the text box does not recognize any of the previously mentioned data

### Test 5: Messages arrive in the correct order
This test covers requirements: NFR1, NFR3

#### Steps:
1. Set up User A and User B in an active session
2. Use script to have User A send 10 messages rapidly: "Message 1", "Message 2", "Message 3", etc
3. Verify messages display to both users in ascending numerical order
4. Verify messages display to User B within 5 seconds of being sent by User A

### Test 6: Message size limit
This test covers requirements: NFR2

#### Steps
1. Set up a chat between User A and User B
2. Attempt to enter a message that is greater than 256 characters
3. verify that characters past 256 are not entered into the text box
   
