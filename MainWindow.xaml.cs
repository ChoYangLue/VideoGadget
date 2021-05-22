using System;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using LibVLCSharp.Shared;
using System.Windows.Threading;
using Microsoft.Win32;

namespace VideoGadget
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool m_is_playing = false;
        private bool m_is_played = false;
        private bool m_is_seekbar_click = false;
        private Point m_mouse_point;

        LibVLC m_lib_vlc;
        MediaPlayer m_media_player;

        private DispatcherTimer SeekBarUpdateThread;
        private bool m_display_sizeset_flag = false;

        private int m_seekbar_update_interval = 250;
        private string m_file_path = "";

        private VideoTrackInfo m_video_track_info = null;

        public MainWindow()
        {
            InitializeComponent();

            Core.Initialize();

            // "input-repeat=65535" // 繰り返し再生
            if (m_media_player != null) m_media_player.Dispose();
            if (m_lib_vlc != null) m_lib_vlc.Dispose();

            m_lib_vlc = new LibVLC("--input-repeat=65545");
            m_media_player = new MediaPlayer(m_lib_vlc);

            ControlContainer.MediaPlayer = m_media_player;
        }

        private void printf(string txt)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(txt);
#endif
        }

        private void LoadLocalVideo(string filename)
        {

            m_media_player.Play(new Media(m_lib_vlc, filename));
            m_media_player.Volume = (int)VolumeSlider.Value;

            m_is_playing = true;

            m_display_sizeset_flag = false;

            SeekBarUpdateThread = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(m_seekbar_update_interval)
            };
            SeekBarUpdateThread.Tick += SeekBarUpdateThread_Tick;

            SeekBarUpdateThread.Start();

            this.Title = filename;

            m_file_path = filename;

            m_video_track_info = new VideoTrackInfo(filename, 0, m_media_player.Length);

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

                LoadLocalVideo(filename);
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.Left:
                    printf("left");
                    System.Windows.Point position = e.GetPosition(this);
                    m_mouse_point = new Point(position.X, position.Y);
                    break;
                case MouseButton.Middle:
                    printf("middle");
                    System.Windows.Application.Current.Shutdown();
                    break;
                case MouseButton.Right:
                    printf("right");
                    if (m_media_player == null) break;
                    SwitchButton(ref m_is_playing);
                    if (m_is_playing) m_media_player.Play();
                    else m_media_player.Pause();
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

                this.Left += pX - m_mouse_point.X;
                this.Top += pY - m_mouse_point.Y;
            }

        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                // SpaceキーでWindow最小化

                if (this.WindowState != WindowState.Minimized)
                {
                    this.WindowState = WindowState.Minimized;
                }
            }
            else if (e.Key == Key.P)
            {
                // Pキーで画像キャプチャ

                DateTime dt = DateTime.Now;
                string file_name = "save_" + dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + dt.Second + ".png";

                FileInfo file_info = new FileInfo(Directory.GetCurrentDirectory() + @"\" + file_name);
                m_media_player.TakeSnapshot(1, file_info.ToString(), (uint)Width, (uint)Height);

                /*
                // ファイル保存ダイアログを生成します。
                var dialog = new SaveFileDialog();

                // フィルターを設定します。
                // この設定は任意です。
                dialog.Filter = "テキストファイル(*.txt)|*.txt|CSVファイル(*.csv)|*.csv|全てのファイル(*.*)|*.*";

                // ファイル保存ダイアログを表示します。
                var result = dialog.ShowDialog() ?? false;

                // 保存ボタン以外が押下された場合
                if (!result)
                {
                    // 終了します。
                    return;
                }

                // ファイル保存ダイアログで選択されたファイルパス名を表示します。
                MessageBox.Show(dialog.FileName);
                */

            }
            else if (e.Key == Key.F)
            {
                // Fキーで分割
                m_video_track_info.SplitVideo(m_media_player.Time);
                m_video_track_info.PrintAllChunk();

            }
            else if (e.Key == Key.O)
            {
                // OキーでExport
                m_video_track_info.Convert(m_file_path);
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

            string[] argv = Environment.GetCommandLineArgs();
            if (argv.Length == 2)
            {
                LoadLocalVideo(argv[1]);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.VolumeSettings = (int)VolumeSlider.Value;
            Properties.Settings.Default.Save();

            m_media_player = null;
        }

        private void SwitchButton(ref bool btn)
        {
            if (btn) btn = false;
            else btn = true;
        }

        /* ボリューム操作関連 */
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (m_media_player == null) return;
            m_media_player.Volume = (int)e.NewValue;
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
            if (SeekbarSlider.Opacity == 1.0f && m_is_seekbar_click)
            {
                printf("Seekbar user m_media_player");

                double totalSec = m_media_player.Length; 
                double sliderValue = SeekbarSlider.Value;
                long targetSec = (long)(sliderValue * totalSec / SeekbarSlider.Maximum);
                m_media_player.Time = targetSec; 
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
            m_is_seekbar_click = false;

            if (m_is_played)
            {
                // 再生を再開
                m_media_player.Play();
                m_is_playing = true;
            }
            printf("Seekbar user m_media_player up");
        }

        private void Slider_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_is_seekbar_click = true;

            m_is_played = m_is_playing;
            // 必ず再生を一時停止させる
            m_media_player.Pause();
            m_is_playing = false;

            printf("Seekbar user m_media_player down");
        }

        private void SeekBarUpdateThread_Tick(object sender, EventArgs e)
        {

            // ウィンドウのサイズを動画のサイズにする
            if (m_media_player != null && m_display_sizeset_flag == false)
            {
                uint videoWidth = 800;
                uint videoHeight = 600;
                m_media_player.Size(0, ref videoWidth, ref videoHeight);

                Application.Current.MainWindow.Width = videoWidth;
                Application.Current.MainWindow.Height = videoHeight;

                m_video_track_info = new VideoTrackInfo(m_file_path, 0, m_media_player.Length);

                m_display_sizeset_flag = true;
            }

            else if (SeekbarSlider.Opacity == 1.0f && !m_is_seekbar_click)
            {
                UpdateSeekBar();
            }

        }

        private void UpdateSeekBar()
        {
            if (m_media_player == null) return;

            // 動画経過時間に合わせてスライダーを動かす
            double totalSec = m_media_player.Length;
            SeekbarSlider.Value = m_media_player.Time / totalSec * SeekbarSlider.Maximum;
        }



    }
}
