# Security Tests
## Overview
These test cover edge cases and various other security related tests

| Requirement | Description |
|--|--|
| FR4 | Text shall only be able to be inputted in the text box |
| FR15 | The system shall prevent users from accessing an existing chat session unless they have been authenticated through proper setup.  |
| NFR4 | Message transmission shall occur over secure channels |

### Test 1: Writing System Input Verification
This test covers requirement: FR4
#### Steps:
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
 3. Verify the text entered into the text box is the same as the untranslated message  

### Test 2: Verify User Cannot Access a Private Chat Session without Permission
This test covers requirement: FR15
#### Steps:
1. User X initiates a private chat with user Y
2. User Z initiates a chat with either user X or user Y
3. Verify that user Z does not gain access to the existing private chat session between user X and Y

### Test 3: Messages Only Sent Using Secure Methods
This test covers requirement: NFR4
#### Steps:
1. Start capturing network traffic using a network analysis tool
2. Send a message within the chat application
3. Recieve a message within the chat application
4. Stop the network traffic capture
5. Analyze the captured network traffic. Identify packets related to the message transmission from client to the server. Identify the packets related to the message reception from the server to the client.
6. Inspect the protocol layers of these packets.
7. All transmission and reception packets should clearly indicate the use of HTTPS
