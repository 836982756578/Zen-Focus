// Zen Focus - Masaüstü Minimalist Pomodoro ve Odaklanma Sayacı
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Diagnostics;
using System.Media;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Orientation = System.Windows.Controls.Orientation;

namespace ZenFocus
{
    public class MainWindow : Window
    {
        private FocusConfig _config;
        private SessionMode _currentMode = SessionMode.Focus;
        private int _remainingSeconds;
        private bool _isRunning = false;
        private DispatcherTimer _timer;

        private Border _outerBorder;
        private TextBlock _txtTime;
        private TextBlock _txtModeBadge;
        private TextBlock _txtStats;
        private Button _btnPlay;
        private Button _btnPin;
        private StackPanel _fullContentStack;
        private StackPanel _miniStack;
        private bool _isMiniMode = false;
        private NotifyIcon _notifyIcon;

        public MainWindow()
        {
            _config = FocusConfig.Load();
            _remainingSeconds = _config.FocusMinutes * 60;

            InitializeComponent();
            InitTimer();
            InitNotifyIcon();
            UpdateDisplay();
        }

        private void InitializeComponent()
        {
            Title = "Zen Focus";
            Width = 270;
            Height = 320;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;

            _outerBorder = new Border
            {
                Background = new SolidColorBrush(ColorFromHex("#121217")),
                BorderBrush = new SolidColorBrush(ColorFromHex("#EF4444")),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(14, 10, 14, 10)
            };
            _outerBorder.MouseLeftButtonDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };

            var mainGrid = new Grid();

            // --- FULL CONTENT ---
            _fullContentStack = new StackPanel();

            // 1. Top Bar
            var topBar = new Grid();
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var iconText = new TextBlock { Text = "☯️", FontSize = 16, Margin = new Thickness(0, 0, 6, 0) };
            var titleText = new TextBlock { Text = "Zen Focus", Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 13 };
            titleStack.Children.Add(iconText);
            titleStack.Children.Add(titleText);

            var topActions = new StackPanel { Orientation = Orientation.Horizontal };
            var btnMini = CreateSmallButton("🗗", "Mini Moda Geç", (s, e) => ToggleMiniMode());
            _btnPin = CreateSmallButton("📌", "Her Zaman Üstte", (s, e) => TogglePin());
            var btnClose = CreateSmallButton("✕", "Kapat", (s, e) => Close());
            topActions.Children.Add(btnMini);
            topActions.Children.Add(_btnPin);
            topActions.Children.Add(btnClose);

            Grid.SetColumn(titleStack, 0);
            Grid.SetColumn(topActions, 2);
            topBar.Children.Add(titleStack);
            topBar.Children.Add(topActions);
            _fullContentStack.Children.Add(topBar);

