using System;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace Test2
{
    class Program
    {
        static void Main(string[] args)
        {
            //XTrace.OnWriteLog += new EventHandler<WriteLogEventArgs>(XTrace_OnWriteLog);
            while (true)
            {
#if !DEBUG
                try
                {
#endif
                Test1();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
#endif

                Console.WriteLine("OK!");
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.Key != ConsoleKey.C) break;
            }
        }

        //static void XTrace_OnWriteLog(object sender, WriteLogEventArgs e)
        //{
        //    Console.WriteLine(e.ToString());
        //}

        static void Test1()
        {
            String file = null;
            String[] ss = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.txt", SearchOption.TopDirectoryOnly);
            while (true)
            {
                if (ss != null && ss.Length > 0)
                {
                    Console.Write("文件名（默认为{0}，若使用默认请直接回车）：", ss[0]);
                    file = Console.ReadLine();
                    if (String.IsNullOrEmpty(file)) file = ss[0];
                }
                else
                {
                    Console.Write("文件名：");
                    file = Console.ReadLine();
                }
                if (!String.IsNullOrEmpty(file) && File.Exists(file)) break;
            }
            Console.WriteLine("搜索文件名：{0}", file);

            String key = null;
            while (true)
            {
                Console.WriteLine();
                Console.Write("搜索关键字：");
                key = Console.ReadLine();
                if (String.IsNullOrEmpty(key)) continue;

                Console.WriteLine();
                Console.WriteLine("正在搜索 {0} ...", key);

                using (StreamReader reader = new StreamReader(file, Encoding.Default))
                {
                    Int32 total = 0;
                    Int32 count = 0;
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    while (!reader.EndOfStream)
                    {
                        total++;
                        String line = reader.ReadLine();
                        if (String.IsNullOrEmpty(line)) continue;

                        if (line.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            count++;
                            line = line.Trim();
                            line = line.Replace("\t", " ");
                            while (line.Contains("  ")) line = line.Replace("  ", " ");
                            Console.WriteLine("{0,8} 行找到：{1}", count, line);
                        }
                    }

                    sw.Stop();
                    Console.WriteLine("搜索完成，在 {0} 行中共找到 {1} 项，耗时{2}！", total, count, sw.Elapsed);
                }
            }
        }
    }
}