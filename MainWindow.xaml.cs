using System;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using Vlc.DotNet.Wpf;
using Vlc.DotNet.Core;
using System.Windows.Threading;

namespace VideoGadget
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool IsPlaying = false;
        private bool IsPlayed = false;
        private bool IsSeekbarClick = false;
        private Point mousePoint;

        private readonly DirectoryInfo vlcLibDirectory;
        private VlcControl control;

        private DispatcherTimer SeekBarUpdateThread;
        private bool DisplaySizeSetFlag = false;

        private int SeekbarUpdateInterval = 500;

        public MainWindow()
        {
            InitializeComponent();

            var currentAssembly = Assembly.GetEntryAssembly();
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            // Default installation path of VideoLAN.LibVLC.Windows
            vlcLibDirectory = new DirectoryInfo(System.IO.Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));
        }

        private void printf(string txt)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(txt);
#endif
        }

        /* マウス操作とD＆D操作とキーダウン操作関連 */
        private void Window_DragOver(object sender, DragEventArgs e)
        {
            // マウスポインタを変更する。
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = false;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop); // ドロップしたファイル名を全部取得する。
                string filename = "";
                foreach (string t in filenames)
                {
                    filename = filename + t;
                }
                printf(filename);

                control?.Dispose();
                control = new VlcControl();
                ControlContainer.Content = this.control;
                control.SourceProvider.CreatePlayer(vlcLibDirectory);

                // This can also be called before EndInit
                this.control.SourceProvider.MediaPlayer.Log += (_, args) =>
                {
                    string message = $"libVlc : {args.Level} {args.Message} @ {args.Module}";
                    printf(message);
                };

                string[] @params = null;
                @params = new string[] { "input-repeat=65535" }; // 繰り返し再生

                FileInfo fi = new FileInfo(filename);
                control.SourceProvider.MediaPlayer.SetMedia(fi, @params);
                control.SourceProvider.MediaPlayer.Play();
                control.SourceProvider.MediaPlayer.Audio.Volume = (int)VolumeSlider.Value;

                IsPlaying = true;

                DisplaySizeSetFlag = false;

                SeekBarUpdateThread = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(SeekbarUpdateInterval)
                };
                SeekBarUpdateThread.Tick += SeekBarUpdateThread_Tick;

                SeekBarUpdateThread.Start();
            }
        }

        private void MainMadiaElement_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.Right:
                    printf("right mme");
                    SwitchButton(ref IsPlaying);
                    if (IsPlaying) control.SourceProvider.MediaPlayer.Play();
                    else control.SourceProvider.MediaPlayer.Pause();
                    break;
                case MouseButton.XButton1:
                    printf("buton1");
                    break;
                case MouseButton.XButton2:
                    printf("button2");
                    break;
                default:
                    break;
            }

        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.Left:
                    printf("left");
                    System.Windows.Point position = e.GetPosition(this);
                    mousePoint = new Point(position.X, position.Y);
                    break;
                case MouseButton.Middle:
                    printf("middle");
                    System.Windows.Application.Current.Shutdown();
                    break;
                case MouseButton.Right:
                    printf("right wm");
                    break;
                case MouseButton.XButton1:
                    printf("buton1");
                    break;
                case MouseButton.XButton2:
                    printf("button2");
                    break;
                default:
                    break;
            }
        }

        private void MouseMoveHandler(object sender, MouseEventArgs e)
        {
            if (VolumeSlider.Opacity == 1.0f) return;
            if (SeekbarSlider.Opacity == 1.0f) return;

            // 左クリック時のみ
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                System.Windows.Point position = e.GetPosition(this);
                double pX = position.X;
                double pY = position.Y;

                this.Left += pX - mousePoint.X;
                this.Top += pY - mousePoint.Y;
            }

        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // SpaceキーでWindow最小化
            if (e.Key == Key.Space)
            {
                if (this.WindowState != WindowState.Minimized)
                {
                    this.WindowState = WindowState.Minimized;
                }
            }

        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // ダブルクリックでWindowの最大化と最小化を切り替える
            if (this.WindowState != WindowState.Maximized)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
        }

        /* Windowのロードと終了 */
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            VolumeSlider.Value = Properties.Settings.Default.VolumeSettings;
            VolumeSlider.Opacity = 0.0f;

            SeekbarSlider.Opacity = 0.0f;
            SeekbarSlider.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(Slider_MouseLeftButtonDown), true);
            SeekbarSlider.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(Slider_MouseLeftButtonUp), true);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.VolumeSettings = control.SourceProvider.MediaPlayer.Audio.Volume;
            Properties.Settings.Default.Save();

            control?.Dispose();
            control = null;
        }

        private void SwitchButton(ref bool btn)
        {
            if (btn) btn = false;
            else btn = true;
        }

        /* ボリューム操作関連 */
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (control == null) return;
            control.SourceProvider.MediaPlayer.Audio.Volume = (int)e.NewValue;
        }

        private void VolumeSlider_MouseEnter(object sender, MouseEventArgs e)
        {
            VolumeSlider.Opacity = 1.0f;
            printf("VolumeSlider on");
        }

        private void VolumeSlider_MouseLeave(object sender, MouseEventArgs e)
        {
            VolumeSlider.Opacity = 0.0f;
            printf("VolumeSlider off");
        }

        /* シークバー操作関連 */
        private void SeekbarSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SeekbarSlider.Opacity == 1.0f && IsSeekbarClick)
            {
                printf("Seekbar user control");

                double totalSec = control.SourceProvider.MediaPlayer.Length; 
                double sliderValue = SeekbarSlider.Value;
                long targetSec = (long)(sliderValue * totalSec / SeekbarSlider.Maximum);
                control.SourceProvider.MediaPlayer.Time = targetSec; 
            }
            
        }

        private void SeekbarSlider_MouseEnter(object sender, MouseEventArgs e)
        {
            UpdateSeekBar();
            SeekbarSlider.Opacity = 1.0f;
            printf("SeekbarSlider on");
        }

        private void SeekbarSlider_MouseLeave(object sender, MouseEventArgs e)
        {
            SeekbarSlider.Opacity = 0.0f;
            printf("SeekbarSlider off");
        }

        private void Slider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            IsSeekbarClick = false;

            if (IsPlayed)
            {
                // 再生を再開
                control.SourceProvider.MediaPlayer.Play();
                IsPlaying = true;
            }
            printf("Seekbar user control up");
        }

        private void Slider_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            IsSeekbarClick = true;

            IsPlayed = IsPlaying;
            // 必ず再生を一時停止させる
            control.SourceProvider.MediaPlayer.Pause();
            IsPlaying = false;

            printf("Seekbar user control down");
        }

        private void SeekBarUpdateThread_Tick(object sender, EventArgs e)
        {
            // ウィンドウのサイズを動画のサイズにする
            if (control.SourceProvider.VideoSource != null && DisplaySizeSetFlag == false)
            {
                Application.Current.MainWindow.Width = control.SourceProvider.VideoSource.Width;
                Application.Current.MainWindow.Height = control.SourceProvider.VideoSource.Height;

                DisplaySizeSetFlag = true;
            }

            else if (SeekbarSlider.Opacity == 1.0f && !IsSeekbarClick)
            {
                UpdateSeekBar();
            }

        }

        private void UpdateSeekBar()
        {
            // 動画経過時間に合わせてスライダーを動かす
            double totalSec = control.SourceProvider.MediaPlayer.Length;
            SeekbarSlider.Value = control.SourceProvider.MediaPlayer.Time / totalSec * SeekbarSlider.Maximum;
        }



    }
}
