## Text Requirements Overview
(idk if an overview is necessary)

### Functional Requirements
1. Text shall only be able to be inputted in the text box.
2. Text shall only be sent when pressing the send button.
3. All visible text shall be in the language set by the user viewing it.
3. The sender shall be able to view the status of the message:
    1. The sent message shall display "Sent" beneath it when the message successfully arrives to the recipient.
    2. The sent message shall display "In Progress" beneath it when the message is in the process of sending to the recipient.
    3. The sent message shall display "Failed" beneath it when the message fails to send to the recipient. 
        1. A failure is defined in `Nonfunctional Requirements 1.1`
4. Sent and received messages shall be saved, up to a maximum of X (TBD) messages
    1. Messages that are sent beyond the maximum shall cause the oldest message to be deleted.

### Nonfunctional Requirements
1. The time it takes for a text to be received after sending shall take a maximum of 5 seconds before being considered a failure.
    1. Failed messages shall be able to be re-sent as a new message without the sender retyping it.
2. Messages shall have a maximum size of 255 characters.
3. Messages shall be received in the order that they are sent.
    1. This excludes sent messages that are considered a failure