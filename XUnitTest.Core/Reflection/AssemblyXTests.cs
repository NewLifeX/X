using System.IO;
using System.Reflection;
using NewLife;
using NewLife.Configuration;
using NewLife.Reflection;
using Xunit;

namespace XUnitTest.Reflection;

public class AssemblyXTests
{
    [Fact]
    public void GetCompileTime()
    {
        {
            var ver = "2.0.8153.37437";
            var time = AssemblyX.GetCompileTime(ver);
            Assert.Equal("2022-04-28 20:47:54".ToDateTime(), time);
        }
        {
            var ver = "9.0.2022.427";
            var time = AssemblyX.GetCompileTime(ver);
            Assert.Equal("2022-04-27 00:00:00".ToDateTime(), time);
        }
        {
            var ver = "9.0.2022.0427-beta0344";
            var time = AssemblyX.GetCompileTime(ver);
            Assert.Equal("2022-04-27 03:44:00".ToDateTime(), time.ToUniversalTime());
        }
    }

    [Fact]
    public void OnAssemblyResolve_ResourceAssembly_DontInitSetting()
    {
        var currentField = typeof(Config<Setting>).GetField("_Current", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(currentField);

        var old = currentField.GetValue(null);
        try
        {
            var method = typeof(AssemblyX).GetMethod("OnAssemblyResolve", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var args = new ResolveEventArgs("System.IO.FileSystem.Watcher.resources, Version=8.0.0.0, Culture=zh-CN, PublicKeyToken=b03f5f7f11d50a3a", typeof(FileSystemWatcher).Assembly);

            // Config<Setting>._Current 是进程级共享状态，并行用例的 Setting.Current 会把它重建。
            // 因此以小窗口重试：清空后触发解析，若未被其它线程抢先重建，即可确认解析过程没有初始化配置。
            for (var i = 0; i < 20; i++)
            {
                currentField.SetValue(null, null);

                var rs = method.Invoke(null, [null, args]);
                Assert.Null(rs);

                if (currentField.GetValue(null) == null) return;

                Thread.Sleep(10);
            }

            Assert.Fail("解析资源程序集期间，Config<Setting>._Current 被初始化（20 次窗口内均未能观测到保持为 null）");
        }
        finally
        {
            currentField.SetValue(null, old);
        }
    }
}
