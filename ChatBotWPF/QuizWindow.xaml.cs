using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CyberBotWPF
{
    public class QuizQuestion
    {
        public string Question { get; set; }
        public List<string> Options { get; set; }
        public int CorrectIndex { get; set; }
        public string Explanation { get; set; }
        public string Type { get; set; } // "MULTIPLE CHOICE" or "TRUE / FALSE"
    }

    public partial class QuizWindow : Window
    {
        private readonly List<QuizQuestion> _questions;
        private int _currentIndex = 0;
        private int _score = 0;
        private bool _answered = false;

        // Expose score and total for activity log
        public int FinalScore => _score;
        public int TotalQuestions => _questions.Count;

        public QuizWindow()
        {
            InitializeComponent();
            _questions = BuildQuestions();
            Shuffle(_questions);
            LoadQuestion();
        }

        // ──────────────────────────────────────────
        //  Question bank
        // ──────────────────────────────────────────
        private List<QuizQuestion> BuildQuestions()
        {
            return new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    Question = "What should you do if you receive an email asking for your password?",
                    Options = new List<string> { "A)  Reply with your password", "B)  Delete the email", "C)  Report the email as phishing", "D)  Ignore it" },
                    CorrectIndex = 2,
                    Explanation = "Reporting phishing emails helps protect others. Legitimate organisations will never ask for your password via email.",
                    Type = "MULTIPLE CHOICE"
                },
                new QuizQuestion
                {
                    Question = "Which of the following is the strongest password?",
                    Options = new List<string> { "A)  password123", "B)  MyDogSpot", "C)  Tr0ub4dor&3!", "D)  123456" },
                    CorrectIndex = 2,
                    Explanation = "A strong password mixes uppercase, lowercase, numbers and symbols. Avoid dictionary words and predictable patterns.",
                    Type = "MULTIPLE CHOICE"
                },
                new QuizQuestion
                {
                    Question = "Two-Factor Authentication (2FA) makes your account less secure.",
                    Options = new List<string> { "A)  True", "B)  False" },
                    CorrectIndex = 1,
                    Explanation = "False. 2FA adds an extra layer of security. Even if your password is stolen, attackers cannot access your account without the second factor.",
                    Type = "TRUE / FALSE"
                },
                new QuizQuestion
                {
                    Question = "What does a VPN primarily do?",
                    Options = new List<string> { "A)  Speeds up your internet connection", "B)  Encrypts your internet traffic", "C)  Removes viruses from your device", "D)  Blocks all advertisements" },
                    CorrectIndex = 1,
                    Explanation = "A VPN (Virtual Private Network) encrypts your traffic and masks your IP address, protecting your data especially on public Wi-Fi.",
                    Type = "MULTIPLE CHOICE"
                },
                new QuizQuestion
                {
                    Question = "It is safe to use the same password for multiple accounts.",
                    Options = new List<string> { "A)  True", "B)  False" },
                    CorrectIndex = 1,
                    Explanation = "False. If one account is breached, attackers will use that password to try to access your other accounts — a technique called credential stuffing.",
                    Type = "TRUE / FALSE"
                },
                new QuizQuestion
                {
                    Question = "What is phishing?",
                    Options = new List<string> { "A)  A type of computer virus", "B)  A deceptive attempt to steal sensitive information", "C)  A method to speed up your browser", "D)  A firewall configuration technique" },
                    CorrectIndex = 1,
                    Explanation = "Phishing is a social engineering attack where criminals impersonate trusted entities to trick users into revealing passwords or personal data.",
                    Type = "MULTIPLE CHOICE"
                },
                new QuizQuestion
                {
                    Question = "Which of the following best protects against ransomware?",
                    Options = new List<string> { "A)  Using a VPN", "B)  Keeping regular offline backups", "C)  Using a stronger password", "D)  Disabling your firewall" },
                    CorrectIndex = 1,
                    Explanation = "Regular offline backups mean you can restore your data without paying the ransom. Backups are your best defence against ransomware.",
                    Type = "MULTIPLE CHOICE"
                },
                new QuizQuestion
                {
                    Question = "Public Wi-Fi networks are always safe to use for banking.",
                    Options = new List<string> { "A)  True", "B)  False" },
                    CorrectIndex = 1,
                    Explanation = "False. Public Wi-Fi is a common target for man-in-the-middle attacks. Always use a VPN or avoid sensitive transactions on public networks.",
                    Type = "TRUE / FALSE"
                },
                new QuizQuestion
                {
                    Question = "What is social engineering in the context of cybersecurity?",
                    Options = new List<string> { "A)  Writing code for social media platforms", "B)  Manipulating people into revealing confidential information", "C)  Building secure network infrastructure", "D)  Installing software updates automatically" },
                    CorrectIndex = 1,
                    Explanation = "Social engineering exploits human psychology rather than technical vulnerabilities. Attackers manipulate people into giving up passwords or access.",
                    Type = "MULTIPLE CHOICE"
                },
                new QuizQuestion
                {
                    Question = "Which type of 2FA is considered the most secure?",
                    Options = new List<string> { "A)  SMS text message code", "B)  Email verification code", "C)  Authenticator app code", "D)  Security question" },
                    CorrectIndex = 2,
                    Explanation = "Authenticator apps generate time-based codes locally and are not vulnerable to SIM-swapping attacks, making them more secure than SMS-based 2FA.",
                    Type = "MULTIPLE CHOICE"
                },
                new QuizQuestion
                {
                    Question = "Keeping your software updated is an important cybersecurity practice.",
                    Options = new List<string> { "A)  True", "B)  False" },
                    CorrectIndex = 0,
                    Explanation = "True. Software updates patch known security vulnerabilities. Attackers actively exploit outdated software to gain access to systems.",
                    Type = "TRUE / FALSE"
                },
                new QuizQuestion
                {
                    Question = "What does HTTPS indicate about a website?",
                    Options = new List<string> { "A)  The website is owned by a trusted company", "B)  The connection between your browser and the site is encrypted", "C)  The website has no viruses", "D)  The website loads faster than HTTP sites" },
                    CorrectIndex = 1,
                    Explanation = "HTTPS means the connection is encrypted using SSL/TLS. However, it does not guarantee the site itself is safe or legitimate.",
                    Type = "MULTIPLE CHOICE"
                },
            };
        }

        // ──────────────────────────────────────────
        //  Load a question into the UI
        // ──────────────────────────────────────────
        private void LoadQuestion()
        {
            if (_currentIndex >= _questions.Count)
            {
                ShowFinalScreen();
                return;
            }

            _answered = false;
            FeedbackPanel.Visibility = Visibility.Collapsed;
            NextBtn.IsEnabled = false;
            FinalMessage.Text = "";

            var q = _questions[_currentIndex];

            // Update labels
            QuestionTypeBadge.Text = q.Type;
            QuestionText.Text = q.Question;
            ProgressLabel.Text = $"Question {_currentIndex + 1} of {_questions.Count}";
            ScoreLabel.Text = $"{_score} / {_currentIndex}";

            // Animate progress bar
            double totalWidth = 600; // approximate
            double targetWidth = ((_currentIndex + 1.0) / _questions.Count) * (totalWidth - 60);
            var anim = new DoubleAnimation(ProgressBar.Width, Math.Max(0, targetWidth), TimeSpan.FromMilliseconds(400));
            ProgressBar.BeginAnimation(WidthProperty, anim);

            // Show/hide buttons based on question type
            bool isMultiChoice = q.Options.Count == 4;
            BtnC.Visibility = isMultiChoice ? Visibility.Visible : Visibility.Collapsed;
            BtnD.Visibility = isMultiChoice ? Visibility.Visible : Visibility.Collapsed;

            // Set button content
            var buttons = new[] { BtnA, BtnB, BtnC, BtnD };
            for (int i = 0; i < q.Options.Count; i++)
                buttons[i].Content = q.Options[i];

            // Reset button colours
            foreach (var btn in buttons)
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(20, 28, 46));
                btn.BorderBrush = new SolidColorBrush(Color.FromRgb(30, 58, 95));
                btn.IsEnabled = true;
            }

            RestartBtn.Visibility = Visibility.Collapsed;
            NextBtn.Content = _currentIndex == _questions.Count - 1 ? "FINISH" : "NEXT";
        }

        // ──────────────────────────────────────────
        //  Answer selected
        // ──────────────────────────────────────────
        private void Answer_Click(object sender, RoutedEventArgs e)
        {
            if (_answered) return;
            _answered = true;

            var btn = (Button)sender;
            int selected = int.Parse(btn.Tag.ToString());
            var q = _questions[_currentIndex];
            bool correct = selected == q.CorrectIndex;

            if (correct) _score++;

            // Colour the buttons
            var buttons = new[] { BtnA, BtnB, BtnC, BtnD };
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].IsEnabled = false;
                if (i == q.CorrectIndex)
                    buttons[i].Background = new SolidColorBrush(Color.FromRgb(0, 40, 25));
                else if (i == selected && !correct)
                    buttons[i].Background = new SolidColorBrush(Color.FromRgb(60, 10, 20));
            }

            // Show feedback
            FeedbackPanel.Visibility = Visibility.Visible;
            if (correct)
            {
                FeedbackResult.Text = "Correct!";
                FeedbackResult.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 159));
            }
            else
            {
                FeedbackResult.Text = $"Incorrect. The correct answer was: {q.Options[q.CorrectIndex]}";
                FeedbackResult.Foreground = new SolidColorBrush(Color.FromRgb(255, 64, 96));
            }
            FeedbackExplanation.Text = q.Explanation;

            NextBtn.IsEnabled = true;
            ScoreLabel.Text = $"{_score} / {_currentIndex + 1}";
        }

        // ──────────────────────────────────────────
        //  Next question
        // ──────────────────────────────────────────
        private void Next_Click(object sender, RoutedEventArgs e)
        {
            _currentIndex++;
            LoadQuestion();
        }

        // ──────────────────────────────────────────
        //  Final screen
        // ──────────────────────────────────────────
        private void ShowFinalScreen()
        {
            QuestionText.Text = "Quiz Complete!";
            QuestionTypeBadge.Text = "RESULTS";
            BtnA.Visibility = Visibility.Collapsed;
            BtnB.Visibility = Visibility.Collapsed;
            BtnC.Visibility = Visibility.Collapsed;
            BtnD.Visibility = Visibility.Collapsed;
            FeedbackPanel.Visibility = Visibility.Collapsed;
            NextBtn.Visibility = Visibility.Collapsed;
            RestartBtn.Visibility = Visibility.Visible;

            double percentage = (double)_score / _questions.Count * 100;
            string feedback;
            if (percentage >= 90)
                feedback = "Outstanding! You are a cybersecurity expert.";
            else if (percentage >= 70)
                feedback = "Great job! You have a solid understanding of cybersecurity.";
            else if (percentage >= 50)
                feedback = "Good effort. Keep learning to stay safe online.";
            else
                feedback = "Keep learning! Cybersecurity knowledge is essential in today's world.";

            ScoreLabel.Text = $"{_score} / {_questions.Count}";
            FinalMessage.Text = $"You scored {_score} out of {_questions.Count} ({percentage:0}%).\n{feedback}";
            ProgressLabel.Text = "Quiz finished";
        }

        // ──────────────────────────────────────────
        //  Restart
        // ──────────────────────────────────────────
        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            _currentIndex = 0;
            _score = 0;
            Shuffle(_questions);

            BtnA.Visibility = Visibility.Visible;
            BtnB.Visibility = Visibility.Visible;
            NextBtn.Visibility = Visibility.Visible;
            RestartBtn.Visibility = Visibility.Collapsed;
            FinalMessage.Text = "";

            LoadQuestion();
        }

        // ──────────────────────────────────────────
        //  Shuffle helper
        // ──────────────────────────────────────────
        private void Shuffle(List<QuizQuestion> list)
        {
            var rng = new Random();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}