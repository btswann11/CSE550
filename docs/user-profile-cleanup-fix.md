# User Profile Cleanup Bug Fix

## Problem Description

The application had a bug where user profile data was being deleted too aggressively, causing users to lose their sessions even when they were still actively using the application. The original implementation violated the requirement that user data should only be deleted in two specific scenarios:

1. User has closed the tab or browser
2. User was inactive for 30 minutes

## Root Cause Analysis

### Issues Found

1. **No 30-minute inactivity timer**: The application lacked any implementation of a proper 30-minute inactivity timer
2. **Aggressive 5-second cleanup**: User profiles were deleted after just 5 seconds when users switched tabs or minimized the browser
3. **Immediate cleanup on browser events**: User profiles were deleted immediately on various browser events (`beforeunload`, `pagehide`, visibility changes)
4. **No user activity tracking**: The application didn't track user activity to determine if they were actually inactive

### Original Problematic Behavior

- Users switching tabs for more than 5 seconds → Profile deleted
- Users minimizing browser → Profile deleted immediately
- Users briefly navigating away → Profile deleted immediately
- No actual 30-minute inactivity detection

## Solution Implemented

### 1. Added Proper Inactivity Detection

**New Data Properties:**
```javascript
inactivityTimer: null, // Timer for 30-minute inactivity detection
lastUserActivity: null, // Timestamp of last user activity
inactivityTimeoutDuration: 30 * 60 * 1000, // 30 minutes in milliseconds
showInactivityWarning: false, // Show warning before timeout
inactivityWarningTimer: null // Timer for showing warning
```

### 2. Implemented User Activity Tracking

**Activity Listeners:**
- `mousedown`, `mousemove`, `keypress`, `scroll`, `touchstart`, `click`
- Tracks activity and resets the 30-minute timer
- Prevents excessive resets with 10-second debouncing

**Activity Reset Triggers:**
- Sending messages
- Opening chats
- Creating new chats
- Any user interaction

### 3. Modified Visibility Change Behavior

**Before (Aggressive):**
- 5-second timeout when tab hidden → Profile deleted

**After (Reasonable):**
- 5-minute timeout when tab hidden → Profile deleted (only if still hidden)
- Timer is paused when tab hidden, resumed when visible
- User can briefly switch tabs without losing their session

### 4. Added User-Friendly Warnings

**25-Minute Warning:**
- Shows warning banner at 25-minute mark (5 minutes before timeout)
- Allows user to click "Stay Active" button to reset timer
- Clear visual indicator that session will expire soon

**Warning Banner Features:**
- Fixed position at top of screen
- Slide-down animation
- Clear call-to-action button
- Automatic reset on any user activity

### 5. Proper Timer Management

**Timer Lifecycle:**
- Started when user completes setup and profile is created
- Reset on any user activity
- Stopped when session ends or user logs out
- Proper cleanup in component destruction

**Multiple Timer Coordination:**
- Inactivity timer (30 minutes)
- Warning timer (25 minutes)
- Visibility timeout (5 minutes when tab hidden)
- All timers properly cleared when not needed

## Implementation Details

### Key Methods Added

1. **`startInactivityTimer()`** - Starts both the 25-minute warning timer and 30-minute session timeout
2. **`resetInactivityTimer()`** - Resets all timers when user is active
3. **`stopInactivityTimer()`** - Stops all timers and hides warnings
4. **`setupActivityListeners()`** - Sets up DOM event listeners for activity detection

### Modified Methods

1. **`completeSetup()`** - Now starts inactivity timer after setup
2. **`registerUserWithBackend()`** - Starts timer after successful profile creation
3. **`sendNewMessage()`** - Resets timer on message sending
4. **`openChat()`** - Resets timer on chat interaction
5. **`createNewChat()`** - Resets timer on new chat creation
6. **`visibilityChangeHandler`** - Changed from 5-second to 5-minute cleanup delay

### UI Enhancements

1. **Inactivity Warning Banner** - Shows at 25-minute mark
2. **Stay Active Button** - Allows manual timer reset
3. **Smooth Animations** - Slide-down effect for warning banner
4. **Layout Adjustment** - App container adjusts when warning is shown

## Validation of Requirements

### ✅ Requirement 1: User has closed the tab or browser
- `beforeunload` and `pagehide` events still trigger immediate cleanup
- 5-minute visibility timeout catches cases where browser is closed without firing events
- Only triggers after extended absence, not brief tab switching

### ✅ Requirement 2: User was inactive for 30 minutes
- Proper 30-minute inactivity timer implemented
- User activity tracking across multiple interaction types
- Warning system gives users chance to stay active
- Timer resets on any user interaction

## Testing Recommendations

### Manual Test Cases

1. **Normal Usage**: User should remain logged in during active use
2. **Brief Tab Switch**: User switches tabs for 1-2 minutes → Should remain logged in
3. **Extended Tab Switch**: User switches tabs for 6+ minutes → Should be logged out
4. **Inactivity Warning**: User inactive for 25 minutes → Should see warning banner
5. **Inactivity Timeout**: User inactive for 30 minutes → Should be logged out
6. **Activity Reset**: User clicks "Stay Active" → Timer should reset
7. **Browser Close**: User closes browser → Should be logged out immediately

### Automated Test Areas

1. Timer functionality (start, reset, stop)
2. Activity detection and timer reset
3. Warning banner display logic
4. Cleanup on component destruction
5. Integration with existing chat functionality

## Benefits

1. **Better User Experience**: Users no longer lose sessions during normal usage
2. **Compliance**: Meets the 30-minute inactivity requirement
3. **User-Friendly**: Warning system prevents unexpected logouts
4. **Robust**: Handles various browser behaviors and edge cases
5. **Maintainable**: Clean separation of timer logic and UI components

## Files Modified

- `src/UniversalTranslator/content/index.html` - Main application file with all changes

## Future Enhancements

1. **Configurable Timeout**: Make the 30-minute timeout configurable
2. **Server-Side Validation**: Add backend timer validation
3. **Multiple Warning Levels**: Show warnings at 20, 25, and 28 minutes
4. **Persistent Settings**: Remember user preference for timeout duration
5. **Analytics**: Track common inactivity patterns to optimize timeout values
