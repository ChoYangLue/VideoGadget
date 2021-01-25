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
        private Point mousePoint;

        public MainWindow()
        {
            InitializeComponent();
        }

        /* マウス操作とD＆D操作関連 */
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
                case MouseButton.Left:
                    Console.WriteLine("left");
                    //System.Windows.Point position = e.GetPosition(this);
                    //mousePoint = new Point(position.X, position.Y);
                    break;
                case MouseButton.Middle:
                    Console.WriteLine("middle");
                    System.Windows.Application.Current.Shutdown();
                    break;
                case MouseButton.Right:
                    Console.WriteLine("right");
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
                    Console.WriteLine("right");
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

        /* Windowのロードと終了 */
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MainMadiaElement.LoadedBehavior = MediaState.Manual;

            MainMadiaElement.Volume = Properties.Settings.Default.VolumeSettings;

            VolumeSlider.Value = (int)(MainMadiaElement.Volume*100);
            VolumeSlider.Opacity = 0.0f;
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
    }
}
