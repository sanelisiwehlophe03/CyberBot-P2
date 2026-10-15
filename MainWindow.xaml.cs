using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CyberBot.Models;
using CyberBot.Services;
using CyberBot.Utils;

namespace CyberBot
{
    /// <summary>
    /// The chat window. It contains only presentation logic (bubbles, typing effect,
    /// side panel). All conversation logic lives in the Chatbot class.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Chatbot _bot = new();
        private bool _awaitingName = true;
        private bool _busy;

        public MainWindow()
        {
            InitializeComponent();

            AsciiLogo.Text = AsciiArt.Logo;   // ASCII art from Part 1, now shown in the GUI header
            BuildTopicButtons();

            Loaded += async (_, _) => await StartConversationAsync();
        }

        // =========================================================
        // STARTUP
        // =========================================================

        /// <summary>Voice greeting first, then the text greeting, then ask for the name.</summary>
        private async Task StartConversationAsync()
        {
            SetBusy(true);

            bool played = AudioPlayer.PlayGreeting();
            StatusText.Text = played
                ? "Voice greeting played"
                : "Voice greeting file (greeting.wav) not found - continuing without audio";
            StatusText.Foreground = (Brush)FindResource(played ? "GreenBrush" : "MutedBrush");

            await AddBotMessageAsync("Hello! Welcome to CyberBot. I'm here to help you become safer online.");
            await AddBotMessageAsync("First things first - what's your name?");

            SetBusy(false);
        }

        /// <summary>Creates one button per topic in the side panel.</summary>
        private void BuildTopicButtons()
        {
            foreach (Topic topic in _bot.Topics)
            {
                string displayName = topic.DisplayName;

                var button = new Button
                {
                    Content = char.ToUpper(displayName[0]) + displayName[1..],
                    Style = (Style)FindResource("PillButton"),
                    Margin = new Thickness(0, 0, 0, 6)
                };

                button.Click += async (_, _) => await SubmitAsync($"Tell me about {displayName}");
                TopicList.Children.Add(button);
            }
        }

        // =========================================================
        // EVENT HANDLERS
        // =========================================================

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SubmitAsync(InputBox.Text);
        }

        private async void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await SubmitAsync(InputBox.Text);
            }
        }

        private async void QuickAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: string message })
            {
                await SubmitAsync(message);
            }
        }

        // =========================================================
        // CONVERSATION FLOW
        // =========================================================

        /// <summary>Shows the user's message, gets the bot's reply and displays it.</summary>
        private async Task SubmitAsync(string? text)
        {
            if (_busy)
            {
                return;
            }

            text = text?.Trim() ?? string.Empty;
            SetBusy(true);

            try
            {
                InputBox.Clear();

                if (text.Length > 0)
                {
                    AddUserMessage(text);
                }

                await ShowTypingIndicatorAsync();

                if (_awaitingName)
                {
                    bool accepted = _bot.TryRegisterUser(text, out string nameReply);
                    await AddBotMessageAsync(nameReply);

                    if (accepted)
                    {
                        _awaitingName = false;
                        await AddBotMessageAsync(_bot.GetHelpText());
                    }
                }
                else
                {
                    string reply = _bot.GetReply(text);
                    await AddBotMessageAsync(reply);

                    if (_bot.IsFinished)
                    {
                        UpdateSidePanel();
                        InputBox.IsEnabled = false;
                        SendButton.IsEnabled = false;
                        QuickPanel.IsEnabled = false;
                        TopicList.IsEnabled = false;

                        await Task.Delay(2200);
                        Close();
                        return;
                    }
                }

                UpdateSidePanel();
            }
            catch (Exception)
            {
                // The GUI must never crash because of a bad message.
                await AddBotMessageAsync("Sorry, something went wrong. Please try again.");
            }
            finally
            {
                if (!_bot.IsFinished)
                {
                    SetBusy(false);
                }
            }
        }

        // =========================================================
        // UI HELPERS
        // =========================================================

        /// <summary>Disables input while the bot is "typing" so messages can't overlap.</summary>
        private void SetBusy(bool busy)
        {
            _busy = busy;
            InputBox.IsEnabled = !busy;
            SendButton.IsEnabled = !busy;

            bool chatReady = !busy && !_awaitingName;
            QuickPanel.IsEnabled = chatReady;
            TopicList.IsEnabled = chatReady;

            if (!busy)
            {
                InputBox.Focus();
            }
        }

        private async Task ShowTypingIndicatorAsync()
        {
            TypingText.Visibility = Visibility.Visible;
            await Task.Delay(400);
            TypingText.Visibility = Visibility.Collapsed;
        }

        /// <summary>Refreshes the "What I remember" card.</summary>
        private void UpdateSidePanel()
        {
            User? user = _bot.User;

            NameText.Text = user?.Name ?? "-";
            InterestText.Text = _bot.GetFavouriteTopicName() ?? "Not set yet";
            TopicsText.Text = user is { TopicsDiscussed.Count: > 0 }
                ? string.Join(", ", user.TopicsDiscussed)
                : "None yet";
            MoodText.Text = SentimentAnalyser.Describe(user?.LastSentiment ?? Sentiment.Neutral);
        }

        private void AddUserMessage(string text)
        {
            (Border bubble, TextBlock body) = CreateBubble("You", isUser: true);
            body.Text = text;
            ChatPanel.Children.Add(bubble);
            ChatScroll.ScrollToEnd();
        }

        /// <summary>Adds a bot bubble and "types" the message into it (typing effect).</summary>
        private async Task AddBotMessageAsync(string text)
        {
            if (text.Length == 0)
            {
                return;
            }

            (Border bubble, TextBlock body) = CreateBubble("CyberBot", isUser: false);
            ChatPanel.Children.Add(bubble);

            // Reveal the text in chunks so long replies still finish in about two seconds.
            int steps = Math.Min(text.Length, 110);
            int chunk = (int)Math.Ceiling(text.Length / (double)steps);
            var typed = new StringBuilder();

            for (int i = 0; i < text.Length; i += chunk)
            {
                typed.Append(text, i, Math.Min(chunk, text.Length - i));
                body.Text = typed.ToString();
                ChatScroll.ScrollToEnd();
                await Task.Delay(15);
            }
        }

        /// <summary>Builds a rounded chat bubble (left = bot, right = user).</summary>
        private (Border Bubble, TextBlock Body) CreateBubble(string speaker, bool isUser)
        {
            var header = new TextBlock
            {
                Text = $"{speaker}  -  {DateTime.Now:HH:mm}",
                FontSize = 11,
                Foreground = (Brush)FindResource("MutedBrush"),
                Margin = new Thickness(0, 0, 0, 4)
            };

            var body = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                LineHeight = 21,
                Foreground = (Brush)FindResource("TextBrush")
            };

            var content = new StackPanel();
            content.Children.Add(header);
            content.Children.Add(body);

            var bubble = new Border
            {
                Child = content,
                Background = (Brush)FindResource(isUser ? "UserBubbleBrush" : "BotBubbleBrush"),
                BorderBrush = (Brush)FindResource(isUser ? "GreenBrush" : "AccentBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 10),
                MaxWidth = 640,
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            return (bubble, body);
        }
    }
}
