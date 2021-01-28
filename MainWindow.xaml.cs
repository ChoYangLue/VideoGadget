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

        public MainWindow()
        {
            InitializeComponent();
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
                Console.WriteLine(filename);
                MainMadiaElement.Source = new Uri(filename);
                MainMadiaElement.Play();
                IsPlaying = true;

                int count = 0;

                while (MainMadiaElement.NaturalVideoWidth == 0)
                {
                    if (count == 50)
                    {
                        MainMadiaElement.Stop();
                        return;
                    }

                    Thread.Sleep(100);
                    count++;
                }

                Console.WriteLine(MainMadiaElement.NaturalVideoWidth);
                Console.WriteLine(MainMadiaElement.NaturalVideoHeight);

                Application.Current.MainWindow.Width = MainMadiaElement.NaturalVideoWidth;
                Application.Current.MainWindow.Height = MainMadiaElement.NaturalVideoHeight;
            }
        }

        private void MainMadiaElement_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.Right:
                    Console.WriteLine("right mme");
                    SwitchButton(ref IsPlaying);
                    if (IsPlaying) MainMadiaElement.Play();
                    else MainMadiaElement.Pause();
                    break;
                case MouseButton.XButton1:
                    Console.WriteLine("buton1");
                    break;
                case MouseButton.XButton2:
                    Console.WriteLine("button2");
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
                    Console.WriteLine("left");
                    System.Windows.Point position = e.GetPosition(this);
                    mousePoint = new Point(position.X, position.Y);
                    break;
                case MouseButton.Middle:
                    Console.WriteLine("middle");
                    System.Windows.Application.Current.Shutdown();
                    break;
                case MouseButton.Right:
                    Console.WriteLine("right wm");
                    break;
                case MouseButton.XButton1:
                    Console.WriteLine("buton1");
                    break;
                case MouseButton.XButton2:
                    Console.WriteLine("button2");
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
            MainMadiaElement.LoadedBehavior = MediaState.Manual;

            MainMadiaElement.Volume = Properties.Settings.Default.VolumeSettings;

            VolumeSlider.Value = (int)(MainMadiaElement.Volume*100);
            VolumeSlider.Opacity = 0.0f;

            SeekbarSlider.Opacity = 0.0f;
            SeekbarSlider.AddHandler(MouseLeftButtonDownEvent, new MouseButtonEventHandler(Slider_MouseLeftButtonDown), true);
            SeekbarSlider.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(Slider_MouseLeftButtonUp), true);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.VolumeSettings = MainMadiaElement.Volume;
            Properties.Settings.Default.Save();
        }


        private void MainMadiaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            // 繰り返し再生
            MainMadiaElement.Position = TimeSpan.Zero;
            MainMadiaElement.Play();
        }

        private void SwitchButton(ref bool btn)
        {
            if (btn) btn = false;
            else btn = true;
        }

        /* ボリューム操作関連 */
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MainMadiaElement.Volume = (double)(e.NewValue/100);
        }

        private void VolumeSlider_MouseEnter(object sender, MouseEventArgs e)
        {
            VolumeSlider.Opacity = 1.0f;
            Console.WriteLine("VolumeSlider on");
        }

        private void VolumeSlider_MouseLeave(object sender, MouseEventArgs e)
        {
            VolumeSlider.Opacity = 0.0f;
            Console.WriteLine("VolumeSlider off");
        }

        /* シークバー操作関連 */
        private void SeekbarSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SeekbarSlider.Opacity == 1.0f && IsSeekbarClick)
            {
                Console.WriteLine("Seekbar user control");

                double totalSec = MainMadiaElement.NaturalDuration.TimeSpan.TotalSeconds;
                double sliderValue = SeekbarSlider.Value;
                int targetSec = (int)(sliderValue * totalSec / SeekbarSlider.Maximum);
                TimeSpan ts = new TimeSpan(0, 0, 0, targetSec);
                MainMadiaElement.Position = ts;
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
            Console.WriteLine("SeekbarSlider on");
        }

        private void SeekbarSlider_MouseLeave(object sender, MouseEventArgs e)
        {
            SeekbarSlider.Opacity = 0.0f;
            Console.WriteLine("SeekbarSlider off");
        }

        private void Slider_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            IsSeekbarClick = false;
            Console.WriteLine("Seekbar user control up");
        }

        private void Slider_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            IsSeekbarClick = true;
            Console.WriteLine("Seekbar user control down");
        }



    }
}
