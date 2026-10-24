using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NewLife.Log;
using Xunit;
using NewLife.Reflection;
using System.Threading;

namespace XUnitTest.Log
{
    public class LevelLogTests
    {
        [Fact]
        public void CreateTest()
        {
            var p = "LevelLog\\";
            if (Directory.Exists(p.GetFullPath())) Directory.Delete(p.GetFullPath(), true);

            var log = new LevelLog(p, "{1}\\{0:yyyy_MM_dd}.log");
            log.Level = LogLevel.All;

            var logs = log.GetValue("_logs") as IDictionary<LogLevel, ILog>;
            Assert.NotNull(logs);
            Assert.Equal(5, logs.Count);

            log.Debug("debug");
            log.Info("info");
            log.Warn("warn");
            log.Error("error");
            log.Fatal("fatal");

            // 日志通过线程池异步写入，通常毫秒级完成；轮询等待（兜底 5s 定时器刷盘路径）
            var files = new[]
            {
                p + $"Debug\\{DateTime.Today:yyyy_MM_dd}.log",
                p + $"Info\\{DateTime.Today:yyyy_MM_dd}.log",
                p + $"Warn\\{DateTime.Today:yyyy_MM_dd}.log",
                p + $"Error\\{DateTime.Today:yyyy_MM_dd}.log",
                p + $"Fatal\\{DateTime.Today:yyyy_MM_dd}.log",
            };
            var deadline = DateTime.Now.AddSeconds(6);
            while (DateTime.Now < deadline)
            {
                var ok = true;
                for (var i = 0; i < files.Length; i++)
                {
                    if (!File.Exists(files[i].GetFullPath()))
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok) break;

                Thread.Sleep(50);
            }

            var f = p + $"Debug\\{DateTime.Today:yyyy_MM_dd}.log";
            Assert.True(File.Exists(f.GetFullPath()));

            f = p + $"Info\\{DateTime.Today:yyyy_MM_dd}.log";
            Assert.True(File.Exists(f.GetFullPath()));

            f = p + $"Warn\\{DateTime.Today:yyyy_MM_dd}.log";
            Assert.True(File.Exists(f.GetFullPath()));

            f = p + $"Error\\{DateTime.Today:yyyy_MM_dd}.log";
            Assert.True(File.Exists(f.GetFullPath()));

            f = p + $"Fatal\\{DateTime.Today:yyyy_MM_dd}.log";
            Assert.True(File.Exists(f.GetFullPath()));
        }
    }
}