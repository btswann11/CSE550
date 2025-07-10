# User Login Tests

## Overview
This test design outlines the testing strategy for the User Agreement and Login Page of the multilingual messaging web application. The focus of this test design is to validate all key requirements (both functional and non-functional) relevant to the login and user agreement experience. The following requirements are covered in this component:

| Requirement ID | Description                                                                                                                                              |
|----------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| FR1            | The application shall allow users to choose a unique username and select their native language before entering the chat system.                          |
| FR14           | The application shall display a warning message advising users not to share private or sensitive information during use.                                 |
| FR15           | The system shall prevent users from accessing an existing chat session unless they have been authenticated through proper setup.                         |
| NFR5           | A pre-login screen shall be shown that includes warnings about data privacy and age limitations before allowing the user to proceed with login or setup. |

## Test cases

### Test 1a: User Declines Pre-login Warning Screen
This test covers requirements: NFR5, FR14

#### Steps:
1. Navigate to the web application's root URL.
2. Verify that a warning screen is immediately displayed before any login or input is available.
3. Confirm the screen contains a message not to share sensitive information.
4. Confirm the screen asks for age confirmation (18+).
5. Select "Decline" on the user agreement or age confirmation.
6. Verify that a message is displayed stating: "You must accept the user agreement and be 18 years or older."
7. Confirm that the user is not allowed to use the app and cannot proceed further.
8. Attempting to interact with the app should not allow access; the only way to proceed is to refresh the webpage and accept the agreement.

### Test 1b: User Accepts Pre-login Warning Screen
This test covers requirements: NFR5, FR14

#### Steps:
1. Navigate to the web application's root URL.
2. Verify that a warning screen is immediately displayed before any login or input is available.
3. Confirm the screen contains a message not to share sensitive information.
4. Confirm the screen asks for age confirmation (18+).
5. Select "Accept" on the user agreement and age confirmation.
6. Verify that the user is allowed to move forward to the username/language setup.

### Test 2a: Username Uniqueness and Validation
This test covers requirement: FR1

#### Steps:
1. Proceed to the user setup page and accept the popup.
2. Enter a username that is already in use (e.g., "testuser1").
3. Open a separate browset and repeat steps 1 and 2
4. Verify that a warning appears stating the username is already in use.
5. Enter a new, unique username (e.g., "newuser123").
6. Select a native language from the dropdown.
7. Submit the form.
8. Verify that the user is successfully taken to the chat interface, confirming the setup is accepted.

### Test 2b: Username Field Validation (Empty and Invalid Input)
This test covers requirement: FR1

#### Steps:
1. Proceed to the user setup page.
2. Leave the username field empty and try to proceed.
3. Verify that an error message appears indicating the field is required.
4. Enter a username with invalid characters (e.g., "!!!" or spaces).
5. Verify that the system rejects the input with a relevant message.
6. Enter a valid username to confirm validation success.

### Test 3: Sensitive Info Warning Is Always Displayed
This test covers requirement: FR14

#### Steps:
1. From a new browser session, access the website.
2. On the pre-login screen, confirm a warning is shown stating not to share personal or sensitive information.
3. Complete the setup and enter the chat interface.
4. Verify that the same warning is also repeated or displayed in a persistent notice, banner, or tooltip in the chat UI.

### Test 4: Prevent Unauthorized Session Access
This test covers requirement: FR15

#### Steps:
1. Open the web app in one browser window, complete setup, and enter a chat session.
2. Copy the session/chat URL.
3. Open an incognito/private window and paste the same URL.
4. Verify that the session does not open directly without completing the setup.
5. Confirm that the user is redirected to the pre-login warning screen or setup page instead.