            // 2. Mode Selector Pills
            var modeGrid = new Grid { Margin = new Thickness(0, 12, 0, 12) };
            modeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            modeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Pixel) });
            modeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            modeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Pixel) });
            modeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var btnFocus = CreateModeTab("Odak (25m)", 0);
            var btnShort = CreateModeTab("Mola (5m)", 2);
            var btnLong = CreateModeTab("Uzun (15m)", 4);

            btnFocus.Click += (s, e) => SetMode(SessionMode.Focus);
            btnShort.Click += (s, e) => SetMode(SessionMode.ShortBreak);
            btnLong.Click += (s, e) => SetMode(SessionMode.LongBreak);

            modeGrid.Children.Add(btnFocus);
            modeGrid.Children.Add(btnShort);
            modeGrid.Children.Add(btnLong);
            _fullContentStack.Children.Add(modeGrid);

            // 3. Central Time Card
            var timeCard = new Border
            {
                Background = new SolidColorBrush(ColorFromHex("#191924")),
                BorderBrush = new SolidColorBrush(ColorFromHex("#2B2B3E")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 14, 12, 14),
                Margin = new Thickness(0, 0, 0, 12)
            };
            var timeStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

            _txtModeBadge = new TextBlock
            {
                Text = "🎯 ODAKLANMA",
                Foreground = new SolidColorBrush(ColorFromHex("#EF4444")),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };

            _txtTime = new TextBlock
            {
                Text = "25:00",
                Foreground = Brushes.White,
                FontSize = 44,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas"),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            timeStack.Children.Add(_txtModeBadge);
            timeStack.Children.Add(_txtTime);
            timeCard.Child = timeStack;
            _fullContentStack.Children.Add(timeCard);

            // 4. Play / Pause / Skip Controls
            var ctrlGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            ctrlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            ctrlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel) });
            ctrlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _btnPlay = new Button
            {
                Content = "▶️ Başlat",
                Height = 36,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(ColorFromHex("#EF4444")),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            _btnPlay.Click += (s, e) => TogglePlay();

            var btnSkip = new Button
            {
                Content = "⏭️ Sıradaki",
                Height = 36,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Foreground = new SolidColorBrush(ColorFromHex("#A0A0BA")),
                Background = new SolidColorBrush(ColorFromHex("#222230")),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnSkip.Click += (s, e) => SkipSession();

            Grid.SetColumn(_btnPlay, 0);
            Grid.SetColumn(btnSkip, 2);
            ctrlGrid.Children.Add(_btnPlay);
            ctrlGrid.Children.Add(btnSkip);
            _fullContentStack.Children.Add(ctrlGrid);

            // 5. Daily Stats & Footer
            _txtStats = new TextBlock
            {
                Text = "Bugün 0 odaklanma seansı",
                Foreground = new SolidColorBrush(ColorFromHex("#8E8EA8")),
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };
            _fullContentStack.Children.Add(_txtStats);

            var authorText = new TextBlock
            {
                Text = "Serkan ŞENTÜRK © 2026",
                Foreground = new SolidColorBrush(ColorFromHex("#5E5E72")),
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            authorText.MouseLeftButtonDown += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://github.com/836982756578") { UseShellExecute = true }); } catch { }
            };
            _fullContentStack.Children.Add(authorText);

            // --- MINI STACK (Collapsed by default) ---
            _miniStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Visibility = Visibility.Collapsed
            };
            mainGrid.Children.Add(_fullContentStack);
            mainGrid.Children.Add(_miniStack);

            _outerBorder.Child = mainGrid;
            Content = _outerBorder;
        }

        private void InitTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) =>
            {
                if (_remainingSeconds > 0)
                {
                    _remainingSeconds--;
                    UpdateDisplay();
                }
                else
                {
                    OnTimerCompleted();
                }
            };
        }

        private void TogglePlay()
        {
            _isRunning = !_isRunning;
            if (_isRunning)
            {
                _timer.Start();
                _btnPlay.Content = "⏸️ Duraklat";
            }
            else
            {
                _timer.Stop();
                _btnPlay.Content = "▶️ Devam Et";
            }
        }

        private void SetMode(SessionMode mode)
        {
            _currentMode = mode;
            _isRunning = false;
            _timer.Stop();
            _btnPlay.Content = "▶️ Başlat";

            string accentHex = "#EF4444";

            switch (mode)
            {
                case SessionMode.Focus:
                    _remainingSeconds = _config.FocusMinutes * 60;
                    _txtModeBadge.Text = "🎯 ODAKLANMA";
                    accentHex = "#EF4444";
                    break;
                case SessionMode.ShortBreak:
                    _remainingSeconds = _config.ShortBreakMinutes * 60;
                    _txtModeBadge.Text = "☕ KISA MOLA";
                    accentHex = "#10B981";
                    break;
                case SessionMode.LongBreak:
                    _remainingSeconds = _config.LongBreakMinutes * 60;
                    _txtModeBadge.Text = "🌴 UZUN MOLA";
                    accentHex = "#0EA5E9";
                    break;
            }

            _txtModeBadge.Foreground = new SolidColorBrush(ColorFromHex(accentHex));
            _outerBorder.BorderBrush = new SolidColorBrush(ColorFromHex(accentHex));
            _btnPlay.Background = new SolidColorBrush(ColorFromHex(accentHex));

            UpdateDisplay();
        }

        private void SkipSession()
        {
            if (_currentMode == SessionMode.Focus)
            {
                SetMode(SessionMode.ShortBreak);
            }
            else
            {
                SetMode(SessionMode.Focus);
            }
        }

        private void OnTimerCompleted()
        {
            _timer.Stop();
            _isRunning = false;
            _btnPlay.Content = "▶️ Başlat";

            try
            {
                SystemSounds.Asterisk.Play();
            }
            catch { }

            if (_currentMode == SessionMode.Focus)
            {
                _config.CompletedToday++;
                _config.Save();

                // If completed 4 sessions, offer long break
                if (_config.CompletedToday % 4 == 0)
                {
                    SetMode(SessionMode.LongBreak);
                }
                else
                {
                    SetMode(SessionMode.ShortBreak);
                }
            }
            else
            {
                SetMode(SessionMode.Focus);
            }

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            int m = _remainingSeconds / 60;
            int s = _remainingSeconds % 60;
            string str = string.Format("{0:D2}:{1:D2}", m, s);
            _txtTime.Text = str;

            _txtStats.Text = string.Format("Bugün {0} odaklanma seansı tamamlandı", _config.CompletedToday);

            if (_notifyIcon != null)
            {
                _notifyIcon.Text = string.Format("Zen Focus: {0} ({1})", str, _txtModeBadge.Text);
            }
        }

        private void ToggleMiniMode()
        {
            _isMiniMode = !_isMiniMode;
            if (_isMiniMode)
            {
                _fullContentStack.Visibility = Visibility.Collapsed;
                _miniStack.Visibility = Visibility.Visible;
                _miniStack.Children.Clear();

                var miniTime = new TextBlock
                {
                    Text = _txtTime.Text,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 16,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                var miniPlay = CreateSmallButton(_isRunning ? "⏸️" : "▶️", "Başlat/Duraklat", (s, e) =>
                {
                    TogglePlay();
                    ((Button)s).Content = _isRunning ? "⏸️" : "▶️";
                });
                var miniExpand = CreateSmallButton("🗖", "Büyüt", (s, e) => ToggleMiniMode());

                _miniStack.Children.Add(miniTime);
                _miniStack.Children.Add(miniPlay);
                _miniStack.Children.Add(miniExpand);

                Width = 140;
                Height = 44;
            }
            else
            {
                _miniStack.Visibility = Visibility.Collapsed;
                _fullContentStack.Visibility = Visibility.Visible;
                Width = 270;
                Height = 320;
            }
        }

        private void TogglePin()
        {
            Topmost = !Topmost;
            _btnPin.Content = Topmost ? "📌" : "📍";
        }

        private Button CreateModeTab(string label, int col)
        {
            var btn = new Button
            {
                Content = label,
                Height = 26,
                FontSize = 10,
                Foreground = new SolidColorBrush(ColorFromHex("#A0A0BA")),
                Background = new SolidColorBrush(ColorFromHex("#1E1E2B")),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            Grid.SetColumn(btn, col);
            return btn;
        }

        private Button CreateSmallButton(string text, string tip, RoutedEventHandler onClick)
        {
            var btn = new Button
            {
                Content = text,
                Width = 22,
                Height = 22,
                Foreground = new SolidColorBrush(ColorFromHex("#A0A0BA")),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 11,
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = tip,
                Margin = new Thickness(2, 0, 0, 0)
            };
            btn.Click += onClick;
            return btn;
        }

        private void InitNotifyIcon()
        {
            try
            {
                _notifyIcon = new NotifyIcon();
                string exePath = Assembly.GetExecutingAssembly().Location;
                _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath) ?? System.Drawing.SystemIcons.Application;
                _notifyIcon.Text = "Zen Focus - Odaklanma Sayacı";
                _notifyIcon.Visible = true;

                var menu = new ContextMenuStrip();
                menu.Items.Add("▶️ / ⏸️ Başlat / Duraklat", null, (s, e) => TogglePlay());
                menu.Items.Add("🎯 Odak Modu (25m)", null, (s, e) => SetMode(SessionMode.Focus));
                menu.Items.Add("☕ Mola Modu (5m)", null, (s, e) => SetMode(SessionMode.ShortBreak));
                menu.Items.Add("-");
                menu.Items.Add("🌐 Geliştirici: Serkan ŞENTÜRK", null, (s, e) =>
                {
                    try { Process.Start(new ProcessStartInfo("https://github.com/836982756578") { UseShellExecute = true }); } catch { }
                });
                menu.Items.Add("✕ Çıkış", null, (s, e) =>
                {
                    _notifyIcon.Visible = false;
                    Application.Current.Shutdown();
                });

                _notifyIcon.ContextMenuStrip = menu;
                _notifyIcon.DoubleClick += (s, e) =>
                {
                    Show();
                    WindowState = WindowState.Normal;
                    Activate();
                };
            }
            catch { }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            base.OnClosed(e);
        }

        private static Color ColorFromHex(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
    }
}
