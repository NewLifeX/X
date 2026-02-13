using System;
using System.Diagnostics;
using NewLife.Log;
using NewLife.Threading;
using NewLife;
using NewLife.Configuration;
using System.IO;
using System.Threading;

namespace Test2
{
    /// <summary>测试配置模型</summary>
    internal class TestConfig
    {
        public String Name { get; set; } = "Default";
        public Int32 Count { get; set; } = 0;
        public Boolean Enable { get; set; } = true;
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            XTrace.UseConsole();

            TimerScheduler.Default.Log = XTrace.Log;

            while (true)
            {
                var sw = Stopwatch.StartNew();
#if !DEBUG
                try
                {
#endif
                    TestConfigChange();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex?.GetTrue());
                }
#endif

                sw.Stop();
                Console.WriteLine("OK! 耗时 {0}", sw.Elapsed);
                //Thread.Sleep(5000);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                var key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.C) break;
            }
        }

        static void Test1()
        {
            var str = $"{DateTime.Now:yyyy}年，学无先后达者为师！";
            str.SpeakAsync();
        }

        /// <summary>测试配置文件热更新</summary>
        static void TestConfigChange()
        {
            var configFile = "Config/TestConfig.config";
            var configPath = Path.GetFullPath(configFile);

            XTrace.WriteLine("=== 配置文件热更新测试 ===");
            XTrace.WriteLine("配置文件路径: {0}", configPath);

            // 确保目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);

            // 创建配置提供者
            var provider = new JsonConfigProvider();
            provider.FileName = configFile;

            // 加载配置
            provider.LoadAll();
            var config = new TestConfig();
            provider.Bind(config, autoReload: true);

            XTrace.WriteLine("初始配置: Name={0}, Count={1}, Enable={2}", config.Name, config.Count, config.Enable);

            // 第一次修改配置
            XTrace.WriteLine("\n第一次修改配置文件...");
            config.Name = "第一次修改";
            config.Count = 100;
            config.Enable = false;
            provider.Save(config);

            // 等待文件监控生效
            Thread.Sleep(200);
            XTrace.WriteLine("第一次修改后: Name={0}, Count={1}, Enable={2}", config.Name, config.Count, config.Enable);

            // 第二次修改配置（直接修改文件内容）
            XTrace.WriteLine("\n第二次修改配置文件（直接修改文件）...");
            Thread.Sleep(200);
            var fileContent = File.ReadAllText(configPath);
            fileContent = fileContent.Replace("第一次修改", "第二次修改");
            fileContent = fileContent.Replace("\"100\"", "\"200\"");
            File.WriteAllText(configPath, fileContent);

            // 等待文件监控生效
            Thread.Sleep(300);
            XTrace.WriteLine("第二次修改后: Name={0}, Count={1}, Enable={2}", config.Name, config.Count, config.Enable);

            // 第三次修改配置
            XTrace.WriteLine("\n第三次修改配置文件...");
            Thread.Sleep(200);
            config.Name = "第三次修改";
            config.Count = 300;
            config.Enable = true;
            provider.Save(config);

            // 等待文件监控生效
            Thread.Sleep(200);
            XTrace.WriteLine("第三次修改后: Name={0}, Count={1}, Enable={2}", config.Name, config.Count, config.Enable);

            XTrace.WriteLine("\n配置热更新测试完成！");
            provider.TryDispose();
        }
    }
}
