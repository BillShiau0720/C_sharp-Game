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
        private BattleSession? _activeBattle;
        private string? _pendingBattleResolutionNextId;
        private readonly Dictionary<string, BattleDefinition> _battleDefinitions = new();
        private StoryStep? _currentStep;

        public MainWindow()
        {
            InitializeComponent();

            this.KeyDown += MainWindow_KeyDown;

            InitializeStorySystem();
            InitializeBattleDefinitions();

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

        private void InitializeBattleDefinitions()
        {
            _battleDefinitions.Clear();

            _battleDefinitions["tutorial_choice"] = new BattleDefinition(
                "tutorial_choice",
                "bandit_report",
                PlayerDefeatStepId,
                new BattleFighter { Name = "貓貓", MaxHp = 120, Attack = 26, Defense = 8 },
                new BattleFighter { Name = "豹豹", MaxHp = 110, Attack = 22, Defense = 10, SupportRatio = 0.7 },
                new BattleFighter { Name = "訓練傀儡", MaxHp = 90, Attack = 18, Defense = 6 },
                System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Audio", "BGM", "Battle.mp3"),
                "訓練模式：依序輪流出手，擊倒傀儡即可結束。"
            );

            _battleDefinitions["bandit_battle_choice"] = new BattleDefinition(
                "bandit_battle_choice",
                "bandit_battle_result",
                PlayerDefeatStepId,
                new BattleFighter { Name = "貓貓", MaxHp = 120, Attack = 28, Defense = 10 },
                new BattleFighter { Name = "豹豹", MaxHp = 110, Attack = 24, Defense = 12, SupportRatio = 0.8 },
                new BattleFighter { Name = "山匪頭目", MaxHp = 140, Attack = 22, Defense = 10 },
                System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Audio", "BGM", "Battle.mp3"),
                "山道伏戰：像寶可夢般下達指令，擊潰頭目。"
            );

            _battleDefinitions["final_battle_choice"] = new BattleDefinition(
                "final_battle_choice",
                "epilogue",
                PlayerDefeatStepId,
                new BattleFighter { Name = "貓貓", MaxHp = 120, Attack = 30, Defense = 12 },
                new BattleFighter { Name = "豹豹", MaxHp = 95, Attack = 26, Defense = 12, SupportRatio = 0.9 },
                new BattleFighter { Name = "黑袍首領", MaxHp = 180, Attack = 26, Defense = 12 },
                System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Audio", "BGM", "Final Boss Battle.mp3"),
                "決戰天璇峰：連攜武功削弱魔焰，輪流出手直至勝負。"
            );
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

            if (ShouldBlockBattleAdvance())
            {
                if (BattlePromptText != null)
                {
                    BattlePromptText.Text = "戰鬥尚未結束，請繼續戰鬥直到一方 HP 歸零。";
                }

                return;
            }

            _storyService?.Continue();
        }

        private void BattleNextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep?.UseBattleLayout == true && _isTypewriting)
            {
                return;
            }

            if (FinishTypewriterEarly())
            {
                return;
            }

            if (_pendingBattleResolutionNextId != null)
            {
                var target = _pendingBattleResolutionNextId;
                _pendingBattleResolutionNextId = null;
                _activeBattle = null;
                CloseSkillMenu();
                _storyService?.Choose(target);
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

        private void BattleChoiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is StoryChoice choice)
            {
                if (_currentStep?.UseBattleLayout == true && _isTypewriting)
                {
                    return;
                }

                if (_activeBattle != null && _currentStep?.UseBattleLayout == true)
                {
                    ExecuteBattleAction(choice);
                    return;
                }

                HandleChoiceSelection(choice.NextId);
            }
        }

        private void HandleChoiceSelection(string? nextId)
        {
            if (FinishTypewriterEarly())
            {
                return;
            }

            if (ShouldBlockBattleAdvance())
            {
                if (BattlePromptText != null)
                {
                    BattlePromptText.Text = "敵方仍在場上，無法跳過戰鬥劇情。";
                }

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

            if (_activeBattle != null)
            {
                RefreshBattleHealthFromState();
            }
            else
            {
                UpdateBattleHealth(step);
            }

            StartTypewriter(step.Dialogue ?? string.Empty, BattleDialogueText);

            UpdateImageSource(BattleBackgroundImage, step.BackgroundImage);
            UpdateImageSource(BattlePlayerAvatar, step.PlayerAvatar ?? step.CharacterImage);
            UpdateImageSource(BattleAllyAvatar, step.AllyAvatar);
            UpdateImageSource(BattleEnemyAvatar, step.EnemyAvatar ?? step.CharacterImage);

            TryStartBattle(step);
            RenderBattleChoices(step);
        }


        // 劇情模式的指令清單（仙劍風）
        // 劇情模式的指令清單：改用 StoryChoicesPanel + 仙劍風 Style
        private void RenderStoryChoices(StoryStep step)
        {
            // 清空舊的選項按鈕
            StoryChoicesPanel.Children.Clear();

            // 有選項的情況 → 顯示一整排指令按鈕
            if (step?.Choices != null && step.Choices.Count > 0)
            {
                // 劇情有選項時，先把「下一句」按鈕藏起來
                StoryNextButton.Visibility = Visibility.Collapsed;

                // 拿劇情專用 Style（外觀仙劍風）
                var storyButtonStyle = TryFindResource("StoryChoiceButtonStyle") as Style;

                foreach (var choice in step.Choices)
                {
                    var btn = new Button
                    {
                        Tag = choice.NextId,
                        // 內容就是選項文字，外觀交給 Style 控制
                        Content = choice.Text ?? string.Empty,
                        Margin = new Thickness(0, 6, 0, 6),
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };

                    if (storyButtonStyle != null)
                    {
                        btn.Style = storyButtonStyle;
                    }

                    btn.Click += StoryChoiceButton_Click;

                    // ✅ 關鍵：加到「StoryChoicesPanel」，不再碰 BattleChoicesPanel
                    StoryChoicesPanel.Children.Add(btn);
                }
            }
            else
            {
                // 沒選項 → 顯示「下一句」或「劇情完結」
                StoryNextButton.Visibility = Visibility.Visible;
                StoryNextButton.IsEnabled = true;
                StoryNextButton.Content = step != null && step.IsFinal
                    ? "劇情完結"
                    : "下一句";
            }
        }

        private enum BattleActionKind
        {
            Attack,
            Defense,
            Skill,
            Other
        }

        private enum BattleAction
        {
            Attack,
            Defend,
            SkillWind,
            SkillFrost,
            SkillBind,
            SkillThunder,
            Item
        }

        private record BattleDefinition(
            string StepId,
            string VictoryNextId,
            string DefeatNextId,
            BattleFighter Player,
            BattleFighter Ally,
            BattleFighter Enemy,
            string? BattleBgm,
            string IntroPrompt);

        private sealed class BattleSession
        {
            public BattleDefinition Definition { get; }
            public BattleFighter Player { get; }
            public BattleFighter Ally { get; }
            public BattleFighter Enemy { get; }
            public bool IsResolved => Player.CurrentHp <= 0 || Enemy.CurrentHp <= 0;
            public bool EnemyStunned { get; set; }
            public bool PlayerGuarding { get; set; }
            public int Turn { get; private set; } = 1;

            public BattleSession(BattleDefinition definition)
            {
                Definition = definition;
                Player = definition.Player.Clone();
                Ally = definition.Ally.Clone();
                Enemy = definition.Enemy.Clone();
            }

            public BattleTurnResult ExecutePlayerAction(BattleAction action)
            {
                var log = new StringBuilder();

                if (IsResolved)
                {
                    log.AppendLine("戰鬥已結束，等待下一步劇情。");
                    return new BattleTurnResult(log.ToString(), ResolveOutcome());
                }

                switch (action)
                {
                    case BattleAction.Attack:
                        ApplyAttack(Player, Enemy, 1.0, log, "貓貓使用普通攻擊");
                        break;
                    case BattleAction.Defend:
                        PlayerGuarding = true;
                        log.AppendLine("貓貓收刀防守，下一次受到的傷害降低。");
                        break;
                    case BattleAction.SkillWind:
                        ApplyAttack(Player, Enemy, 0.85, log, "風剪四式削弱防禦");
                        Enemy.DefenseModifier = Math.Max(0, Enemy.DefenseModifier - 2);
                        log.AppendLine("敵方防禦下降，後續攻擊將更痛。");
                        break;
                    case BattleAction.SkillFrost:
                        ApplyAttack(Player, Enemy, 0.75, log, "霜鎖月輪斬");
                        Enemy.AttackModifier = Math.Max(0, Enemy.AttackModifier - 2);
                        log.AppendLine("敵方攻擊被寒氣壓制。");
                        break;
                    case BattleAction.SkillBind:
                        ApplyAttack(Player, Enemy, 0.5, log, "影縛勾鎖束縛敵人");
                        EnemyStunned = true;
                        log.AppendLine("敵人被定身，下個回合跳過。");
                        break;
                    case BattleAction.SkillThunder:
                        ApplyAttack(Player, Enemy, 1.35, log, "雷踏裂地爆發");
                        var recoil = Math.Max(4, (int)Math.Round(Player.Attack * 0.1));
                        Player.CurrentHp = Math.Max(0, Player.CurrentHp - recoil);
                        log.AppendLine($"真氣反震讓貓貓自損 {recoil} 點。");
                        break;
                    case BattleAction.Item:
                        var heal = Math.Min(Player.MaxHp - Player.CurrentHp, 30);
                        Player.CurrentHp += heal;
                        log.AppendLine($"貓貓使用金創藥，回復 {heal} HP。");
                        break;
                }

                if (Enemy.CurrentHp <= 0)
                {
                    log.AppendLine("敵人倒下，戰局逆轉！");
                    return new BattleTurnResult(log.ToString(), BattleOutcome.Victory);
                }

                if (Ally.CurrentHp > 0 && Enemy.CurrentHp > 0)
                {
                    ApplyAttack(Ally, Enemy, Ally.SupportRatio, log, "豹豹按腳本出手");
                    if (Enemy.CurrentHp <= 0)
                    {
                        log.AppendLine("豹豹的追擊終結了戰鬥！");
                        return new BattleTurnResult(log.ToString(), BattleOutcome.Victory);
                    }
                }

                if (Enemy.CurrentHp > 0)
                {
                    if (EnemyStunned)
                    {
                        log.AppendLine("敵人被束縛，錯過了行動。");
                        EnemyStunned = false;
                    }
                    else
                    {
                        ApplyEnemyTurn(log);
                    }
                }

                Turn++;
                return new BattleTurnResult(log.ToString(), ResolveOutcome());
            }

            private BattleOutcome ResolveOutcome()
            {
                if (Player.CurrentHp <= 0)
                {
                    return BattleOutcome.Defeat;
                }

                if (Enemy.CurrentHp <= 0)
                {
                    return BattleOutcome.Victory;
                }

                return BattleOutcome.Ongoing;
            }

            private void ApplyEnemyTurn(StringBuilder log)
            {
                var target = Player;
                var damage = CalculateDamage(Enemy, target, 1.0);

                if (PlayerGuarding)
                {
                    damage = (int)Math.Round(damage * 0.55);
                    PlayerGuarding = false;
                    log.AppendLine("防禦姿態減輕了傷害。");
                }

                target.CurrentHp = Math.Max(0, target.CurrentHp - damage);
                log.AppendLine($"敵方猛攻，{target.Name} 受到 {damage} 點傷害。");
            }

            private void ApplyAttack(BattleFighter attacker, BattleFighter target, double multiplier, StringBuilder log, string title)
            {
                var damage = CalculateDamage(attacker, target, multiplier);
                target.CurrentHp = Math.Max(0, target.CurrentHp - damage);
                log.AppendLine($"{title}，造成 {damage} 傷害。");
            }

            private static int CalculateDamage(BattleFighter attacker, BattleFighter target, double multiplier)
            {
                var attackStat = attacker.Attack + attacker.AttackModifier;
                var defenseStat = target.Defense + target.DefenseModifier;
                var raw = Math.Max(1, attackStat - (int)Math.Round(defenseStat * 0.35));
                var damage = (int)Math.Round(raw * multiplier);
                return Math.Max(1, damage);
            }
        }

        private sealed class BattleFighter
        {
            public string Name { get; init; } = string.Empty;
            public int MaxHp { get; init; }
            public int CurrentHp { get; set; }
            public int Attack { get; init; }
            public int Defense { get; init; }
            public int AttackModifier { get; set; }
            public int DefenseModifier { get; set; }
            public double SupportRatio { get; init; } = 0.8;

            public BattleFighter Clone()
            {
                return new BattleFighter
                {
                    Name = Name,
                    MaxHp = MaxHp,
                    CurrentHp = MaxHp,
                    Attack = Attack,
                    Defense = Defense,
                    AttackModifier = AttackModifier,
                    DefenseModifier = DefenseModifier,
                    SupportRatio = SupportRatio,
                };
            }
        }

        private enum BattleOutcome
        {
            Ongoing,
            Victory,
            Defeat
        }

        private sealed record BattleTurnResult(string Narration, BattleOutcome Outcome);

        private void RenderBattleChoices(StoryStep step)
        {
            CloseSkillMenu();
            BattleChoicesPanel.Children.Clear();
            BattleChoicesPanel.IsEnabled = true;

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

        private void TryStartBattle(StoryStep step)
        {
            if (_activeBattle != null)
            {
                RefreshBattleHealthFromState();
                return;
            }

            if (_battleDefinitions.TryGetValue(step.Id, out var definition))
            {
                _activeBattle = new BattleSession(definition);
                _pendingBattleResolutionNextId = null;
                RefreshBattleHealthFromState();

                if (!string.IsNullOrEmpty(definition.BattleBgm))
                {
                    BgmService.Instance.Play(definition.BattleBgm, loop: true, targetVolume: 1f, fadeSeconds: 0.6);
                }

                if (!string.IsNullOrWhiteSpace(definition.IntroPrompt))
                {
                    BattlePromptText.Text = definition.IntroPrompt;
                }
            }
        }

        private void RefreshBattleHealthFromState()
        {
            if (_activeBattle == null)
            {
                return;
            }

            _lastPlayerHp = _activeBattle.Player.CurrentHp;
            _lastPlayerHpMax = _activeBattle.Player.MaxHp;
            _lastEnemyHp = _activeBattle.Enemy.CurrentHp;
            _lastEnemyHpMax = _activeBattle.Enemy.MaxHp;

            UpdateHealthRow(BattlePlayerHealthRow, BattlePlayerHpLabel, BattlePlayerHpValueText, BattlePlayerHpFillScale,
                _activeBattle.Player.Name, true, _activeBattle.Player.CurrentHp, _activeBattle.Player.MaxHp);
            UpdateHealthRow(BattleAllyHealthRow, BattleAllyHpLabel, BattleAllyHpValueText, BattleAllyHpFillScale,
                _activeBattle.Ally.Name, _activeBattle.Ally.MaxHp > 0, _activeBattle.Ally.CurrentHp, _activeBattle.Ally.MaxHp);
            UpdateHealthRow(BattleEnemyHealthRow, BattleEnemyHpLabel, BattleEnemyHpValueText, BattleEnemyHpFillScale,
                _activeBattle.Enemy.Name, true, _activeBattle.Enemy.CurrentHp, _activeBattle.Enemy.MaxHp);

            var hasAny = _activeBattle.Player.MaxHp > 0 || _activeBattle.Ally.MaxHp > 0 || _activeBattle.Enemy.MaxHp > 0;
            BattleHealthPanel.Visibility = hasAny ? Visibility.Visible : Visibility.Collapsed;
        }

        private Style? FindBattleResourceStyle(string key)
        {
            // Use the battle layout scope first so we can pick up resources defined
            // inside the battle layer instead of only window-level resources.
            return BattleLayout.TryFindResource(key) as Style
                   ?? TryFindResource(key) as Style;
        }

        private Style? ResolveBattleActionStyle(string? text, int index)
        {
            var normalized = (text ?? string.Empty).Replace('：', ':').ToLowerInvariant();

            if (normalized.Contains("防禦"))
            {
                return FindBattleResourceStyle("BattleDefenseButtonStyle");
            }

            if (normalized.Contains("攻擊"))
            {
                return FindBattleResourceStyle("BattleAttackButtonStyle");
            }

            if (normalized.Contains("風剪"))
            {
                return FindBattleResourceStyle("BattleSkill1ButtonStyle");
            }

            if (normalized.Contains("霜鎖") || normalized.Contains("霜锁"))
            {
                return FindBattleResourceStyle("BattleSkill2ButtonStyle");
            }

            if (normalized.Contains("影縛") || normalized.Contains("影缚"))
            {
                return FindBattleResourceStyle("BattleSkill3ButtonStyle");
            }

            if (normalized.Contains("雷踏"))
            {
                return FindBattleResourceStyle("BattleSkill4ButtonStyle");
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
                return FindBattleResourceStyle(styleKey);
            }
            return FindBattleResourceStyle("BattleUtilityButtonStyle");
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
                Tag = choice,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            button.Style = ResolveBattleActionStyle(choice.Text, index)
                           ?? FindBattleResourceStyle("BattleActionButtonBaseStyle");
            button.Content = CreateBattleChoiceContent(choice.Text ?? string.Empty);
            button.Click += BattleChoiceButton_Click;

            return button;
        }

        private Button CreateSkillBundleButton(IReadOnlyCollection<StoryChoice> skillChoices)
        {
            var skillButton = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Content = CreateBattleChoiceContent("技能: 展開招式")
            };

            skillButton.Style = FindBattleResourceStyle("BattleSkill1ButtonStyle")
                                ?? FindBattleResourceStyle("BattleActionButtonBaseStyle");

            var menu = BuildSkillMenu(skillChoices);

            skillButton.Click += (s, e) =>
            {
                if (_currentStep?.UseBattleLayout == true && _isTypewriting)
                {
                    return;
                }

                if (FinishTypewriterEarly())
                    return;

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
                Background = new SolidColorBrush(Color.FromArgb(0xF0, 0x24, 0x2C, 0x45)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xCB, 0xB3, 0x7E)),
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
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFD, 0xF9, 0xF0)),
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
                if (menuItem.Tag is StoryChoice choice)
                {
                    if (_currentStep?.UseBattleLayout == true && _isTypewriting)
                    {
                        return;
                    }
                    if (_activeBattle != null)
                    {
                        ExecuteBattleAction(choice);
                        return;
                    }

                    HandleChoiceSelection(choice.NextId);
                    return;
                }

                HandleChoiceSelection(menuItem.Tag as string);
            }
        }

        private void ExecuteBattleAction(StoryChoice choice)
        {
            if (_activeBattle == null)
            {
                HandleChoiceSelection(choice.NextId);
                return;
            }

            var action = ResolveBattleActionFromChoice(choice);
            var result = _activeBattle.ExecutePlayerAction(action);

            RefreshBattleHealthFromState();

            BattlePromptText.Text = $"第 {_activeBattle.Turn} 回合：等待下一步指令";
            StartTypewriter(result.Narration.Trim(), BattleDialogueText);

            if (result.Outcome != BattleOutcome.Ongoing)
            {
                PrepareBattleResolution(result.Outcome);
            }
        }

        private void PrepareBattleResolution(BattleOutcome outcome)
        {
            if (_activeBattle == null)
            {
                return;
            }

            _pendingBattleResolutionNextId = outcome == BattleOutcome.Victory
                ? _activeBattle.Definition.VictoryNextId
                : _activeBattle.Definition.DefeatNextId;

            BattleChoicesPanel.IsEnabled = false;
            BattleNextButton.Visibility = Visibility.Visible;
            BattleNextButton.IsEnabled = true;
            BattleNextButton.Content = outcome == BattleOutcome.Victory ? "戰鬥勝利" : "戰鬥失敗";

            BattlePromptText.Text = outcome == BattleOutcome.Victory
                ? "勝利！點擊戰鬥勝利進入劇情結算。"
                : "敗北，點擊戰鬥失敗回到讀檔或重來。";
        }

        private BattleAction ResolveBattleActionFromChoice(StoryChoice choice)
        {
            var normalized = (choice.Text ?? string.Empty).Replace('：', ':').ToLowerInvariant();

            if (normalized.Contains("防禦"))
                return BattleAction.Defend;
            if (normalized.Contains("道具") || normalized.Contains("藥"))
                return BattleAction.Item;
            if (normalized.Contains("影縛") || normalized.Contains("影缚"))
                return BattleAction.SkillBind;
            if (normalized.Contains("雷踏") || normalized.Contains("雷"))
                return BattleAction.SkillThunder;
            if (normalized.Contains("霜") || normalized.Contains("月"))
                return BattleAction.SkillFrost;
            if (normalized.Contains("風") || normalized.Contains("飛燕"))
                return BattleAction.SkillWind;
            if (normalized.Contains("攻擊") || normalized.Contains("普攻"))
                return BattleAction.Attack;

            return BattleAction.Attack;
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
                Foreground = new SolidColorBrush(Color.FromRgb(0xFD, 0xF9, 0xF0))
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
                    Foreground = new SolidColorBrush(Color.FromRgb(0xD7, 0xE1, 0xF9)),
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
            if (_activeBattle != null)
            {
                return !_activeBattle.IsResolved || _pendingBattleResolutionNextId == null;
            }

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
            _activeBattle = null;
            _pendingBattleResolutionNextId = null;
            BattleChoicesPanel.IsEnabled = true;
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