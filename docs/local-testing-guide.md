# Universal Translator Local Testing Guide

## Overview

This document provides step-by-step instructions for team members to run and test the Universal Translator application locally. The application is an Azure Functions-based real-time chat platform with automatic translation capabilities.

## Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 8.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Azure Functions Core Tools v4** - [Installation guide](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- **Visual Studio Code** (recommended) with Azure Functions extension
- **Git** for cloning the repository

### Verify Prerequisites

Run these commands to verify your setup:

```bash
# Check .NET version
dotnet --version

# Check Azure Functions Core Tools
func --version

# Should show version 4.x.x
```

## Project Setup

### 1. Clone and Navigate to Project

```bash
git clone <repository-url>
cd CSE550/src/UniversalTranslator
```

### 2. Restore NuGet Packages

```bash
dotnet restore
```

### 3. Create local.settings.json

**CRITICAL:** Create a `local.settings.json` file in the `/src/UniversalTranslator/` directory. This file contains sensitive configuration data that will be shared separately via secure channels.

Create the `local.settings.json` file with the following content

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "AzureSignalRConnectionString": "Endpoint=https://cse550-universal-translator-signalr.service.signalr.net;AccessKey=[TO_BE_PROVIDED];Version=1.0;",

    "UT_TRANSLATION_SERVICE_BASE_URI": "https://api.cognitive.microsofttranslator.com/",
    "UT_TRANSLATION_SERVICE_API_KEY": "[TO_BE_PROVIDED]",
    "UT_TRANSLATION_SERVICE_LOCATION": "eastus",

    "UT_CHATS_STORAGE_CONNECTION_STRING": "[TO_BE_PROVIDED]",
    "UT_CHATS_TABLE_NAME": "utchats-<your-name>",

    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",        
  }
}
```

**⚠️ IMPORTANT NOTES:**

- Replace `<your-name>` with your actual name or identifier to avoid conflicts during local testing. Each person will have their own backend table for storing chat data.
- Replace `[TO_BE_PROVIDED]` values with the actual connection strings and keys I'll provide separately
- **DO NOT commit this file to Git** - it contains sensitive information
- The file is already in `.gitignore` to prevent accidental commits

## Running the Application

### Option 1: Using Visual Studio Enterprise (Recommended)

1. **Open the solution file:**
   - Launch Visual Studio Enterprise
   - Go to `File > Open > Project/Solution`
   - Navigate to and open `/path/to/CSE550/src/UniversalTranslator.sln`

2. **Configure startup settings:**
   - Right-click on the `UniversalTranslator` project in Solution Explorer
   - Select `Set as Startup Project`
   - The `launchSettings.json` will automatically configure the port to 7151

3. **Start debugging:**
   - Press `F5` or click the green "Start" button
   - Visual Studio will automatically:
     - Build the project
     - Start the Azure Functions runtime
     - Launch with the correct port configuration
     - Provide rich debugging capabilities

4. **Verify startup:**
   - Check the Output window (View > Output, select "Azure Functions" from dropdown)
   - Look for the function endpoints listed with `http://localhost:7151/api/...`
   - The Debug toolbar should show the application is running

**Benefits of using Visual Studio Enterprise:**

- **Integrated debugging** with breakpoints and step-through capabilities
- **IntelliSense** for Azure Functions development
- **Built-in Azure tools** and extensions
- **Seamless project management** with Solution Explorer
- **Hot reload** capabilities for faster development cycles

### Option 2: Using Azure Functions Core Tools 

1. **Open terminal in the project directory:**

   ```bash
   cd /path/to/CSE550/src/UniversalTranslator
   ```

2. **Start the function app:**

   ```bash
   func start
   ```

3. **Expected example output:**

   ```
   Azure Functions Core Tools
   Core Tools Version:       4.x.x
   Function Runtime Version: 4.x.x
   
   Functions:
   
           AddChatMember: [POST] http://localhost:7151/api/AddChatMember
   
           GetChatMembers: [GET] http://localhost:7151/api/getchatmembers/{groupName}
   
           IsUserNameAvailable: [GET] http://localhost:7151/api/isusernameavailable/{username}
   
           RemoveChatMember: [DELETE] http://localhost:7151/api/removechatmember/{groupName}/{userId}
   
           SendMessageToUser: [POST] http://localhost:7151/api/SendMessageToUser
   
   For detailed output, run func with --verbose flag.
   [2025-07-11T10:00:00.000Z] Host lock lease acquired by instance ID '...'
   [2025-07-11T10:00:00.000Z] Host started
   [2025-07-11T10:00:00.000Z] Job host started
   ```

### Option 3: Using Visual Studio Code

1. **Open the project in VS Code:**
   
   ```bash
   code /path/to/CSE550/src/UniversalTranslator
   ```

2. **Install recommended extensions** (if prompted):
   - Azure Functions
   - C# for Visual Studio Code

3. **Start debugging:**
   - Press `F5` or go to `Run > Start Debugging`
   - VS Code will automatically use the `launchSettings.json` configuration

### Option 4: Using .NET CLI

```bash
cd /path/to/CSE550/src/UniversalTranslator
dotnet run
```

## Port Configuration

**⚠️ CRITICAL PORT INFORMATION:**

