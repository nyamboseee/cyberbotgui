using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Data.Sqlite;

namespace CyberBotWPF
{
    // ─────────────────────────────────────────────
    //  Task model
    // ─────────────────────────────────────────────
    public class CyberTask
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Reminder { get; set; }
        public string Timeframe { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ─────────────────────────────────────────────
    //  Task Window
    // ─────────────────────────────────────────────
    public partial class TaskWindow : Window
    {
        private readonly List<CyberTask> _tasks = new();
        private readonly string _dbPath;
        private readonly string _connectionString;

        // Expose for activity log
        public string LastTaskTitle { get; private set; } = "";

        public TaskWindow()
        {
            InitializeComponent();

            // Database stored in app folder
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cyberbot_tasks.db");
            _connectionString = $"Data Source={_dbPath}";

            InitialiseDatabase();
            LoadTasksFromDb();
            RefreshTaskList();
        }

        // ──────────────────────────────────────────
        //  Database setup
        // ──────────────────────────────────────────
        private void InitialiseDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string createTable = @"
                CREATE TABLE IF NOT EXISTS Tasks (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title       TEXT    NOT NULL,
                    Description TEXT,
                    Reminder    TEXT,
                    Timeframe   TEXT,
                    IsCompleted INTEGER DEFAULT 0,
                    CreatedAt   TEXT
                );";

            using var cmd = new SqliteCommand(createTable, connection);
            cmd.ExecuteNonQuery();
        }

        // ──────────────────────────────────────────
        //  Load tasks from DB
        // ──────────────────────────────────────────
        private void LoadTasksFromDb()
        {
            _tasks.Clear();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string query = "SELECT Id, Title, Description, Reminder, Timeframe, IsCompleted, CreatedAt FROM Tasks ORDER BY Id ASC;";
            using var cmd = new SqliteCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                _tasks.Add(new CyberTask
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Reminder = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Timeframe = reader.IsDBNull(4) ? "No reminder" : reader.GetString(4),
                    IsCompleted = reader.GetInt32(5) == 1,
                    CreatedAt = DateTime.Parse(reader.GetString(6))
                });
            }
        }

        // ──────────────────────────────────────────
        //  Add task
        // ──────────────────────────────────────────
        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            string title = TaskTitleInput.Text.Trim();
            string desc = TaskDescInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                SetStatus("Please enter a task title.", isError: true);
                return;
            }

            string reminder = TaskReminderInput.Text.Trim();
            string timeframe = (ReminderTimeframe.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "No reminder";
            string createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Insert into DB
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string insert = @"
                INSERT INTO Tasks (Title, Description, Reminder, Timeframe, IsCompleted, CreatedAt)
                VALUES (@title, @desc, @reminder, @timeframe, 0, @createdAt);";

            using var cmd = new SqliteCommand(insert, connection);
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@desc", string.IsNullOrWhiteSpace(desc) ? "No description provided." : desc);
            cmd.Parameters.AddWithValue("@reminder", reminder);
            cmd.Parameters.AddWithValue("@timeframe", timeframe);
            cmd.Parameters.AddWithValue("@createdAt", createdAt);
            cmd.ExecuteNonQuery();

            LastTaskTitle = title;

            // Clear inputs
            TaskTitleInput.Clear();
            TaskDescInput.Clear();
            TaskReminderInput.Clear();
            ReminderTimeframe.SelectedIndex = 0;

            LoadTasksFromDb();
            RefreshTaskList();

            string statusMsg = timeframe == "No reminder"
                ? $"Task added: '{title}'."
                : $"Task added: '{title}'. Reminder set for {timeframe}.";
            SetStatus(statusMsg, isError: false);
        }

        // ──────────────────────────────────────────
        //  Mark complete / undo
        // ──────────────────────────────────────────
        private void CompleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var task = _tasks.Find(t => t.Id == id);
                if (task == null) return;

                task.IsCompleted = !task.IsCompleted;

                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                string update = "UPDATE Tasks SET IsCompleted = @val WHERE Id = @id;";
                using var cmd = new SqliteCommand(update, connection);
                cmd.Parameters.AddWithValue("@val", task.IsCompleted ? 1 : 0);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();

                RefreshTaskList();
                SetStatus(task.IsCompleted
                    ? $"Task '{task.Title}' marked as completed."
                    : $"Task '{task.Title}' marked as pending.", isError: false);
            }
        }

        // ──────────────────────────────────────────
        //  Delete task
        // ──────────────────────────────────────────
        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var task = _tasks.Find(t => t.Id == id);
                if (task == null) return;

                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                string delete = "DELETE FROM Tasks WHERE Id = @id;";
                using var cmd = new SqliteCommand(delete, connection);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();

                LoadTasksFromDb();
                RefreshTaskList();
                SetStatus($"Task '{task.Title}' deleted.", isError: false);
            }
        }

        // ──────────────────────────────────────────
        //  Refresh task list UI
        // ──────────────────────────────────────────
        private void RefreshTaskList()
        {
            TaskListPanel.Children.Clear();
            TaskCountLabel.Text = _tasks.Count.ToString();
            EmptyLabel.Visibility = _tasks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            foreach (var task in _tasks)
                TaskListPanel.Children.Add(BuildTaskCard(task));
        }

        // ──────────────────────────────────────────
        //  Build task card
        // ──────────────────────────────────────────
        private Border BuildTaskCard(CyberTask task)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(20, 28, 46)),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(task.IsCompleted
                                    ? Color.FromRgb(0, 60, 40)
                                    : Color.FromRgb(30, 58, 95)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(10, 4, 10, 4),
                Padding = new Thickness(14, 10, 14, 10),
                Opacity = task.IsCompleted ? 0.6 : 1.0
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left: task info
            var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
            if (task.IsCompleted)
            {
                titleRow.Children.Add(new TextBlock
                {
                    Text = "[DONE]  ",
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 159)),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
            titleRow.Children.Add(new TextBlock
            {
                Text = task.Title,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(task.IsCompleted
                                    ? Color.FromRgb(90, 106, 138)
                                    : Color.FromRgb(232, 240, 255)),
                TextDecorations = task.IsCompleted ? TextDecorations.Strikethrough : null
            });
            infoStack.Children.Add(titleRow);

            infoStack.Children.Add(new TextBlock
            {
                Text = task.Description,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(90, 106, 138)),
                Margin = new Thickness(0, 3, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });

            if (task.Timeframe != "No reminder")
            {
                infoStack.Children.Add(new TextBlock
                {
                    Text = $"Reminder: {task.Timeframe}" + (string.IsNullOrEmpty(task.Reminder) ? "" : $" — {task.Reminder}"),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 180, 255)),
                    Margin = new Thickness(0, 4, 0, 0)
                });
            }

            infoStack.Children.Add(new TextBlock
            {
                Text = $"Added: {task.CreatedAt:dd MMM yyyy HH:mm}",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(60, 76, 100)),
                Margin = new Thickness(0, 3, 0, 0)
            });

            Grid.SetColumn(infoStack, 0);

            // Right: action buttons
            var btnStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };

            var completeBtn = new Button
            {
                Content = task.IsCompleted ? "UNDO" : "DONE",
                Style = (Style)FindResource("SuccessBtn"),
                Width = 60,
                Margin = new Thickness(0, 0, 8, 0),
                Tag = task.Id
            };
            completeBtn.Click += CompleteTask_Click;

            var deleteBtn = new Button
            {
                Content = "DELETE",
                Style = (Style)FindResource("DangerBtn"),
                Width = 70,
                Tag = task.Id
            };
            deleteBtn.Click += DeleteTask_Click;

            btnStack.Children.Add(completeBtn);
            btnStack.Children.Add(deleteBtn);
            Grid.SetColumn(btnStack, 1);

            grid.Children.Add(infoStack);
            grid.Children.Add(btnStack);
            card.Child = grid;

            // Fade in
            card.Opacity = 0;
            var anim = new DoubleAnimation(0, task.IsCompleted ? 0.6 : 1.0, TimeSpan.FromMilliseconds(200));
            card.BeginAnimation(OpacityProperty, anim);

            return card;
        }

        // ──────────────────────────────────────────
        //  Status bar
        // ──────────────────────────────────────────
        private void SetStatus(string message, bool isError)
        {
            StatusLabel.Text = message;
            StatusLabel.Foreground = isError
                ? new SolidColorBrush(Color.FromRgb(255, 64, 96))
                : new SolidColorBrush(Color.FromRgb(0, 255, 159));
        }
    }
}