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
        public MainWindow()
        {
            InitializeComponent();
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
    }
}