- **Default Port:** The application is configured to run on port `7151`
- **DO NOT change this port** without coordination
- The port is configured in:
  - `launchSettings.json` (line 5): `"commandLineArgs": "--port 7151"`
  - `local.settings.json`: `"LocalHttpPort": 7151`
  - `index.html` (line 7): `window.apiBaseUrl = 'http://localhost:7151'`

**If you MUST change the port:**

1. Contact me immediately to update upstream server-side configurations
2. Update all three files above consistently
3. Restart the application completely

## Browser Developer Tools - CRITICAL for Debugging

**🔧 ALWAYS Open Browser Developer Tools When Testing**

When testing the web interface, it's essential to have your browser's developer tools open to capture any errors. This information is crucial for troubleshooting and getting help from the team.

### How to Open Developer Tools

**Chrome/Edge:**

- Press `F12` or `Ctrl+Shift+I` (Windows/Linux) or `Cmd+Option+I` (Mac)
- Or right-click on the page and select "Inspect"

**Firefox:**

- Press `F12` or `Ctrl+Shift+I` (Windows/Linux) or `Cmd+Option+I` (Mac)
- Or right-click on the page and select "Inspect Element"

**Safari:**

- Press `Cmd+Option+I` (Mac)
- Or go to `Develop > Show Web Inspector` (enable Develop menu in Preferences first)

### Key Tabs to Monitor

1. **Console Tab** - Shows JavaScript errors and log messages
   - Look for red error messages
   - Yellow warnings may also be important
   - Network connection errors will appear here

2. **Network Tab** - Shows API calls and responses
   - Filter by "Fetch/XHR" to see API calls
   - Look for failed requests (red status codes)
   - Check response bodies for error details

3. **Application/Storage Tab** - Shows local storage and session data
   - Check if SignalR connections are established
   - Verify API base URL configuration

### What to Capture When Reporting Issues

**Before contacting for help, please capture:**

1. **Console Errors:**
   - Take a screenshot of the Console tab showing any red errors
   - Copy the full error text (right-click error → "Copy message")

2. **Network Failures:**
   - Screenshot of the Network tab showing failed requests
   - Click on failed requests to see details
   - Copy the request URL and response body

3. **Browser Information:**
   - Note which browser and version you're using
   - Include operating system (Windows/Mac/Linux)

### Common Error Patterns to Watch For

**SignalR Connection Issues:**

```javascript
Error: Failed to start the connection: Error: WebSocket failed to connect.
```

**API Connection Issues:**

```javascript
Error: Failed to fetch
TypeError: NetworkError when attempting to fetch resource
```

**CORS Issues:**

```javascript
Access to fetch at 'http://localhost:7151/api/...' from origin 'null' has been blocked by CORS policy
```

**Translation Service Issues:**

```javascript
Error: Translation service returned error: [specific error message]
```

### Testing Workflow with Developer Tools

1. **Open the application** in your browser: `http://localhost:7151/api/content/index.html`
2. **Open Developer Tools** immediately (F12)
3. **Clear the console** (click the clear icon or press Ctrl+L)
4. **Perform your test actions** (join chat, send messages, etc.)
5. **Monitor for errors** in real-time
6. **Capture screenshots** of any errors before they scroll away
7. **Test network connectivity** by checking the Network tab

### Quick Debug Checklist

Before reporting issues, verify in browser dev tools:

- [ ] No red errors in Console tab
- [ ] API calls in Network tab are returning 200 status codes
- [ ] SignalR connection is established (check Console for connection messages)
- [ ] No CORS errors preventing API access
- [ ] Browser can reach `http://localhost:7151/api/isusernameavailable/test`

**💡 Pro Tip:** Keep developer tools open during your entire testing session. Many errors are transient and might be missed if you only open the tools after problems occur.

## Local Debug Checklist

Before reporting issues, verify:

- [ ] .NET 8.0 SDK is installed
- [ ] Azure Functions Core Tools v4 is installed
- [ ] `local.settings.json` exists with correct values
- [ ] Port 7151 is available
- [ ] All NuGet packages are restored
- [ ] Project builds successfully (`dotnet build`)
- [ ] Basic endpoint responds (`curl http://localhost:7151/api/isusernameavailable/test`)

## Support and Communication

### Getting Help

1. **Check the logs** in the terminal where `func start` is running
2. **Try the troubleshooting steps** above
3. **Collect diagnostic information:**
   ```bash
   # System info
   dotnet --info
   func --version
   
   # Project info
   dotnet list package
   ```

### Reporting Issues

When reporting issues, please include:
- Complete error messages
- Steps to reproduce
- Your operating system
- Output of `dotnet --info` and `func --version`
- Whether the issue occurs on a fresh clone

### Configuration Updates

**Important:** If you need to modify any configuration:

- **Port changes**: Contact me immediately before making changes
- **Connection strings**: These will be provided separately via secure channels
- **New dependencies**: Coordinate with the team before adding

## Security Notes

- Never commit `local.settings.json` to version control
- Don't share connection strings in public channels
- Use secure communication for sensitive configuration data
- Report any suspected security issues immediately

---

**Happy Testing!** 🚀

*For urgent issues or questions, reach out via our secure communication channel.*
