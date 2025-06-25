### Test _: Writing System Input Verification
This test covers requirement: FR1
### Steps:
1. Enter a message from one of the following writing systems:
- Latin Script
- Chinese Characters
- Arabic Script
- Devangari
- Bengali-Assamese Script
- Cyrillic Script
- Japanese Scripts (Hiragana, Katakana, Kanji)
 - Hangul
 - Telugu Script
 - Tamil Script
 2. Click Send
 ### Expected Results:
 The text entered into the text box is the same as the untranslated message.

 ### Test _: User cannot send files, pictures or gifs
 This test covers requirement: FR1
### Steps:
1. Copy a GIF, picture, or word document and paste it into the text box
2. Click Send
### Expected Results:
The text box should not recognize any of the previously mentioned data

### Out of Date / Back Matter
I've been working with outdated requirements so I'll just put the ones not pertaining to the reqs here

### Test _: Session Ends When a User Disconnects
This test covers requirement: 
### Steps:
1. Have user X and user Y initiate a chat session 
2. Have user X disconnect from the chat session
3. Session should end for the user Y
### Expected Results:
The chat window will display to user Y that user X has left the chat session

### Test _: Verify that Messages from a Terminated Session are Erased
This test covers requirement: 
### Steps:
1. Initiate a chat between user X and user Y
2. Have user X in the chat session disconnect, ending the session
3. Have user Y navigate to a separate active session with user Z
4. Have user Y attempt to reaccess the terminated session with user X
### Expected Results:
The messages in the terminated session should be deleted

### Test _: Verify User Cannot Access a Private Chat Session without Permission
This test covers requirement: 
### Steps:
1. User X initiates a private chat with user Y
2. User Z initiates a chat with either user X or user Y
3. Verify that user Z does not gain access to the existing private chat session between user X and Y
### Expected Results:
User Z should not see messages or any details of the session between user X and user Y.

### Test _: Verify that User Warning Pops Up before Login Screen
This test covers requirements: 
### Steps:
1. User navigates to the website
2. Pop up should appear warning the user not to share sensitive or private information
3. User clicks 'Accept'
4. Website navigates to Login Screen

### Expected Results: 
Pop up warning should appear before the user sees the login screen

### Test_: Messages Only Sent Using Secure Methods
This test covers requirement: 
### Steps:
1. Start capturing network traffic using a network analysis tool
2. Send a message within the chat application
3. Recieve a message within the chat application
4. Stop the network traffic capture
5. Analyze the captured network traffic. Identify packets related to the message transmission from client to the server. Identify the packets related to the message reception from the server to the client.
6. Inspect the protocol layers of these packets.
### Expected Results:
All identified transmission and reception packets should should clearly indicate the use of HTTPS. The data payload of the messages within these packets should appear encrypted to the observer without decryption keys. No message transmission should occur over unecrypted protocols.