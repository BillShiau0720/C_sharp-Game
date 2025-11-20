using Birthday.Services;
using Birthday.Story;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private ContextMenu? _activeSkillMenu;
        private int _typewriterIndex;
        private string _currentFullDialogue = string.Empty;
        private TextBlock? _currentDialogueTarget;
        private bool _isTypewriting;
        private const string PlayerDefeatStepId = "player_defeat";
        private bool _isStoryMenuOpen;
        private int _lastPlayerHp = -1;
        private int _lastPlayerHpMax = -1;
        private int _lastEnemyHp = -1;
        private int _lastEnemyHpMax = -1;
        private StoryStep? _currentStep;

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

        private void BattleNextButton_Click(object sender, RoutedEventArgs e)
        {
            if (FinishTypewriterEarly())
            {
                return;
            }

            if (ShouldBlockBattleAdvance())
            {
                if (BattlePromptText != null)
                {
                    BattlePromptText.Text = "敵人尚未倒下，繼續戰鬥！";
                }

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
            if (sender is Button button)
            {
                HandleChoiceSelection(button.Tag as string);
            }
        }

        private void HandleChoiceSelection(string? nextId)
        {
            if (FinishTypewriterEarly())
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(nextId))
            {
                _storyService?.Choose(nextId);
            }
        }


        private void StoryService_StepChanged(StoryStep step)
        {
            Dispatcher.Invoke(() =>
            {
                _currentStep = step;
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
                _currentDialogueTarget = null;
                _isStoryRunning = false;
                _currentStep = null;
                StoryLayout.Visibility = Visibility.Visible;
                BattleLayout.Visibility = Visibility.Collapsed;

                StoryChoicesPanel.Children.Clear();
                StoryNextButton.Visibility = Visibility.Visible;
                StoryNextButton.IsEnabled = false;
                StoryNextButton.Content = "劇情完結";
                StoryStatusText.Text = "Need to add fighting system.";
                StoryStatusText.Visibility = Visibility.Visible;
                StorySpeakerText.Text = "系統";
                StoryDialogueText.Text = "Need to add money system";
                StoryCharacterImage.Source = null;
                BattleChoicesPanel.Children.Clear();
                BattleNextButton.Visibility = Visibility.Visible;
                BattleNextButton.IsEnabled = false;
                ResetBattleTracking();
            });
        }
        private void ApplyStoryStep(StoryStep step)
        {
            StoryLayout.Visibility = step.UseBattleLayout ? Visibility.Collapsed : Visibility.Visible;
            BattleLayout.Visibility = step.UseBattleLayout ? Visibility.Visible : Visibility.Collapsed;

            if (!step.UseBattleLayout)
            {
                ResetBattleTracking();
            }

            StopTypewriterEffect();
            _currentFullDialogue = string.Empty;
            _typewriterIndex = 0;

            if (step.UseBattleLayout)
            {
                ApplyBattleStep(step);
            }
            else
            {
                ApplyNarrativeStep(step);
            }
        }

        private void ApplyNarrativeStep(StoryStep step)
        {
            StoryTitleText.Text = step.Title ?? string.Empty;
            StorySpeakerText.Text = string.IsNullOrWhiteSpace(step.CharacterName) ? "旁白" : step.CharacterName;
            StartTypewriter(step.Dialogue ?? string.Empty, StoryDialogueText);
            StoryStatusText.Visibility = Visibility.Collapsed;
            StoryStatusText.Text = string.Empty;

            UpdateImageSource(StoryBackgroundImage, step.BackgroundImage);
            UpdateImageSource(StoryCharacterImage, step.CharacterImage);

            RenderStoryChoices(step);
        }

        private void ApplyBattleStep(StoryStep step)
        {
            BattleTitleText.Text = step.Title ?? string.Empty;
            BattleSpeakerText.Text = string.IsNullOrWhiteSpace(step.CharacterName) ? "旁白" : step.CharacterName;
            BattlePromptText.Text = step.BattlePrompt ?? string.Empty;

            UpdateBattleHealth(step);

            StartTypewriter(step.Dialogue ?? string.Empty, BattleDialogueText);

            StartTypewriter(step.Dialogue ?? string.Empty, BattleDialogueText);

            UpdateImageSource(BattleBackgroundImage, step.BackgroundImage);
            UpdateImageSource(BattlePlayerAvatar, step.PlayerAvatar ?? step.CharacterImage);
            UpdateImageSource(BattleAllyAvatar, step.AllyAvatar);
            UpdateImageSource(BattleEnemyAvatar, step.EnemyAvatar ?? step.CharacterImage);

            RenderBattleChoices(step);
        }


        private void RenderStoryChoices(StoryStep step)
        {
            StoryChoicesPanel.Children.Clear();

            if (step.Choices.Count > 0)
            {
                BattleNextButton.Visibility = Visibility.Collapsed;

                var battleActionStyle = TryFindResource("BattleActionButtonStyle") as Style;

                for (int i = 0; i < step.Choices.Count; i++)
                {
                    var choice = step.Choices[i];
                    var button = new Button
                    {
                        Margin = new Thickness(12),
                        MinHeight = 92,
                        MinWidth = 180,
                        Tag = choice.NextId,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch
                    };

                    if (battleActionStyle != null)
                    {
                        button.Style = battleActionStyle;
                    }

                    button.Content = new TextBlock
                    {
                        Text = choice.Text ?? string.Empty,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        FontSize = 20,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x05, 0x07, 0x0F)),
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    button.Click += StoryChoiceButton_Click;
                    BattleChoicesPanel.Children.Add(button);
                }
            }
            else
            {
                BattleNextButton.Visibility = Visibility.Visible;
                BattleNextButton.IsEnabled = true;
                BattleNextButton.Content = step.IsFinal ? "劇情完結" : "下一句";
            }
        }
        private enum BattleActionKind
        {
            Attack,
            Defense,
            Skill,
            Other
        }


        private void RenderBattleChoices(StoryStep step)
        {
            CloseSkillMenu();
            BattleChoicesPanel.Children.Clear();

            if (step.Choices.Count > 0)
            {
                BattleNextButton.Visibility = Visibility.Collapsed;

                var skillChoices = new List<StoryChoice>();

                for (int i = 0; i < step.Choices.Count; i++)
                {
                    var choice = step.Choices[i];
                    var kind = GetBattleActionKind(choice.Text, i);

                    if (kind == BattleActionKind.Skill)
                    {
                        skillChoices.Add(choice);
                        continue;
                    }

                    BattleChoicesPanel.Children.Add(CreateBattleActionButton(choice, i));
                }

                if (skillChoices.Count > 0)
                {
                    BattleChoicesPanel.Children.Add(CreateSkillBundleButton(skillChoices));
                }
            }
            else
            {
                BattleNextButton.Visibility = Visibility.Visible;
                BattleNextButton.IsEnabled = true;
                BattleNextButton.Content = step.IsFinal ? "劇情完結" : "下一句";
            }
        }

        private Style? ResolveBattleActionStyle(string? text, int index)
        {
            var normalized = (text ?? string.Empty).Replace('：', ':').ToLowerInvariant();

            if (normalized.Contains("防禦"))
            {
                return TryFindResource("BattleDefenseButtonStyle") as Style;
            }

            if (normalized.Contains("攻擊"))
            {
                return TryFindResource("BattleAttackButtonStyle") as Style;
            }

            if (normalized.Contains("風剪"))
            {
                return TryFindResource("BattleSkill1ButtonStyle") as Style;
            }

            if (normalized.Contains("霜鎖") || normalized.Contains("霜锁"))
            {
                return TryFindResource("BattleSkill2ButtonStyle") as Style;
            }

            if (normalized.Contains("影縛") || normalized.Contains("影缚"))
            {
                return TryFindResource("BattleSkill3ButtonStyle") as Style;
            }

            if (normalized.Contains("雷踏"))
            {
                return TryFindResource("BattleSkill4ButtonStyle") as Style;
            }

            if (index >= 2 && index <= 5)
            {
                // fall back to skill palette for additional unnamed skills
                var skillStyles = new[]
                {
                    "BattleSkill1ButtonStyle",
                    "BattleSkill2ButtonStyle",
                    "BattleSkill3ButtonStyle",
                    "BattleSkill4ButtonStyle"
                };

                var styleKey = skillStyles[index % skillStyles.Length];
                return TryFindResource(styleKey) as Style;
            }

            return TryFindResource("BattleUtilityButtonStyle") as Style;
        }

        private BattleActionKind GetBattleActionKind(string? text, int index)
        {
            var normalized = (text ?? string.Empty).Replace('：', ':').ToLowerInvariant();

            if (normalized.Contains("防禦"))
            {
                return BattleActionKind.Defense;
            }

            if (normalized.Contains("攻擊"))
            {
                return BattleActionKind.Attack;
            }

            if (normalized.Contains("風剪")
                || normalized.Contains("霜鎖")
                || normalized.Contains("霜锁")
                || normalized.Contains("影縛")
                || normalized.Contains("影缚")
                || normalized.Contains("雷踏")
                || (index >= 2 && index <= 5))
            {
                return BattleActionKind.Skill;
            }

            return BattleActionKind.Other;
        }

        private Button CreateBattleActionButton(StoryChoice choice, int index)
        {
            var button = new Button
            {
                Margin = new Thickness(14, 12, 14, 12),
                MinHeight = 96,
                MinWidth = 196,
                Tag = choice.NextId,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            button.Style = ResolveBattleActionStyle(choice.Text, index) ?? TryFindResource("BattleActionButtonBaseStyle") as Style;
            button.Content = CreateBattleChoiceContent(choice.Text ?? string.Empty);
            button.Click += StoryChoiceButton_Click;

            return button;
        }

        private Button CreateSkillBundleButton(IReadOnlyCollection<StoryChoice> skillChoices)
        {
            var skillButton = new Button
            {
                Margin = new Thickness(14, 12, 14, 12),
                MinHeight = 96,
                MinWidth = 196,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Content = CreateBattleChoiceContent("技能: 展開招式")
            };

            skillButton.Style = TryFindResource("BattleSkill1ButtonStyle") as Style ?? TryFindResource("BattleActionButtonBaseStyle") as Style;

            var menu = BuildSkillMenu(skillChoices);

            skillButton.Click += (s, e) =>
            {
                if (FinishTypewriterEarly())
                {
                    return;
                }

                OpenSkillMenu(skillButton, menu);
            };

            return skillButton;
        }

        private ContextMenu BuildSkillMenu(IEnumerable<StoryChoice> skillChoices)
        {
            var menu = new ContextMenu
            {
                Placement = PlacementMode.Bottom,
                StaysOpen = false,
                MinWidth = 360,
                Background = new SolidColorBrush(Color.FromArgb(0xF2, 0xF8, 0xFA, 0xFF)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x8C, 0xA4, 0xE6)),
                BorderThickness = new Thickness(1.2),
                HasDropShadow = true
            };

            var factory = new FrameworkElementFactory(typeof(UniformGrid));
            factory.SetValue(UniformGrid.ColumnsProperty, 2);
            factory.SetValue(UniformGrid.MarginProperty, new Thickness(6));
            menu.ItemsPanel = new ItemsPanelTemplate(factory);

            foreach (var choice in skillChoices)
            {
                var item = new MenuItem
                {
                    Tag = choice.NextId,
                    Header = CreateBattleChoiceContent(choice.Text ?? string.Empty),
                    Padding = new Thickness(4),
                    Margin = new Thickness(6, 4, 6, 4),
                    Foreground = new SolidColorBrush(Color.FromRgb(0x0A, 0x0D, 0x18)),
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                };

                item.Click += SkillMenuItem_Click;
                menu.Items.Add(item);
            }

            menu.Closed += (s, e) =>
            {
                if (_activeSkillMenu == menu)
                {
                    _activeSkillMenu = null;
                }
            };

            return menu;
        }

        private void OpenSkillMenu(Button skillButton, ContextMenu menu)
        {
            CloseSkillMenu();

            _activeSkillMenu = menu;
            menu.PlacementTarget = skillButton;
            menu.IsOpen = true;
        }

        private void SkillMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                HandleChoiceSelection(menuItem.Tag as string);
            }
        }

        private void CloseSkillMenu()
        {
            if (_activeSkillMenu != null)
            {
                _activeSkillMenu.IsOpen = false;
                _activeSkillMenu = null;
            }
        }

        private static UIElement CreateBattleChoiceContent(string text)
        {
            var parts = text.Replace('：', ':').Split(':');
            var title = parts[0].Trim();
            var detail = parts.Length > 1 ? string.Join(":", parts.Skip(1)).Trim() : string.Empty;

            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            stack.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(title) ? text : title,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x0A, 0x0D, 0x18))
            });

            if (!string.IsNullOrWhiteSpace(detail))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = detail,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    FontSize = 15,
                    Margin = new Thickness(0, 4, 0, 0),
                    Foreground = new SolidColorBrush(Color.FromRgb(0x24, 0x2C, 0x45)),
                    Opacity = 0.9
                });
            }

            return stack;
        }

        private void UpdateBattleHealth(StoryStep step)
        {
            var playerHasHp = TryExtractHp(step.PlayerStatus, out var playerCurrent, out var playerMax);
            var allyHasHp = TryExtractHp(step.AllyStatus, out var allyCurrent, out var allyMax);
            var enemyHasHp = TryExtractHp(step.EnemyStatus, out var enemyCurrent, out var enemyMax);

            _lastPlayerHp = playerHasHp ? playerCurrent : -1;
            _lastPlayerHpMax = playerHasHp ? playerMax : -1;
            _lastEnemyHp = enemyHasHp ? enemyCurrent : -1;
            _lastEnemyHpMax = enemyHasHp ? enemyMax : -1;

            UpdateHealthRow(BattlePlayerHealthRow, BattlePlayerHpLabel, BattlePlayerHpValueText, BattlePlayerHpFillScale,
                ExtractNameFromStatus(step.PlayerStatus, "玩家"), playerHasHp, playerCurrent, playerMax);
            UpdateHealthRow(BattleAllyHealthRow, BattleAllyHpLabel, BattleAllyHpValueText, BattleAllyHpFillScale,
                ExtractNameFromStatus(step.AllyStatus, "夥伴"), allyHasHp, allyCurrent, allyMax);
            UpdateHealthRow(BattleEnemyHealthRow, BattleEnemyHpLabel, BattleEnemyHpValueText, BattleEnemyHpFillScale,
                ExtractNameFromStatus(step.EnemyStatus, "敵人"), enemyHasHp, enemyCurrent, enemyMax);

            var hasAny = (playerHasHp && playerMax > 0) || (allyHasHp && allyMax > 0) || (enemyHasHp && enemyMax > 0);
            BattleHealthPanel.Visibility = hasAny ? Visibility.Visible : Visibility.Collapsed;

            CheckPlayerDeath(step);
        }

        private void CheckPlayerDeath(StoryStep step)
        {
            if (_lastPlayerHpMax > 0 && _lastPlayerHp <= 0)
            {
                if (!string.Equals(step.Id, PlayerDefeatStepId, StringComparison.OrdinalIgnoreCase))
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        _storyService?.Choose(PlayerDefeatStepId);
                    }));
                }
            }
        }
        private bool ShouldBlockBattleAdvance()
        {
            if (_currentStep?.UseBattleLayout != true || _storyService == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(_currentStep.NextId))
            {
                return false;
            }

            if (!_storyService.TryGetStep(_currentStep.NextId, out var nextStep) || nextStep == null)
            {
                return false;
            }

            if (nextStep.UseBattleLayout)
            {
                return false;
            }

            return _lastEnemyHpMax > 0 && _lastEnemyHp > 0;
        }

        private void UpdateHealthRow(UIElement row, TextBlock label, TextBlock value, ScaleTransform fillTransform,
            string fallbackLabel, bool hasHp, int current, int max)
        {
            if (row == null || label == null || value == null || fillTransform == null)
            {
                return;
            }

            if (hasHp && max > 0)
            {
                row.Visibility = Visibility.Visible;
                label.Text = fallbackLabel;
                value.Text = $"{current}/{max}";
                var ratio = Math.Max(0, Math.Min((double)current / max, 1));
                fillTransform.ScaleX = ratio;
            }
            else
            {
                row.Visibility = Visibility.Collapsed;
                value.Text = string.Empty;
                fillTransform.ScaleX = 0;
            }
        }

        private static bool TryExtractHp(string? statusText, out int current, out int max)
        {
            current = 0;
            max = 0;

            if (string.IsNullOrWhiteSpace(statusText))
            {
                return false;
            }

            var match = Regex.Match(statusText, @"HP\s*(\d+)\s*/\s*(\d+)", RegexOptions.IgnoreCase);
            if (match.Success
                && int.TryParse(match.Groups[1].Value, out current)
                && int.TryParse(match.Groups[2].Value, out max))
            {
                return true;
            }

            current = 0;
            max = 0;
            return false;
        }

        private static string ExtractNameFromStatus(string? statusText, string fallback)
        {
            if (string.IsNullOrWhiteSpace(statusText))
            {
                return fallback;
            }

            var hpIndex = statusText.IndexOf("HP", StringComparison.OrdinalIgnoreCase);
            if (hpIndex > 0)
            {
                return statusText.Substring(0, hpIndex).Trim('｜', ' ', ':');
            }

            return statusText.Trim();
        }

        private void ResetBattleHealthPanel()
        {
            ResetHealthRow(BattlePlayerHealthRow, BattlePlayerHpValueText, BattlePlayerHpFillScale);
            ResetHealthRow(BattleAllyHealthRow, BattleAllyHpValueText, BattleAllyHpFillScale);
            ResetHealthRow(BattleEnemyHealthRow, BattleEnemyHpValueText, BattleEnemyHpFillScale);

            if (BattleHealthPanel != null)
            {
                BattleHealthPanel.Visibility = Visibility.Collapsed;
            }
            ResetBattleTracking();
        }

        private static void ResetHealthRow(UIElement? row, TextBlock? value, ScaleTransform? fillTransform)
        {
            if (row != null)
            {
                row.Visibility = Visibility.Collapsed;
            }

            if (value != null)
            {
                value.Text = string.Empty;
            }

            if (fillTransform != null)
            {
                fillTransform.ScaleX = 0;
            }
        }
        private void ResetBattleTracking()
        {
            CloseSkillMenu();
            _lastPlayerHp = -1;
            _lastPlayerHpMax = -1;
            _lastEnemyHp = -1;
            _lastEnemyHpMax = -1;
        }


        private void ResetStoryUi()
        {
            StopTypewriterEffect();
            _currentFullDialogue = string.Empty;
            _typewriterIndex = 0;
            _isTypewriting = false;
            _currentDialogueTarget = null;
            StoryLayout.Visibility = Visibility.Visible;
            BattleLayout.Visibility = Visibility.Collapsed;
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
            BattleTitleText.Text = string.Empty;
            BattlePromptText.Text = string.Empty;
            BattleSpeakerText.Text = string.Empty;
            BattleDialogueText.Text = string.Empty;
            ResetBattleHealthPanel();
            BattleChoicesPanel.Children.Clear();
            BattleNextButton.Visibility = Visibility.Visible;
            BattleNextButton.IsEnabled = true;
            BattleNextButton.Content = "下一句";
            BattleBackgroundImage.Source = null;
            BattlePlayerAvatar.Source = null;
            BattleAllyAvatar.Source = null;
            BattleEnemyAvatar.Source = null;
            CollapseStoryMenu();
        }

        private void StartTypewriter(string text, TextBlock target)
        {
            _currentFullDialogue = text ?? string.Empty;
            _typewriterIndex = 0;
            StopTypewriterEffect();
            _currentDialogueTarget = target;

            if (string.IsNullOrEmpty(_currentFullDialogue))
            {
                if (_currentDialogueTarget != null)
                {
                    _currentDialogueTarget.Text = string.Empty;
                }

                _isTypewriting = false;
                _currentDialogueTarget = null;
                return;
            }

            if (_currentDialogueTarget != null)
            {
                _currentDialogueTarget.Text = string.Empty;
            }
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
            if (_typewriterIndex < _currentFullDialogue.Length && _currentDialogueTarget != null)
            {
                _typewriterIndex++;
                _currentDialogueTarget.Text = _currentFullDialogue.Substring(0, _typewriterIndex);
            }

            if (_typewriterIndex >= _currentFullDialogue.Length)
            {
                CompleteTypewriter();
            }
        }
        private void CompleteTypewriter()
        {
            StopTypewriterEffect();
            if (_currentDialogueTarget != null)
            {
                _currentDialogueTarget.Text = _currentFullDialogue;
            }
            _isTypewriting = false;
            _currentDialogueTarget = null;
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