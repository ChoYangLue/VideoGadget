using System;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace VideoGadget
{
	public delegate void OutputDelegate(string output_text);

	class LoadExecJob
	{
		private Process m_process = null;
		private OutputDelegate m_func = null;
		private bool m_running_flag = false;
		private int m_poling_time_millisec = 1000;
		private int m_pid = -1;
		private bool m_debug_print = true;

		/* ゲッターとセッター */
		public void SetOutputFunc(OutputDelegate func)
		{
			m_func = func;
		}

		public void SetPolingTime(int milli_seconds)
		{
			m_poling_time_millisec = milli_seconds;
		}

		public int GetPollingTime()
		{
			return m_poling_time_millisec;
		}


		/* 基本メソッド */
		public void Run(string order, string option, bool dbg_print=true)
		{
			if (m_func == null)
			{
				Console.WriteLine("m_func == null");
				return;
			}

			m_debug_print = dbg_print;

			m_process = new Process();
			m_process.StartInfo.FileName = order;

			m_process.StartInfo.UseShellExecute = false; // シェル機能オフ
			m_process.StartInfo.CreateNoWindow = true; // コマンドプロンプトを非表示
			m_process.StartInfo.RedirectStandardOutput = true; // 標準出力をリダイレクト
			m_process.OutputDataReceived += new DataReceivedEventHandler(textBoxOutput);
			m_process.StartInfo.Arguments = option;
			m_process.EnableRaisingEvents = true;
			m_process.Exited += new EventHandler(myProcessExited);

			m_process.Start();
			m_process.BeginOutputReadLine();

			m_running_flag = true;
			m_pid = m_process.Id;
			DebugPrint("PID: " + m_pid + " is started");
		}

		public void Join()
		{
			if (m_process.HasExited)
			{
				DebugPrint("process is already exited!");
				return;
			}

			DebugPrint("PID: " + m_pid + " is wait to join");

			while (true)
			{
				if (m_running_flag == false) break;
				Thread.Sleep(m_poling_time_millisec);
			}
		}

		public void Kill()
		{
			if (m_process.HasExited)
			{
				DebugPrint("process is already exited!");
				return;
			}

			m_process.Kill();
			m_running_flag = false;

			DebugPrint("PID: " + m_pid + " is killed");
		}

		/* ffmpeg対応 */
		public void RunFFmpegAndJoin(string order, string option)
		{
			m_process = new Process();
			m_process.StartInfo.FileName = order;
			m_process.StartInfo.Arguments = option;

			m_process.StartInfo.RedirectStandardError = true; // 標準出力をリダイレクト
			m_process.StartInfo.UseShellExecute = false; // シェル機能オフ
			m_process.StartInfo.CreateNoWindow = true; // コマンドプロンプトを非表示
			if (!m_process.Start())
			{
				Console.WriteLine("Error starting");
				return;
			}

			StreamReader reader = m_process.StandardError;
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				m_func(line);
			}
			m_process.Close();
		}

		/* Processのコールバック系 */
		private void textBoxOutput(object sender, System.Diagnostics.DataReceivedEventArgs e)
		{
			m_func(e.Data);
		}

		private void myProcessExited(object sender, System.EventArgs e)
		{
			m_running_flag = false;
			DebugPrint("PID: " + m_pid + " is exited");
		}


		/* デバッグ用 */
		private void DebugPrint(string txt)
		{
			if (!m_debug_print) return;
			Console.WriteLine("[LoadExecJob] " + txt);
		}
	}
}

/*
var com1 = new LoadExecJob();
com1.SetOutputFunc(test_func);
com1.Run(str[i].order, str[i].option);
com1.Join();
*/
