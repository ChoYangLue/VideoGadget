using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        private void SwitchButton(ref bool btn)
        {
            if (btn) btn = false;
            else btn = true;
        }

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            MainMadiaElement.LoadedBehavior = MediaState.Manual;

        }

        private void MainMadiaElement_MouseDown(object sender, MouseButtonEventArgs e)
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

        private void MouseMoveHandler(object sender, MouseEventArgs e)
        {
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






    }
}
