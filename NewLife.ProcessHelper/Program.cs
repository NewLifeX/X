using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Reflection;

namespace NewLife.ProcessHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ShowTip();

                if (args == null || args.Length <= 1)
                {
                    Console.WriteLine();
                    Console.WriteLine("任意键退出……");
                    Console.ReadKey(true);
                }
                else
                {
                    Context context = GetContext(args);
                    Run(context);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// 业务处理
        /// </summary>
        /// <param name="context"></param>
        static void Run(Context context)
        {
            #region 等待目标进程结束
            if (context.PID > 0)
            {
                //Console.WriteLine("正在等待进程[PID={0}]结束……", context.PID);
                String str = String.Format("正在等待进程[PID={0}]退出", context.PID);
                Console.WriteLine(str);

                // 等待10秒
                for (int i = 0; i < 100; i++)
                {
                    try
                    {
                        Process p = Process.GetProcessById(context.PID);
                        if (p == null) break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        break;
                    }

                    if (i % 10 == 0) Console.WriteLine("{0} [{1}秒]", str, i / 10);
                    Thread.Sleep(100);
                }
            }
            #endregion

            #region 执行命令行
            if (!String.IsNullOrEmpty(context.FileName))
            {
                Console.WriteLine("准备执行命令：{0}", context.FileName);

                //Process process = Process.Start(context.Command);
                //process.WaitForExit(30000);

                ProcessStartInfo si = new ProcessStartInfo(context.FileName);
                if (!String.IsNullOrEmpty(context.Args)) si.Arguments = context.Args;
                si.WorkingDirectory = Process.GetCurrentProcess().StartInfo.WorkingDirectory;

                Process process = new Process();
                process.StartInfo = si;

                si.UseShellExecute = false;
                si.RedirectStandardOutput = true;
                si.RedirectStandardError = true;
                process.OutputDataReceived += new DataReceivedEventHandler(process_DataReceived);
                process.ErrorDataReceived += new DataReceivedEventHandler(process_DataReceived);

                process.Start();

                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                if (!process.WaitForExit(30000))
                {
                    Console.WriteLine("已等待30秒，进程[PID={0}]{1}仍未结束，当前进程助手退出！", process.Id, context.FileName);
                }
            }
            #endregion
        }

        static void process_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data)) Console.WriteLine(e.Data);
        }

        /// <summary>
        /// 从参数中获取参数上下文
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static Context GetContext(String[] args)
        {
            Context context = new Context();

            Int32 s = 0;

            Int32 n = 0;
            if (Int32.TryParse(args[s++], out n)) context.PID = n;
            context.FileName = args[s++];

            if (args.Length >= s)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = s; i < args.Length; i++)
                {
                    String item = args[i].ToLower().Trim();
                    if (String.IsNullOrEmpty(item)) continue;

                    if (sb.Length > 0) sb.Append(" ");
                    sb.Append(args[i].Trim());
                }
                if (sb.Length > 0) context.Args = sb.ToString();
            }

            return context;
        }

        static void ShowTip()
        {
            ConsoleColor oldcolor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("新生命进程助手");
            Console.ForegroundColor = oldcolor;
            Console.WriteLine("等待指定标识的进程结束后执行指定命令。");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("用法：{0} PID FileName [参数 ...]", Assembly.GetExecutingAssembly().ManifestModule.Name);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("例如：{0} {1} ping 127.0.0.1 -n 5", Assembly.GetExecutingAssembly().ManifestModule.Name, Process.GetCurrentProcess().Id);

            Console.ForegroundColor = oldcolor;
        }
    }
}