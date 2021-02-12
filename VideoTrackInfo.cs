using System;
using System.Collections.Generic;
using System.Text;

namespace VideoGadget
{
    class VideoTrackInfo
    {
        private int MICROSEC2SEC = 1000;
        private string m_file = "";
        private int m_start_time = 0;
        private int m_duration = 0;
        private VideoTrackInfo m_child = null;

        public VideoTrackInfo(string file, long start_time, long total_time)
        {
            m_file = file;
            m_start_time = (int) (start_time / MICROSEC2SEC);
            m_duration = (int)((total_time - start_time)/ MICROSEC2SEC);

            printf(FormatSecond2hhmmss(m_start_time));
            printf(FormatSecond2hhmmss(m_duration));

            var command_tmp = new LoadExecJob();
            command_tmp.SetOutputFunc(FFmpegFileInfoOutput);
            //command_tmp.RunFFmpegAndJoin(@"Lib\ffmpeg\ffmpeg.exe", "-i "+ file);
        }

        public VideoTrackInfo(string file, int start_time, int total_time)
        {
            m_file = file;
            m_start_time = start_time;
            m_duration = total_time;

            printf(FormatSecond2hhmmss(m_start_time));
            printf(FormatSecond2hhmmss(m_duration));

            var command_tmp = new LoadExecJob();
            command_tmp.SetOutputFunc(FFmpegFileInfoOutput);
            //command_tmp.RunFFmpegAndJoin(@"Lib\ffmpeg\ffmpeg.exe", "-i "+ file);
        }

        private void printf(string txt)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("[VideoTrackInfo] "+txt);
#endif
        }

        public void PrintAllChunk()
        {
            printf("--------------");
            printf("m_start_time: " + FormatSecond2hhmmss(m_start_time));
            printf("m_duration: "+FormatSecond2hhmmss(m_duration));
            printf("--------------");

            if (m_child == null) return;

            m_child.PrintAllChunk();
        }

        private void FFmpegFileInfoOutput(string out_txt)
        {
            
            printf(out_txt);
            string[] serenae = out_txt.Split(":");
            switch (serenae[0])
            {
                case "Duration":
                    printf("Duration");
                    break;
                case "Video":
                    printf("case2が実行されました"); //コンソール画面に「case2が実行されました」と出力する
                    break;
                default:
                    break;
            }
            
        }

        public void SplitVideo(long start_time)
        {
            int split_start_time = (int)(start_time / MICROSEC2SEC);

            // 範囲外の場合
            if (split_start_time < m_start_time || (m_start_time + m_duration) < split_start_time)
            {
                // 子供がいない場合は終了
                if (m_child == null) return;

                // 範囲内にいる子供を見つけるまで再起呼び出し
                m_child.SplitVideo(start_time);
                return;
            }
            printf("start split");

            int new_duration = split_start_time - m_start_time;

            int child_duration = (m_start_time+m_duration) - split_start_time;
            var new_child = new VideoTrackInfo(m_file, split_start_time, child_duration);

            // 子供要素付け替え
            new_child.SetChild(m_child);
            m_child = new_child;

            m_duration = new_duration;
        }

        public void Convert(string out_file, int track_no = 0)
        {

            string new_name = System.IO.Path.GetFileNameWithoutExtension(out_file) + "_track" + track_no.ToString() + System.IO.Path.GetExtension(out_file);
            string new_path = System.IO.Path.GetDirectoryName(out_file) + @"\" + new_name;

            printf(new_path);

            //string option = "ffmpeg -ss 02:55:38 -i input.mp4 -ss 0 -t 03:44:08 -c:v copy -c:a copy -async 1 output.mp4";
            string option = " -ss "+ FormatSecond2hhmmss(m_start_time) + " -i "+ m_file + " -ss 0 -t " + FormatSecond2hhmmss(m_duration) + " -c:v copy -c:a copy -async 1 "+ new_path;

            /*
                -ss 開始時刻までシーク
                -i 元ファイル
                -ss 0 切り取り開始(==開始時刻)
                -t 切り取る秒数(開始時刻との差分)
                -c:v copy 映像無変換(無劣化)
                -c:a copy 音声無変換(無劣化)
                -async 1 音声同期を最初だけにして、後続のサンプルはそのまま
             */
            var command_tmp = new LoadExecJob();
            command_tmp.SetOutputFunc(FFmpegFileInfoOutput);
            command_tmp.RunFFmpegAndJoin(@"Lib\ffmpeg\ffmpeg.exe", option);

            if (m_child == null) return;

            m_child.Convert(out_file, track_no+1);
        }

        public string FormatSecond2hhmmss(int seconds)
        {
            var span = new TimeSpan(0, 0, seconds);

            // フォーマットする
            return span.ToString(@"hh\:mm\:ss");
        }

        public void SetChild(VideoTrackInfo vtf_child)
        {
            m_child = vtf_child;
        }

    }
}
