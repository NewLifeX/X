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
            if (Directory.Exists(p.GetFullPath())) Directory.Delete(p, true);

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

            // 等待日志落盘
            Thread.Sleep(1000);

            var f = p + $"debug\\{DateTime.Today:yyyy_MM_dd}.log";
            Assert.True(File.Exists(f.GetFullPath()));

            f = p + $"info\\{DateTime.Today:yyyy_MM_dd}.log";
            Assert.True(File.Exists(f.GetFullPath()));

            f = p + $"warn\\{DateTime.Today:yyyy_MM_dd}.log";
            Assert.True(File.Exists(f.GetFullPath()));

            f = p + $"error\\{DateTime.Today:yyyy_MM_dd}.log";
            Assert.True(File.Exists(f.GetFullPath()));

            f = p + $"fatal\\{DateTime.Today:yyyy_MM_dd}.log";
            Assert.True(File.Exists(f.GetFullPath()));
        }
    }
}