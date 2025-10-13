using Birthday.Services;
using System.Diagnostics;
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

        public MainWindow()
        {
            InitializeComponent();

            this.KeyDown += MainWindow_KeyDown;
     
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
            // TODO: 開啟你的遊戲場景視窗或切換頁面
            /*
            var game = new GameWindow();   // 先做個占位視窗
            game.Owner = this;
            game.Show();
            this.Hide();
            */
        }

        private void LoadGame_Click(object sender, RoutedEventArgs e)
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
            }

            Trace.WriteLine("Loaded save compelet.");
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("Setting clicked.");
            SettingsPopup.Visibility = Visibility.Visible;

        }


    }
}