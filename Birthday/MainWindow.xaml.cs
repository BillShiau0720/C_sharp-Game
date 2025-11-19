using Birthday.Services;
using Birthday.Story;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using TSRC_Overview;

namespace Birthday
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string LoadSetini = @"C:\Users\Public\Documents\Bill_Games\Setting\Config.ini";

        private bool _isFullscreen = false;
        private WindowStyle _prevStyle;
        private ResizeMode _prevResize;
        private Rect _prevBounds;

        private MediaPlayer _mediaPlayer;
        private StoryService? _storyService;
        private bool _isStoryRunning;
        private DispatcherTimer? _typewriterTimer;
        private int _typewriterIndex;
        private string _currentFullDialogue = string.Empty;
        private bool _isTypewriting;
        private bool _isStoryMenuOpen;
        public MainWindow()
        {
            InitializeComponent();

            this.KeyDown += MainWindow_KeyDown;

            InitializeStorySystem();

            //訂閱開啟事件
            Loaded += (_, __) =>
            {
                Trace.WriteLine("Checking source ...");
                var bgmPath = AppContext.BaseDirectory + @"Assets\Audio\BGM\MainMenu.mp3";
                BgmService.Instance.Play(bgmPath, loop: true, targetVolume: 1f, fadeSeconds: 0.8);  //啟動背景音樂
                Trace.WriteLine("[BGM] Exists = " + System.IO.File.Exists(bgmPath));
                Trace.WriteLine("Checking source ok.");

                StartBgVideo();     //啟動背景圖片
                EnterFullscreen();      // 啟動就全螢幕
                
            };

            //訂閱關閉事件
            Closed += (_, __) =>
            {
                BgmService.Instance.Stop(0.4);
                BgmService.Instance.Dispose();
            };

            Trace.WriteLine("Main page check ok.");
        }

        // 播放
        private void StartBgVideo()
        {
            _mediaPlayer = new MediaPlayer();

            string path = AppContext.BaseDirectory + @"Assets\Videos\MainPageVideo.mp4";
            if (!File.Exists(path))
            {
                MessageBox.Show("影片不存在: " + path);
                return;
            }

            _mediaPlayer.Open(new Uri(path));
            _mediaPlayer.MediaEnded += (s, ev) =>
            {
                _mediaPlayer.Position = TimeSpan.Zero; // 迴圈播放
                _mediaPlayer.Play();
            };

            _mediaPlayer.IsMuted = true;
            _mediaPlayer.Play();

            // 建立 VideoDrawing
            var videoDrawing = new VideoDrawing
            {
                Rect = new Rect(0, 0, VideoArea.Width, VideoArea.Height),
                Player = _mediaPlayer
            };

            // 用 DrawingBrush 包起來
            var brush = new DrawingBrush(videoDrawing)
            {
                Stretch = Stretch.UniformToFill
            };

            // 設定到 Border 背景
            VideoArea.Background = brush;
        } 


        // 視窗生命週期掛上釋放點
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Close();
            }
            catch { }
            base.OnClosed(e);
        }

        //監聽鍵盤按鍵
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                ToggleFullscreen();
            }
            else if (e.Key == Key.Escape && SettingsPopup.Visibility == Visibility.Visible)
            {
                SettingsPopup.Visibility = Visibility.Collapsed;
            }
            else if (e.Key == Key.Escape && LoadPopup.Visibility == Visibility.Visible)
            {
                LoadPopup.Visibility = Visibility.Collapsed;
            }
            else if (e.Key == Key.Escape && GameLayer.Visibility == Visibility.Visible)
            {
                ReturnToMainMenu();
            }
            else if (e.Key == Key.Escape)
            {
                Close();
            }
        }
        private void EnterFullscreen()
        {
            if (_isFullscreen) return;

            _prevStyle = WindowStyle;
            _prevResize = ResizeMode;
            _prevBounds = new Rect(Left, Top, Width, Height);

            // 取得目前視窗所屬螢幕的「完整」矩形（不是 WorkArea）
            var hwnd = new WindowInteropHelper(this).Handle;
            IntPtr hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            var mi = new MONITORINFO();
            mi.cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO));
            if (!GetMonitorInfo(hMonitor, ref mi))
            {
                // 失敗就退而求其次：用虛擬桌面大小（跨所有螢幕）
                Left = SystemParameters.VirtualScreenLeft;
                Top = SystemParameters.VirtualScreenTop;
                Width = SystemParameters.VirtualScreenWidth;
                Height = SystemParameters.VirtualScreenHeight;
            }
            else
            {
                // rcMonitor = 整個螢幕（含工作列），可完全覆蓋
                Left = mi.rcMonitor.left;
                Top = mi.rcMonitor.top;
                Width = mi.rcMonitor.right - mi.rcMonitor.left;
                Height = mi.rcMonitor.bottom - mi.rcMonitor.top;
            }

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            WindowState = WindowState.Normal;   // 先 Normal 才能正確套寬高

            _isFullscreen = true;
            Trace.WriteLine("Enter full screen (pure Win32).");
        }

        private void ExitFullscreen()
        {
            if (!_isFullscreen) return;

            Topmost = false;
            WindowStyle = _prevStyle;
            ResizeMode = _prevResize;

            WindowState = WindowState.Normal;
            Left = _prevBounds.Left;
            Top = _prevBounds.Top;
            Width = _prevBounds.Width;
            Height = _prevBounds.Height;

            _isFullscreen = false;
            Trace.WriteLine("Exit full screen.");
        }

        private void ToggleFullscreen()
        {
            if (_isFullscreen) ExitFullscreen();
            else EnterFullscreen();
        }

        // ===== Win32 =====
        private const int MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;  // 整個螢幕（含工作列）
            public RECT rcWork;     // 工作區（不含工作列）
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left, top, right, bottom;
        }
        private void CloseSettings_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("Setting window closed.");
            SettingsPopup.Visibility = Visibility.Collapsed;
        }

        // 當滑桿改變數值時
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VolumeLabel != null)
                VolumeLabel.Text = ((int)e.NewValue).ToString();

            BgmService.Instance.SetVolume(e.NewValue / 100.0);
        }
        private void BtnLoadClose_Click(object sender, RoutedEventArgs e)
        {
            LoadPopup.Visibility = Visibility.Collapsed;
        }

        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("Start game clicked.");
            StartStory();
        }

        private void LoadGame_Click(object sender, RoutedEventArgs e)
        {
            ShowSaveManager();
        }

        private void ShowSaveManager()
        {
            Trace.WriteLine("Loading save...");
            LoadPopup.Visibility = Visibility.Visible;

            IniFiles loadSet = new IniFiles(LoadSetini);
            string Save1_LastSaveTimeF = loadSet.IniReadValue("Save1", "LastSaveTime");
            string Save2_LastSaveTimeF = loadSet.IniReadValue("Save2", "LastSaveTime");
            string Save3_LastSaveTimeF = loadSet.IniReadValue("Save3", "LastSaveTime");

            // Slot 1
            if (string.IsNullOrEmpty(Save1_LastSaveTimeF))
            {
                Save1_Img.Visibility = Visibility.Collapsed;
                Save1_Title.Text = "存檔 1 ( 空 ) ";
                Save1_Chapter.Text = "尚未存檔";
                Save1_LastSaveTime.Text = "";
                Save1_BtnLoad.IsEnabled = false;
                Save1_BtnDelete.IsEnabled = false;
                Save1_BtnLoad.Visibility = Visibility.Collapsed;
                Save1_BtnDelete.Visibility = Visibility.Collapsed;
            }
            else
            {
                string Save1_LeveF = loadSet.IniReadValue("Save1", "Level");
                string Save1_ChapterF = loadSet.IniReadValue("Save1", "Chapter");
                Save1_Img.Visibility = Visibility.Visible;
                Save1_Title.Text = "存檔 1";
                Save1_Chapter.Text = "第" + Save1_ChapterF + "章";
                Save1_LastSaveTime.Text = Save1_LastSaveTimeF;
                Save1_BtnLoad.IsEnabled = true;
                Save1_BtnDelete.IsEnabled = true;
                Save1_BtnLoad.Visibility = Visibility.Visible;
                Save1_BtnDelete.Visibility = Visibility.Visible;
            }

            // Slot 2
            if (string.IsNullOrEmpty(Save2_LastSaveTimeF))
            {
                Save2_Img.Visibility = Visibility.Collapsed;
                Save2_Title.Text = "存檔 2 ( 空 ) ";
                Save2_Chapter.Text = "尚未存檔";
                Save2_LastSaveTime.Text = "";
                Save2_BtnLoad.IsEnabled = false;
                Save2_BtnDelete.IsEnabled = false;
                Save2_BtnLoad.Visibility = Visibility.Collapsed;
                Save2_BtnDelete.Visibility = Visibility.Collapsed;
            }
            else
            {
                string Save2_LeveF = loadSet.IniReadValue("Save2", "Level");
                string Save2_ChapterF = loadSet.IniReadValue("Save2", "Chapter");
                Save2_Img.Visibility = Visibility.Visible;
                Save2_Title.Text = "存檔 2";
                Save2_Chapter.Text = "第" + Save2_ChapterF + "章";
                Save2_LastSaveTime.Text = Save2_LastSaveTimeF;
                Save2_BtnLoad.IsEnabled = true;
                Save2_BtnDelete.IsEnabled = true;
                Save2_BtnLoad.Visibility = Visibility.Visible;
                Save2_BtnDelete.Visibility = Visibility.Visible;
            }

            // Slot 3
            if (string.IsNullOrEmpty(Save3_LastSaveTimeF))
            {
                Save3_Img.Visibility = Visibility.Collapsed;
                Save3_Title.Text = "存檔 3 ( 空 ) ";
                Save3_Chapter.Text = "尚未存檔";
                Save3_LastSaveTime.Text = "";
                Save3_BtnLoad.IsEnabled = false;
                Save3_BtnDelete.IsEnabled = false;
                Save3_BtnLoad.Visibility = Visibility.Collapsed;
                Save3_BtnDelete.Visibility = Visibility.Collapsed;
            }
            else
            {
                string Save3_LeveF = loadSet.IniReadValue("Save3", "Level");
                string Save3_ChapterF = loadSet.IniReadValue("Save3", "Chapter");
                Save3_Img.Visibility = Visibility.Visible;
                Save3_Title.Text = "存檔 3";
                Save3_Chapter.Text = "第" + Save3_ChapterF + "章";
                Save3_LastSaveTime.Text = Save3_LastSaveTimeF;
                Save3_BtnLoad.IsEnabled = true;
                Save3_BtnDelete.IsEnabled = true;
                Save3_BtnLoad.Visibility = Visibility.Visible;
                Save3_BtnDelete.Visibility = Visibility.Visible;
            }

            Trace.WriteLine("Loaded save compelet.");
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("Setting clicked.");
            SettingsPopup.Visibility = Visibility.Visible;

        }

        private void InitializeStorySystem()
        {
            _storyService = StoryService.CreateDemoStory();
            _storyService.StepChanged += StoryService_StepChanged;
            _storyService.StoryCompleted += StoryService_StoryCompleted;
            ResetStoryUi();
        }

        private void StartStory()
        {
            if (_storyService == null)
            {
                InitializeStorySystem();
            }

            SettingsPopup.Visibility = Visibility.Collapsed;
            LoadPopup.Visibility = Visibility.Collapsed;
            ResetStoryUi();
            ShowGameLayer();
            _storyService?.Start();
        }

        private void ShowGameLayer()
        {
            GameLayer.Visibility = Visibility.Visible;
            MainMenuLayer.Visibility = Visibility.Collapsed;
            _isStoryRunning = true;
        }

        private void ReturnToMainMenu()
        {
            GameLayer.Visibility = Visibility.Collapsed;
            MainMenuLayer.Visibility = Visibility.Visible;
            _isStoryRunning = false;
            ResetStoryUi();
        }

        private void StoryMenuToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isStoryMenuOpen)
            {
                CollapseStoryMenu();
            }
            else
            {
                ExpandStoryMenu();
            }

            e.Handled = true;
        }

        private void StoryNextButton_Click(object sender, RoutedEventArgs e)
        {
            if (FinishTypewriterEarly())
            {
                return;
            }

            _storyService?.Continue();
        }

        private void StorySaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (FinishTypewriterEarly())
            {
                return;
            }

            ShowSaveManager();
            CollapseStoryMenu();
        }

        private void StoryBackButton_Click(object sender, RoutedEventArgs e)
        {
            ReturnToMainMenu();
            CollapseStoryMenu();
        }

        private void GameLayer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isStoryMenuOpen)
            {
                return;
            }

            if (StoryMenuContainer == null)
            {
                return;
            }

            if (e.OriginalSource is DependencyObject source && IsDescendantOf(source, StoryMenuContainer))
            {
                return;
            }

            CollapseStoryMenu();
        }

        private void ExpandStoryMenu()
        {
            if (StoryMenuButtonsPanel == null || StoryMenuToggleButton == null)
            {
                return;
            }

            StoryMenuButtonsPanel.Visibility = Visibility.Visible;
            StoryMenuToggleButton.Content = "▲ 劇情選單";
            _isStoryMenuOpen = true;
        }

        private void CollapseStoryMenu()
        {
            if (StoryMenuButtonsPanel == null || StoryMenuToggleButton == null)
            {
                _isStoryMenuOpen = false;
                return;
            }

            StoryMenuButtonsPanel.Visibility = Visibility.Collapsed;
            StoryMenuToggleButton.Content = "☰ 劇情選單";
            _isStoryMenuOpen = false;
        }

        private static bool IsDescendantOf(DependencyObject? source, DependencyObject ancestor)
        {
            DependencyObject? current = source;

            while (current != null)
            {
                if (current == ancestor)
                {
                    return true;
                }

                if (current is Visual || current is System.Windows.Media.Media3D.Visual3D)
                {
                    current = VisualTreeHelper.GetParent(current);
                }
                else
                {
                    current = LogicalTreeHelper.GetParent(current);
                }
            }

            return false;
        }

        private void StoryChoiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (FinishTypewriterEarly())
            {
                return;
            }

            if (sender is Button button && button.Tag is string nextId)
            {
                _storyService?.Choose(nextId);
            }
        }

        private void StoryService_StepChanged(StoryStep step)
        {
            Dispatcher.Invoke(() =>
            {
                ApplyStoryStep(step);
            });
        }

        private void StoryService_StoryCompleted()
        {
            Dispatcher.Invoke(() =>
            {
                StopTypewriterEffect();
                _currentFullDialogue = string.Empty;
                _typewriterIndex = 0;
                _isStoryRunning = false;
                StoryChoicesPanel.Children.Clear();
                StoryNextButton.Visibility = Visibility.Visible;
                StoryNextButton.IsEnabled = false;
                StoryNextButton.Content = "劇情完結";
                StoryStatusText.Text = "Need to add fighting system.";
                StoryStatusText.Visibility = Visibility.Visible;
                StorySpeakerText.Text = "系統";
                StoryDialogueText.Text = "Need to add money system";

                StoryCharacterImage.Source = null;
            });
        }

        private void ApplyStoryStep(StoryStep step)
        {
            StoryTitleText.Text = step.Title ?? string.Empty;
            StorySpeakerText.Text = string.IsNullOrWhiteSpace(step.CharacterName) ? "旁白" : step.CharacterName;
            StartTypewriter(step.Dialogue ?? string.Empty);
            StoryStatusText.Visibility = Visibility.Collapsed;
            StoryStatusText.Text = string.Empty;

            UpdateImageSource(StoryBackgroundImage, step.BackgroundImage);
            UpdateImageSource(StoryCharacterImage, step.CharacterImage);

            RenderStoryChoices(step);
        }

        private void RenderStoryChoices(StoryStep step)
        {
            StoryChoicesPanel.Children.Clear();

            if (step.Choices.Count > 0)
            {
                StoryNextButton.Visibility = Visibility.Collapsed;
                StoryStatusText.Text = "請選擇你的行動";
                StoryStatusText.Visibility = Visibility.Visible;

                var choiceStyle = TryFindResource("StoryChoiceButtonStyle") as Style;

                for (int i = 0; i < step.Choices.Count; i++)
                {
                    var choice = step.Choices[i];
                    var button = new Button
                    {
                        Margin = new Thickness(0, i == 0 ? 0 : 12, 0, 0),
                        MinWidth = 320,
                        Tag = choice.NextId,
                        Cursor = Cursors.Hand,
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };

                    if (choiceStyle != null)
                    {
                        button.Style = choiceStyle;
                    }

                    button.Content = CreateChoiceContent(i + 1, choice.Text ?? string.Empty);
                    button.Click += StoryChoiceButton_Click;
                    StoryChoicesPanel.Children.Add(button);
                }
            }
            else
            {
                StoryStatusText.Visibility = Visibility.Collapsed;
                StoryNextButton.Visibility = Visibility.Visible;
                StoryNextButton.IsEnabled = true;
                StoryNextButton.Content = step.IsFinal ? "劇情完結" : "下一句";
            }
        }

        private FrameworkElement CreateChoiceContent(int displayIndex, string text)
        {
            var grid = new Grid
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var badge = new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(18),
                Background = new SolidColorBrush(Color.FromArgb(0xEE, 0x2B, 0x35, 0x4A)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x88, 0xA9, 0xC4, 0xFF)),
                BorderThickness = new Thickness(1.2),
                VerticalAlignment = VerticalAlignment.Center
            };

            var badgeText = new TextBlock
            {
                Text = displayIndex.ToString("00", CultureInfo.InvariantCulture),
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            badge.Child = badgeText;

            var textBlock = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xF4, 0xFF)),
                FontSize = 18,
                Margin = new Thickness(18, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            grid.Children.Add(badge);
            Grid.SetColumn(textBlock, 1);
            grid.Children.Add(textBlock);

            return grid;
        }

        private void ResetStoryUi()
        {
            StopTypewriterEffect();
            _currentFullDialogue = string.Empty;
            _typewriterIndex = 0;
            _isTypewriting = false;
            StoryTitleText.Text = string.Empty;
            StorySpeakerText.Text = string.Empty;
            StoryDialogueText.Text = string.Empty;
            StoryStatusText.Text = string.Empty;
            StoryStatusText.Visibility = Visibility.Collapsed;
            StoryChoicesPanel.Children.Clear();
            StoryNextButton.Visibility = Visibility.Visible;
            StoryNextButton.IsEnabled = true;
            StoryNextButton.Content = "下一句";
            StoryBackgroundImage.Source = null;
            StoryCharacterImage.Source = null;
            CollapseStoryMenu();
        }

        private void StartTypewriter(string text)
        {
            _currentFullDialogue = text ?? string.Empty;
            _typewriterIndex = 0;
            StopTypewriterEffect();

            if (string.IsNullOrEmpty(_currentFullDialogue))
            {
                StoryDialogueText.Text = string.Empty;
                _isTypewriting = false;
                return;
            }

            StoryDialogueText.Text = string.Empty;
            _isTypewriting = true;

            _typewriterTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(35)
            };
            _typewriterTimer.Tick += TypewriterTimer_Tick;
            _typewriterTimer.Start();
        }

        private void TypewriterTimer_Tick(object? sender, EventArgs e)
        {
            if (_typewriterIndex < _currentFullDialogue.Length)
            {
                _typewriterIndex++;
                StoryDialogueText.Text = _currentFullDialogue.Substring(0, _typewriterIndex);
            }

            if (_typewriterIndex >= _currentFullDialogue.Length)
            {
                CompleteTypewriter();
            }
        }

        private void CompleteTypewriter()
        {
            StopTypewriterEffect();
            StoryDialogueText.Text = _currentFullDialogue;
            _isTypewriting = false;
        }

        private bool FinishTypewriterEarly()
        {
            if (!_isTypewriting)
            {
                return false;
            }

            CompleteTypewriter();
            return true;
        }

        private void StopTypewriterEffect()
        {
            if (_typewriterTimer != null)
            {
                _typewriterTimer.Tick -= TypewriterTimer_Tick;
                _typewriterTimer.Stop();
                _typewriterTimer = null;
            }

            _isTypewriting = false;
        }

        private void UpdateImageSource(Image target, string? relativePath)
        {
            if (target == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                target.Source = null;
                return;
            }

            var normalized = relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar);
            var absolutePath = System.IO.Path.Combine(AppContext.BaseDirectory, normalized);

            if (!File.Exists(absolutePath))
            {
                Trace.WriteLine($"[Story] Missing asset: {absolutePath}");
                target.Source = null;
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(absolutePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                target.Source = bitmap;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[Story] Failed to load image: {ex.Message}");
                target.Source = null;
            }
        }

        private void Save1_BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            IniFiles DeleteSave = new IniFiles(LoadSetini);
            DeleteSave.IniWriteValue("Save1", "Level", "1");
            DeleteSave.IniWriteValue("Save1", "Chapter", "0");
            DeleteSave.IniWriteValue("Save1", "Money", "0");
            DeleteSave.IniWriteValue("Save1", "LastSaveTime", "");
            ShowSaveManager();
        }

        private void Save2_BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            IniFiles DeleteSave = new IniFiles(LoadSetini);
            DeleteSave.IniWriteValue("Save2", "Level", "1");
            DeleteSave.IniWriteValue("Save2", "Chapter", "0");
            DeleteSave.IniWriteValue("Save2", "Money", "0");
            DeleteSave.IniWriteValue("Save2", "LastSaveTime", "");
            ShowSaveManager();
        }

        private void Save3_BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            IniFiles DeleteSave = new IniFiles(LoadSetini);
            DeleteSave.IniWriteValue("Save", "Level", "1");
            DeleteSave.IniWriteValue("Save3", "Chapter", "0");
            DeleteSave.IniWriteValue("Save3", "Money", "0");
            DeleteSave.IniWriteValue("Save3", "LastSaveTime", "");
            ShowSaveManager();

        }
    }
}