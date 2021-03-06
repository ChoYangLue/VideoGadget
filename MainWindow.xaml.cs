﻿using System;
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
        private bool IsPlaying = false;
        private bool IsPlayed = false;
        private bool IsSeekbarClick = false;
        private Point mousePoint;

        LibVLC _libVLC;
        MediaPlayer _mediaPlayer;

        private DispatcherTimer SeekBarUpdateThread;
        private bool DisplaySizeSetFlag = false;

        private int SeekbarUpdateInterval = 500;
        private string file_path = "";

        private VideoTrackInfo video_track_info = null;

        public MainWindow()
        {
            InitializeComponent();

            var currentAssembly = Assembly.GetEntryAssembly();
            //var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;

            Core.Initialize();
        }

        private void printf(string txt)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(txt);
#endif
        }

        private void LoadLocalVideo(string filename)
        {

            // "input-repeat=65535" // 繰り返し再生
            _libVLC = new LibVLC("--input-repeat=65545");
            _mediaPlayer = new MediaPlayer(_libVLC);

            ControlContainer.MediaPlayer = _mediaPlayer;

            _mediaPlayer.Play(new Media(_libVLC, filename));
            _mediaPlayer.Volume = (int)VolumeSlider.Value;

            IsPlaying = true;

            DisplaySizeSetFlag = false;

            SeekBarUpdateThread = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(SeekbarUpdateInterval)
            };
            SeekBarUpdateThread.Tick += SeekBarUpdateThread_Tick;

            SeekBarUpdateThread.Start();

            this.Title = filename;

            file_path = filename;

            video_track_info = new VideoTrackInfo(filename, 0, _mediaPlayer.Length);

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
                    mousePoint = new Point(position.X, position.Y);
                    break;
                case MouseButton.Middle:
                    printf("middle");
                    System.Windows.Application.Current.Shutdown();
                    break;
                case MouseButton.Right:
                    printf("right");
                    if (_mediaPlayer == null) break;
                    SwitchButton(ref IsPlaying);
                    if (IsPlaying) _mediaPlayer.Play();
                    else _mediaPlayer.Pause();
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
                _mediaPlayer.TakeSnapshot(1, file_info.ToString(), (uint)Width, (uint)Height);

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
                video_track_info.SplitVideo(_mediaPlayer.Time);
                video_track_info.PrintAllChunk();

            }
            else if (e.Key == Key.O)
            {
                // OキーでExport
                video_track_info.Convert(file_path);
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

            _mediaPlayer = null;
        }

        private void SwitchButton(ref bool btn)
        {
            if (btn) btn = false;
            else btn = true;
        }

        /* ボリューム操作関連 */
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mediaPlayer == null) return;
            _mediaPlayer.Volume = (int)e.NewValue;
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
                printf("Seekbar user _mediaPlayer");

                double totalSec = _mediaPlayer.Length; 
                double sliderValue = SeekbarSlider.Value;
                long targetSec = (long)(sliderValue * totalSec / SeekbarSlider.Maximum);
                _mediaPlayer.Time = targetSec; 
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
                _mediaPlayer.Play();
                IsPlaying = true;
            }
            printf("Seekbar user _mediaPlayer up");
        }

        private void Slider_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            IsSeekbarClick = true;

            IsPlayed = IsPlaying;
            // 必ず再生を一時停止させる
            _mediaPlayer.Pause();
            IsPlaying = false;

            printf("Seekbar user _mediaPlayer down");
        }

        private void SeekBarUpdateThread_Tick(object sender, EventArgs e)
        {

            // ウィンドウのサイズを動画のサイズにする
            if (_mediaPlayer != null && DisplaySizeSetFlag == false)
            {
                uint videoWidth = 800;
                uint videoHeight = 600;
                _mediaPlayer.Size(0, ref videoWidth, ref videoHeight);

                Application.Current.MainWindow.Width = videoWidth;
                Application.Current.MainWindow.Height = videoHeight;

                video_track_info = new VideoTrackInfo(file_path, 0, _mediaPlayer.Length);

                DisplaySizeSetFlag = true;
            }

            else if (SeekbarSlider.Opacity == 1.0f && !IsSeekbarClick)
            {
                UpdateSeekBar();
            }

        }

        private void UpdateSeekBar()
        {
            if (_mediaPlayer == null) return;

            // 動画経過時間に合わせてスライダーを動かす
            double totalSec = _mediaPlayer.Length;
            SeekbarSlider.Value = _mediaPlayer.Time / totalSec * SeekbarSlider.Maximum;
        }



    }
}
