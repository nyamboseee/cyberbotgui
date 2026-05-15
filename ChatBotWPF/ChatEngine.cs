using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberBotWPF
{
    // ─────────────────────────────────────────────
    //  Sentiment enum
    // ─────────────────────────────────────────────
    public enum Sentiment { Positive, Curious, Worried, Frustrated, Neutral }

    // ─────────────────────────────────────────────
    //  User memory store
    // ─────────────────────────────────────────────
    public class UserMemory
    {
        public string Name { get; set; } = string.Empty;
        public string FavouriteTopic { get; set; } = string.Empty;
        public int MessageCount { get; set; } = 0;
        public string LastTopic { get; set; } = string.Empty;
        public bool AskedForMore { get; set; } = false;
    }

    // ─────────────────────────────────────────────
    //  Main chat engine
    // ─────────────────────────────────────────────
    public class ChatEngine
    {
        private readonly Random _rng = new();
        private readonly UserMemory _memory = new();
        private Sentiment _currentSentiment = Sentiment.Neutral;

        public UserMemory Memory => _memory;
        public Sentiment CurrentSentiment => _currentSentiment;

        // ── Keyword → responses (multiple per topic for random selection) ──
        private readonly Dictionary<string, List<string>> _keywordResponses = new(StringComparer.OrdinalIgnoreCase)
        {
            ["password"] = new List<string>
            {
                "🔐 Use strong, unique passwords for every account. A mix of uppercase, lowercase, numbers and symbols makes it much harder for attackers to crack.",
                "🔐 Never reuse passwords across sites. If one gets breached, attackers will try it everywhere — a technique called credential stuffing.",
                "🔐 Consider using a password manager like Bitwarden or KeePass. It generates and stores complex passwords so you only need to remember one master password.",
            },
            ["phishing"] = new List<string>
            {
                "🎣 Be cautious of emails asking for personal information. Scammers often disguise themselves as trusted organisations like banks or government agencies.",
                "🎣 Always check the sender's actual email address — not just the display name. Phishing emails often use addresses like 'support@paypa1.com' with a digit instead of a letter.",
                "🎣 If a message creates urgency ('Your account will be suspended!'), slow down. Legitimate organisations rarely pressure you to act immediately via email.",
            },
            ["malware"] = new List<string>
            {
                "🦠 Keep your operating system and software up to date. Most malware exploits known vulnerabilities that patches have already fixed.",
                "🦠 Avoid downloading software from unofficial sources. Stick to official websites and verified app stores to minimise malware risk.",
                "🦠 Install a reputable antivirus tool and run regular scans. Real-time protection can catch threats before they cause damage.",
            },
            ["privacy"] = new List<string>
            {
                "🔒 Review the privacy settings on your social media accounts regularly. Limit what strangers can see about you.",
                "🔒 Use a VPN on public Wi-Fi to encrypt your traffic. Coffee shop networks are a common hunting ground for data thieves.",
                "🔒 Be mindful of what personal information you share online — even small details can be pieced together by bad actors.",
            },
            ["scam"] = new List<string>
            {
                "💳 If an offer sounds too good to be true, it probably is. Online scams often promise large rewards for small upfront payments.",
                "💳 Legitimate companies will never ask for payment via gift cards or cryptocurrency. These methods are nearly untraceable — a favourite of scammers.",
                "💳 Verify any unsolicited contact independently. If your 'bank' calls, hang up and call the official number on their website.",
            },
            ["vpn"] = new List<string>
            {
                "🌐 A VPN (Virtual Private Network) encrypts your internet traffic, making it much harder for anyone on the same network to intercept your data.",
                "🌐 VPNs can also help protect your identity by masking your real IP address. Choose a reputable provider that has a strict no-logs policy.",
            },
            ["2fa"] = new List<string>
            {
                "🛡️ Two-Factor Authentication (2FA) adds a second layer of security beyond just your password. Even if your password leaks, attackers still can't get in.",
                "🛡️ Prefer authenticator apps (like Google Authenticator or Authy) over SMS-based 2FA. SIM-swapping attacks can intercept SMS codes.",
            },
            ["firewall"] = new List<string>
            {
                "🧱 A firewall monitors incoming and outgoing network traffic and blocks suspicious connections. Keep your system firewall enabled at all times.",
                "🧱 Both hardware and software firewalls play a role. A router firewall protects your whole network; a host firewall protects one device.",
            },
            ["ransomware"] = new List<string>
            {
                "🔓 Ransomware encrypts your files and demands payment for the key. Regular offline backups are your best defence — if you have backups, you can restore without paying.",
                "🔓 Never open email attachments from unknown senders. Ransomware is commonly delivered through phishing emails with malicious attachments.",
            },
            ["backup"] = new List<string>
            {
                "💾 Follow the 3-2-1 backup rule: 3 copies of your data, on 2 different media types, with 1 stored offsite (or in the cloud).",
                "💾 Test your backups regularly. A backup you've never restored from is a backup you can't trust.",
            },
            ["social engineering"] = new List<string>
            {
                "🎭 Social engineering attacks manipulate people rather than systems. Always verify the identity of anyone requesting sensitive information — even if they claim to be IT support.",
                "🎭 Pretexting, baiting, and tailgating are all forms of social engineering. Awareness is your best defence.",
            },
        };

        // ── Sentiment keyword maps ──
        private readonly Dictionary<Sentiment, List<string>> _sentimentKeywords = new()
        {
            [Sentiment.Worried] = new() { "worried", "scared", "afraid", "anxious", "nervous", "concern", "hack", "hacked", "stolen", "danger", "dangerous" },
            [Sentiment.Frustrated] = new() { "frustrated", "annoying", "confusing", "confused", "difficult", "hard", "complicated", "hate", "angry", "useless" },
            [Sentiment.Curious] = new() { "how", "what", "why", "explain", "tell me", "curious", "learn", "understand", "interesting", "know more" },
            [Sentiment.Positive] = new() { "thanks", "thank you", "great", "awesome", "helpful", "love", "good", "perfect", "excellent", "appreciate" },
        };

        // ── Sentiment-adjusted openers ──
        private readonly Dictionary<Sentiment, List<string>> _sentimentOpeners = new()
        {
            [Sentiment.Worried] = new()
            {
                "It's completely understandable to feel that way. Let me share something that should help:",
                "I hear your concern — that's actually a smart thing to be cautious about. Here's what you should know:",
                "You're right to take this seriously. Here's some guidance:",
            },
            [Sentiment.Frustrated] = new()
            {
                "I understand this can feel overwhelming. Let's break it down simply:",
                "Cybersecurity can be a lot to take in — let me make this clearer for you:",
                "Fair enough, it can be confusing. Here's a straightforward explanation:",
            },
            [Sentiment.Curious] = new()
            {
                "Great question! Here's what you need to know:",
                "I love the curiosity! Here's a solid answer:",
                "Good thinking to ask about that. Here's the breakdown:",
            },
            [Sentiment.Positive] = new()
            {
                "Glad to help! Here's some more useful info:",
                "That's the spirit! Stay informed and stay safe:",
                "Awesome! Here's your next cybersecurity insight:",
            },
            [Sentiment.Neutral] = new()
            {
                "",
                "",
                "",
            },
        };

        // ── Follow-up/continuation triggers ──
        private readonly List<string> _moreKeywords = new()
        {
            "tell me more", "explain more", "give me another", "more tips", "more info",
            "continue", "go on", "what else", "anything else", "expand on that", "more"
        };

        // ── Greetings ──
        private readonly List<string> _greetings = new()
        {
            "hello", "hi", "hey", "howdy", "greetings", "good morning", "good afternoon", "good evening","sawubona"
        };

        // ── Default / fallback responses ──
        private readonly List<string> _fallbacks = new()
        {
            "I'm not sure I understand that. Could you try rephrasing? I can help with topics like passwords, phishing, malware, privacy, scams, VPNs, and 2FA.",
            "That's outside my current knowledge base. Try asking me about phishing, ransomware, passwords, or online privacy.",
            "Hmm, I didn't quite catch that. I'm best at cybersecurity topics — feel free to ask about things like scams, firewalls, or two-factor authentication.",
            "Remember, staying safe online starts with awareness. Ask me about passwords, phishing, or scams!"
        };

        // ──────────────────────────────────────────
        //  Public: process a user message
        // ──────────────────────────────────────────
        public string ProcessMessage(string userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput))
                return "Please type a message so I can help you!";

            _memory.MessageCount++;
            string input = userInput.Trim().ToLower();

            // 1. Detect sentiment
            _currentSentiment = DetectSentiment(input);

            // 2. Check for name introduction
            string nameResponse = TryExtractName(input);
            if (nameResponse != null) return nameResponse;

            // 3. Check for interest/topic registration
            string interestResponse = TryExtractInterest(input);
            if (interestResponse != null) return interestResponse;

            // 4. Greetings
            if (_greetings.Any(g => input.Contains(g)))
                return BuildGreeting();

            // 5. Follow-up / "tell me more"
            if (_moreKeywords.Any(k => input.Contains(k)))
                return HandleMoreRequest();

            // 6. Keyword matching
            string keywordResponse = TryMatchKeyword(input);
            if (keywordResponse != null) return keywordResponse;

            // 7. Fallback
            return PickRandom(_fallbacks);
        }

        // ──────────────────────────────────────────
        //  Sentiment detection
        // ──────────────────────────────────────────
        public Sentiment DetectSentiment(string input)
        {
            foreach (var kvp in _sentimentKeywords)
                if (kvp.Value.Any(k => input.Contains(k)))
                    return kvp.Key;
            return Sentiment.Neutral;
        }

        // ──────────────────────────────────────────
        //  Name extraction
        // ──────────────────────────────────────────
        private string TryExtractName(string input)
        {
            string[] nameTriggers = { "my name is ", "i am ", "i'm ", "call me " };
            foreach (var trigger in nameTriggers)
            {
                int idx = input.IndexOf(trigger, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    string rest = input.Substring(idx + trigger.Length).Trim();
                    string name = rest.Split(' ')[0];
                    name = char.ToUpper(name[0]) + name.Substring(1);
                    _memory.Name = name;
                    return $"Nice to meet you, {name}! 👋 I'm CyberBot, your personal cybersecurity guide. What would you like to learn about today?";
                }
            }
            return null;
        }

        // ──────────────────────────────────────────
        //  Interest extraction
        // ──────────────────────────────────────────
        private string TryExtractInterest(string input)
        {
            string[] interestTriggers = { "i'm interested in ", "i am interested in ", "i care about ", "i want to learn about " };
            foreach (var trigger in interestTriggers)
            {
                int idx = input.IndexOf(trigger, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    string topic = input.Substring(idx + trigger.Length).Trim().TrimEnd('.');
                    _memory.FavouriteTopic = topic;
                    string namePrefix = string.IsNullOrEmpty(_memory.Name) ? "" : $"{_memory.Name}, ";
                    return $"Great! I'll remember that {namePrefix}you're interested in {topic}. It's a crucial part of staying safe online. " +
                           $"Feel free to ask me anything about it! 🔐";
                }
            }
            return null;
        }

        // ──────────────────────────────────────────
        //  Greeting builder
        // ──────────────────────────────────────────
        private string BuildGreeting()
        {
            string name = string.IsNullOrEmpty(_memory.Name) ? "there" : _memory.Name;
            List<string> options = new()
            {
                $"Hey {name}! 👋 How can I help you stay safe online today?",
                $"Hello {name}! Welcome back to CyberBot. What cybersecurity topic can I help with?",
                $"Hi {name}! 🛡️ Ready to level up your digital security? Ask me anything.",
            };
            return PickRandom(options);
        }

        // ──────────────────────────────────────────
        //  Follow-up handler
        // ──────────────────────────────────────────
        private string HandleMoreRequest()
        {
            if (!string.IsNullOrEmpty(_memory.LastTopic) && _keywordResponses.ContainsKey(_memory.LastTopic))
            {
                string opener = BuildSentimentOpener();
                string tip = PickRandom(_keywordResponses[_memory.LastTopic]);
                string personalise = string.IsNullOrEmpty(_memory.FavouriteTopic) ? "" :
                    $"\n\n💡 As someone interested in {_memory.FavouriteTopic}, this is especially relevant for you.";
                return (opener.Length > 0 ? opener + "\n\n" : "") + tip + personalise;
            }
            return "What topic would you like to explore further? Try asking about passwords, phishing, malware, privacy, or scams.";
        }

        // ──────────────────────────────────────────
        //  Keyword matching
        // ──────────────────────────────────────────
        private string TryMatchKeyword(string input)
        {
            foreach (var kvp in _keywordResponses)
            {
                if (input.Contains(kvp.Key))
                {
                    _memory.LastTopic = kvp.Key;
                    string opener = BuildSentimentOpener();
                    string response = PickRandom(kvp.Value);

                    // Memory personalisation
                    string memoryNote = "";
                    if (!string.IsNullOrEmpty(_memory.FavouriteTopic) &&
                        input.Contains(_memory.FavouriteTopic.ToLower()))
                    {
                        string name = string.IsNullOrEmpty(_memory.Name) ? "you" : _memory.Name;
                        memoryNote = $"\n\n💡 As someone interested in {_memory.FavouriteTopic}, {name}, you might want to review your account security settings too.";
                    }

                    return (opener.Length > 0 ? opener + "\n\n" : "") + response + memoryNote;
                }
            }
            return null;
        }

        // ──────────────────────────────────────────
        //  Sentiment opener
        // ──────────────────────────────────────────
        private string BuildSentimentOpener()
        {
            var openers = _sentimentOpeners[_currentSentiment];
            return PickRandom(openers);
        }

        // ──────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────
        private string PickRandom(List<string> options)
            => options[_rng.Next(options.Count)];

        public (string label, string emoji, string description, double barWidth, string barColor) GetSentimentDisplay()
        {
            return _currentSentiment switch
            {
                Sentiment.Worried => ("Worried", "😟", "User seems anxious or concerned.", 0.75, "#FF4060"),
                Sentiment.Frustrated => ("Frustrated", "😤", "User appears frustrated or confused.", 0.5, "#FF9F00"),
                Sentiment.Curious => ("Curious", "🤔", "User is actively seeking information.", 0.85, "#00B4FF"),
                Sentiment.Positive => ("Positive", "😊", "User is in a positive, happy mood.", 1.0, "#00FF9F"),
                _ => ("Neutral", "😐", "User mood appears neutral.", 0.5, "#5A6A8A"),
            };
        }

        public string GetWelcomeMessage()
        {
            return "👋 Welcome to CyberBot v2.0!\n\n" +
                   "I'm your personal cybersecurity awareness assistant. I can help you understand:\n\n" +
                   "🔐 Password safety\n" +
                   "🎣 Phishing attacks\n" +
                   "🦠 Malware protection\n" +
                   "🔒 Online privacy\n" +
                   "💳 Scam awareness\n" +
                   "🛡️ Two-factor authentication\n" +
                   "🌐 VPN usage\n\n" +
                   "You can also tell me your name and what topics interest you — I'll remember! Start by saying 'My name is...' or just ask a question.";
        }
    }
}

