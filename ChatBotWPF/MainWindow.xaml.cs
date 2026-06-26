using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace CyberBotWPF
{
    public partial class MainWindow : Window
    {
        private readonly ChatEngine _engine = new();
        private bool _isTyping = false;
        private readonly List<string> _activityLog = new();

        public MainWindow()
        {
            InitializeComponent();
        }

        // ── Window loaded ──
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AppendBotMessage(_engine.GetWelcomeMessage());
            LogActivity("Session started.");
        }

        // ── Send on Enter ──
        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !e.IsRepeat)
            {
                e.Handled = true;
                ProcessInput();
            }
        }

        // ── Send button ──
        private void SendButton_Click(object sender, RoutedEventArgs e)
            => ProcessInput();

        // ── Clear button ──
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ChatPanel.Children.Clear();
            AppendBotMessage("Chat cleared! Ask me anything about cybersecurity.");
            LogActivity("Chat cleared by user.");
        }

        // ── Quick topic buttons from sidebar ──
        private void QuickTopic_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string topic)
            {
                InputBox.Text = topic;
                ProcessInput();
            }
        }

        // ── Open Task Manager ──
        private void OpenTaskManager_Click(object sender, RoutedEventArgs e)
        {
            LogActivity("Task Manager opened.");
            var taskWin = new TaskWindow();
            taskWin.Closed += (s, args) =>
            {
                if (!string.IsNullOrEmpty(taskWin.LastTaskTitle))
                    LogActivity($"Task added: '{taskWin.LastTaskTitle}'.");
            };
            taskWin.ShowDialog();
        }

        // ── Open Quiz ──
        private void OpenQuiz_Click(object sender, RoutedEventArgs e)
        {
            LogActivity("Quiz started.");
            var quiz = new QuizWindow();
            quiz.Closed += (s, args) =>
            {
                LogActivity($"Quiz completed. Score: {quiz.FinalScore} / {quiz.TotalQuestions}.");
                AppendBotMessage($"Welcome back! You scored {quiz.FinalScore} out of {quiz.TotalQuestions} on the quiz. " +
                                 $"Ask me anything to keep learning.");
            };
            quiz.ShowDialog();
        }

        // ── Show Activity Log ──
        private void ShowActivityLog_Click(object sender, RoutedEventArgs e)
        {
            ShowActivityLogInChat();
        }

        // ── Input text changed ──
        private void InputBox_TextChanged(object sender, TextChangedEventArgs e) { }

        // ──────────────────────────────────────────
        //  Core: process user message
        // ──────────────────────────────────────────
        private async void ProcessInput()
        {
            if (_isTyping) return;

            string text = InputBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            InputBox.Clear();

            // Check for task manager command (NLP)
            if (IsTaskManagerRequest(text))
            {
                AppendUserMessage(text);
                LogActivity("Task Manager opened via chat command.");
                var taskWin = new TaskWindow();
                taskWin.ShowDialog();
                AppendBotMessage("Task Manager closed. Your tasks have been updated.");
                return;
            }

            // Check for activity log command (NLP)
            if (IsActivityLogRequest(text))
            {
                AppendUserMessage(text);
                ShowActivityLogInChat();
                return;
            }

            // Show user bubble
            AppendUserMessage(text);

            // Show typing indicator
            var typingBubble = AppendTypingIndicator();
            _isTyping = true;

            // Simulate typing delay
            int delay = Math.Max(600, Math.Min(2000, text.Length * 20));
            await Task.Delay(delay);

            // Get response
            string response = _engine.ProcessMessage(text);

            // Log NLP action
            LogActivity($"User asked about: \"{TruncateForLog(text)}\".");

            // Remove typing indicator
            ChatPanel.Children.Remove(typingBubble);
            _isTyping = false;

            // Show bot response
            AppendBotMessage(response);

            // Update sidebar
            UpdateMemoryPanel();
            UpdateSentimentPanel();

            // Scroll to bottom
            ChatScroller.ScrollToBottom();
        }

        // ──────────────────────────────────────────
        //  NLP: detect activity log request
        // ──────────────────────────────────────────
        private bool IsTaskManagerRequest(string input)
        {
            input = input.ToLower();
            string[] triggers = {
                "add task", "my tasks", "show tasks", "view tasks", "task manager",
                "open tasks", "manage tasks", "set reminder", "add a task",
                "create task", "new task", "task list"
            };
            foreach (var t in triggers)
                if (input.Contains(t)) return true;
            return false;
        }

        private bool IsActivityLogRequest(string input)
        {
            input = input.ToLower();
            string[] triggers = {
                "show activity log", "activity log", "what have you done",
                "show log", "view log", "recent actions", "what have you done for me",
                "show me what you did", "history"
            };
            foreach (var t in triggers)
                if (input.Contains(t)) return true;
            return false;
        }

        // ──────────────────────────────────────────
        //  Activity log
        // ──────────────────────────────────────────
        private void LogActivity(string description)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}]  {description}";
            _activityLog.Add(entry);
        }

        private void ShowActivityLogInChat()
        {
            if (_activityLog.Count == 0)
            {
                AppendBotMessage("No activity recorded yet. Start chatting or take the quiz!");
                return;
            }

            // Show last 5–10 entries
            int start = Math.Max(0, _activityLog.Count - 10);
            var recent = _activityLog.GetRange(start, _activityLog.Count - start);

            string log = "Here is a summary of recent actions:\n\n";
            for (int i = 0; i < recent.Count; i++)
                log += $"{i + 1}. {recent[i]}\n";

            AppendBotMessage(log.TrimEnd());
            LogActivity("Activity log viewed.");
        }

        private string TruncateForLog(string text)
            => text.Length > 40 ? text.Substring(0, 40) + "..." : text;

        // ──────────────────────────────────────────
        //  Message bubble builders
        // ──────────────────────────────────────────
        private void AppendUserMessage(string text)
        {
            var container = new Grid { Margin = new Thickness(0, 6, 0, 6) };
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var bubble = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0, 180, 255)),
                CornerRadius = new CornerRadius(16, 4, 16, 16),
                Padding = new Thickness(14, 10, 14, 10),
                MaxWidth = 520,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(0, 180, 255),
                    BlurRadius = 12,
                    ShadowDepth = 0,
                    Opacity = 0.25
                }
            };

            var label = new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(10, 14, 26)),
                TextWrapping = TextWrapping.Wrap
            };

            bubble.Child = label;
            Grid.SetColumn(bubble, 1);
            container.Children.Add(bubble);

            AnimateFadeIn(container);
            ChatPanel.Children.Add(container);
            ChatScroller.ScrollToBottom();
        }

        private void AppendBotMessage(string text)
        {
            var container = new Grid { Margin = new Thickness(0, 6, 0, 6) };
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var avatar = new Border
            {
                Width = 32,
                Height = 32,
                Background = new SolidColorBrush(Color.FromRgb(0, 255, 159)),
                CornerRadius = new CornerRadius(16),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(0, 255, 159),
                    BlurRadius = 10,
                    ShadowDepth = 0,
                    Opacity = 0.4
                }
            };
            avatar.Child = new TextBlock
            {
                Text = "CB",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(10, 14, 26)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(avatar, 0);

            var bubble = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 22, 38)),
                CornerRadius = new CornerRadius(4, 16, 16, 16),
                Padding = new Thickness(14, 10, 14, 10),
                MaxWidth = 560,
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 58, 95)),
                BorderThickness = new Thickness(1)
            };

            var label = new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(232, 240, 255)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 22
            };

            bubble.Child = label;
            Grid.SetColumn(bubble, 1);

            container.Children.Add(avatar);
            container.Children.Add(bubble);

            AnimateFadeIn(container);
            ChatPanel.Children.Add(container);
            ChatScroller.ScrollToBottom();
        }

        private UIElement AppendTypingIndicator()
        {
            var container = new Grid { Margin = new Thickness(0, 6, 0, 6) };
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            container.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var avatar = new Border
            {
                Width = 32,
                Height = 32,
                Background = new SolidColorBrush(Color.FromRgb(0, 255, 159)),
                CornerRadius = new CornerRadius(16),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            avatar.Child = new TextBlock
            {
                Text = "CB",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(10, 14, 26)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(avatar, 0);

            var bubble = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 22, 38)),
                CornerRadius = new CornerRadius(4, 16, 16, 16),
                Padding = new Thickness(16, 12, 16, 12),
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 58, 95)),
                BorderThickness = new Thickness(1),
                Width = 80
            };

            var dotsPanel = new StackPanel { Orientation = Orientation.Horizontal };
            for (int i = 0; i < 3; i++)
            {
                var dot = new Ellipse
                {
                    Width = 7,
                    Height = 7,
                    Fill = new SolidColorBrush(Color.FromRgb(0, 255, 159)),
                    Margin = new Thickness(i == 0 ? 0 : 5, 0, 0, 0)
                };
                AnimateDot(dot, i * 200);
                dotsPanel.Children.Add(dot);
            }

            bubble.Child = dotsPanel;
            Grid.SetColumn(bubble, 1);

            container.Children.Add(avatar);
            container.Children.Add(bubble);

            ChatPanel.Children.Add(container);
            ChatScroller.ScrollToBottom();

            return container;
        }

        // ──────────────────────────────────────────
        //  Animations
        // ──────────────────────────────────────────
        private void AnimateFadeIn(UIElement element)
        {
            element.Opacity = 0;
            var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
            element.BeginAnimation(OpacityProperty, anim);
        }

        private void AnimateDot(UIElement dot, int delayMs)
        {
            var anim = new DoubleAnimation(0.2, 1, TimeSpan.FromMilliseconds(500))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromMilliseconds(delayMs)
            };
            dot.BeginAnimation(OpacityProperty, anim);
        }

        // ──────────────────────────────────────────
        //  Memory panel update
        // ──────────────────────────────────────────
        private void UpdateMemoryPanel()
        {
            var mem = _engine.Memory;
            MemoryName.Text = $"Name: {(string.IsNullOrEmpty(mem.Name) ? "—" : mem.Name)}";
            MemoryTopic.Text = $"Interest: {(string.IsNullOrEmpty(mem.FavouriteTopic) ? "—" : mem.FavouriteTopic)}";
            MemoryCount.Text = $"Messages: {mem.MessageCount}";

            if (!string.IsNullOrEmpty(mem.Name))
            {
                UserLabel.Text = mem.Name;
                UserAvatar.Text = mem.Name[0].ToString().ToUpper();
            }
        }

        // ──────────────────────────────────────────
        //  Sentiment panel update
        // ──────────────────────────────────────────
        private void UpdateSentimentPanel()
        {
            var (label, emoji, desc, barFraction, colorHex) = _engine.GetSentimentDisplay();

            SentimentLabel.Text = label;
            SentimentEmoji.Text = emoji;
            SentimentDesc.Text = desc;

            var color = (Color)ColorConverter.ConvertFromString(colorHex);
            var brush = new SolidColorBrush(color);

            SentimentLabel.Foreground = brush;

            double targetWidth = ChatScroller.ActualWidth * barFraction * 0.6;
            var widthAnim = new DoubleAnimation(SentimentBar.Width, Math.Max(20, targetWidth),
                TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new QuadraticEase()
            };
            SentimentBar.BeginAnimation(WidthProperty, widthAnim);
            SentimentBar.Background = brush;
            SentimentBarLabel.Text = label.ToUpper();
            SentimentBarLabel.Foreground = brush;
        }
    }
}