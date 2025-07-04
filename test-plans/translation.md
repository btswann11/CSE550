# Message Translation Tests

## Overview
This test verifies the application's ability to translate messages between users with different preferred languages. It ensures that both the original and translated versions of a message are displayed to the recipient in a clear and accurate manner. The following requirement is covered in this component:

| Requirement ID | Description                                                                                                                                              |
|----------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| FR11           | The users local screen shall show original message (source language) and the translated version (destination language)                                   |

### Test 1: messages are properly translated
This test covers requirement: FR11

#### Steps:
1. Log in as **User A** with preferred language set to **English**  
2. Log in as **User B** with preferred language set to **Spanish**  
3. User A sends the message: `"Hello, how are you?"`  
4. Verify that **User B** receives the translated message in **Spanish**  
5. User B sends a message in Spanish: `"Estoy bien, gracias."`  
6. Verify that **User A** receives the translated message in **English**  
7. Log in as **User C** with preferred language set to **French**  
8. User B sends a message to **User C**  
9. Verify that **User C** receives the translated message in **French** (translated from Spanish)  
10. Verify that all translations are accurate, meaningful, and completed

### Test 2: Same languages are not translated
1. Log in as User A with a chosen language
2. Log in as User B with the same chosen language
3. Start a chat between eachother
4. Send any message
5. Verify that no matter what message is sent, both the sent and received message remains the same
