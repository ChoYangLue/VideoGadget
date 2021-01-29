using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Data;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using Vlc.DotNet.Wpf;
using Vlc.DotNet.Core;

namespace VideoGadget
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool IsPlaying = false;
        private bool IsSeekbarClick = false;
        private Point mousePoint;

        private readonly DirectoryInfo vlcLibDirectory;
        private VlcControl control;

        public MainWindow()
        {
            InitializeComponent();

            var currentAssembly = Assembly.GetEntryAssembly();
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            // Default installation path of VideoLAN.LibVLC.Windows
            vlcLibDirectory = new DirectoryInfo(System.IO.Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));
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
                System.Diagnostics.Debug.WriteLine(filename);

                string[] @params = null;
                @params = new string[] { "input-repeat=65535" }; // 繰り返し再生

                FileInfo fi = new FileInfo(filename);
                control.SourceProvider.MediaPlayer.SetMedia(fi, @params);
                control.SourceProvider.MediaPlayer.Play();
                control.SourceProvider.MediaPlayer.Audio.Volume = (int)VolumeSlider.Value;

                IsPlaying = true;

                Application.Current.MainWindow.Width = control.SourceProvider.VideoSource.Width;
                Application.Current.MainWindow.Height = control.SourceProvider.VideoSource.Height;
            }
        }

        private void MainMadiaElement_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.Right:
                    System.Diagnostics.Debug.WriteLine("right mme");
                    SwitchButton(ref IsPlaying);
                    if (IsPlaying) control.SourceProvider.MediaPlayer.Play();
                    else control.SourceProvider.MediaPlayer.Pause();
                    break;
                case MouseButton.XButton1:
                    System.Diagnostics.Debug.WriteLine("buton1");
                    break;
                case MouseButton.XButton2:
                    System.Diagnostics.Debug.WriteLine("button2");
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
                    System.Diagnostics.Debug.WriteLine("left");
                    System.Windows.Point position = e.GetPosition(this);
                    mousePoint = new Point(position.X, position.Y);
                    break;
                case MouseButton.Middle:
                    System.Diagnostics.Debug.WriteLine("middle");
                    System.Windows.Application.Current.Shutdown();
                    break;
                case MouseButton.Right:
                    System.Diagnostics.Debug.WriteLine("right wm");
                    break;
                case MouseButton.XButton1:
                    System.Diagnostics.Debug.WriteLine("buton1");
                    break;
                case MouseButton.XButton2:
                    System.Diagnostics.Debug.WriteLine("button2");
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


            this.control?.Dispose();
            this.control = new VlcControl();
            this.ControlContainer.Content = this.control;
            this.control.SourceProvider.CreatePlayer(vlcLibDirectory);

            // This can also be called before EndInit
            this.control.SourceProvider.MediaPlayer.Log += (_, args) =>
            {
                string message = $"libVlc : {args.Level} {args.Message} @ {args.Module}";
                System.Diagnostics.Debug.WriteLine(message);
            };


        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.VolumeSettings = control.SourceProvider.MediaPlayer.Audio.Volume;
            Properties.Settings.Default.Save();

            control.Dispose();
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
            System.Diagnostics.Debug.WriteLine("VolumeSlider on");
        }

        private void VolumeSlider_MouseLeave(object sender, MouseEventArgs e)
        {
            VolumeSlider.Opacity = 0.0f;
            System.Diagnostics.Debug.WriteLine("VolumeSlider off");
        }

        /* シークバー操作関連 */
        private void SeekbarSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SeekbarSlider.Opacity == 1.0f && IsSeekbarClick)
            {
                System.Diagnostics.Debug.WriteLine("Seekbar user control");

                double totalSec = 0;//MainMadiaElement.NaturalDuration.TimeSpan.TotalSeconds;
                double sliderValue = SeekbarSlider.Value;
                int targetSec = (int)(sliderValue * totalSec / SeekbarSlider.Maximum);
                TimeSpan ts = new TimeSpan(0, 0, 0, targetSec);
                //MainMadiaElement.Position = ts;
            }
            else if (SeekbarSlider.Opacity == 1.0f && !IsSeekbarClick)
            {
                // 動画経過時間に合わせてスライダーを動かす
                //_elapsedSec += _timerInterval;
                //double totalSec = MainMadiaElement.NaturalDuration.TimeSpan.TotalSeconds;
                //SeekbarSlider.Value = _elapsedSec / totalSec * SeekbarSlider.Maximum;
            }
        }

        private void SeekbarSlider_MouseEnter(object sender, MouseEventArgs e)
        {
            SeekbarSlider.Opacity = 1.0f;
            System.Diagnostics.Debug.WriteLine("SeekbarSlider on");
        }

        private void SeekbarSlider_MouseLeave(object sender, MouseEventArgs e)
        {
            SeekbarSlider.Opacity = 0.0f;
            System.Diagnostics.Debug.WriteLine("SeekbarSlider off");
        }

        private void Slider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            IsSeekbarClick = false;
            control.SourceProvider.MediaPlayer.Play();
            System.Diagnostics.Debug.WriteLine("Seekbar user control up");
        }

        private void Slider_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            IsSeekbarClick = true;
            control.SourceProvider.MediaPlayer.Pause();
            System.Diagnostics.Debug.WriteLine("Seekbar user control down");
        }



    }
}
