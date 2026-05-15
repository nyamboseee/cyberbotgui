# CyberBot v2.0 — WPF GUI (Part 2)

A Cybersecurity Awareness Chatbot with a modern WPF GUI, sentiment detection, memory, keyword recognition, and random responses.

## Features Implemented

### 1. GUI Design (Q1)
- Full WPF application with a dark cybersecurity-themed UI
- Colour-coded chat bubbles (blue for user, dark panel for bot)
- Sidebar with memory panel, sentiment panel, and quick-topic buttons
- Animated typing indicator (three bouncing dots)
- Fade-in animation for each new message

### 2. Keyword Recognition (Q2)
Recognises 10+ cybersecurity keywords and responds with targeted advice:
- `password`, `phishing`, `malware`, `privacy`, `scam`, `vpn`, `2fa`,
  `firewall`, `ransomware`, `backup`, `social engineering`

### 3. Random Responses (Q3)
For each keyword topic, 2–3 different responses are stored in a `Dictionary<string, List<string>>`.
Each time a topic is triggered, a random response is selected — so repeated questions feel varied.

### 4. Conversation Flow (Q4)
- Follow-up phrases like *"tell me more"*, *"give me another tip"*, *"explain more"* continue the current topic without restarting
- The bot remembers the last discussed topic and picks a fresh random response from that pool

### 5. Memory and Recall (Q5)
- User can introduce themselves: *"My name is Mpumelelo"*
- User can register an interest: *"I'm interested in privacy"*
- The bot stores name and favourite topic in a `UserMemory` class
- Name appears in the header avatar and sidebar; topic is personalised in responses

### 6. Sentiment Detection (Q6)
- Detects: **Worried**, **Curious**, **Frustrated**, **Positive**, **Neutral**
- Each sentiment adjusts the bot's opening line (empathetic, encouraging, clarifying, etc.)
- Sidebar sentiment panel and animated mood bar update with every message

### 7. Error Handling (Q7)
- Unknown inputs return a friendly default response suggesting valid topics
- No crashes on empty input or unrecognised keywords

### 8. Code Optimisation (Q8)
- `Dictionary<string, List<string>>` for keyword-response storage (O(1) lookup)
- `UserMemory` class for encapsulated state
- `ChatEngine` class separates all logic from the UI layer (MVVM-lite pattern)
- `Sentiment` enum for type-safe sentiment tracking

## Project Structure

```
CyberBotWPF/
├── App.xaml                  # Application entry point
├── App.xaml.cs
├── MainWindow.xaml           # Full WPF UI layout
├── MainWindow.xaml.cs        # UI interaction / code-behind
├── ChatEngine.cs             # All chatbot logic (keywords, sentiment, memory)
├── CyberBotWPF.csproj        # Project file (.NET 8 WPF)
└── README.md
```

## How to Run

1. Open Visual Studio 2022 (or later)
2. Create a new **WPF App (.NET)** project named `CyberBotWPF`
3. Replace the generated files with the files in this folder
4. Press **F5** to build and run

> Requires: .NET 8 SDK, Visual Studio 2022 with the .NET desktop development workload

## GitHub Commit Suggestions (for the 6-commit requirement)

```
feat: add WPF project scaffold and dark UI theme
feat: implement ChatEngine with keyword recognition and random responses
feat: add sentiment detection with mood bar and sidebar panel
feat: implement UserMemory for name and topic recall
feat: add animated typing indicator and fade-in message bubbles
feat: add sidebar quick-topic buttons and conversation flow handling
```