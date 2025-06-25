### Test: Verify Message Translation Between Languages

This test covers requirement: FR11

### Steps:
1. Log in as **User A** with preferred language set to **English**  
2. Log in as **User B** with preferred language set to **Spanish**  
3. User A sends the message: `"Hello, how are you?"`  
4. Confirm that User B receives a translated message in **Spanish**  
5. User B sends a message in Spanish: `"Estoy bien, gracias."`  
6. Confirm that User A receives it translated into **English**  
7. Log in as **User C** with preferred language set to **French**  
8. User B sends a message to User C  
9. Confirm that the message User C receives is in **French** (translated from **Spanish**)  
10. Confirm that all translations are accurate and completed within 5 seconds

### Expected Results:
- Each user receives the message in their preferred language  
- Translations are correct and meaningful  
- Message order is preserved and translations occur automatically

### Acceptance Criteria:
- 95% of translated messages should appear in the recipient's language within 5 seconds  
- Translation errors must be logged or flagged  
- Translations must honor sender and receiver login-configured language preferences

### Notes:
This test assumes translation systems are implemented and message delivery is working as verified in parent test #17. Language preferences are fixed at login and cannot be changed during session.
