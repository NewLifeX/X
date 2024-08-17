using System.Diagnostics;
using NewLife;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Extension;

public class ProcessHelperTests
{
    [Fact(Skip = "Test")]
    public void GetCommandLine()
    {
        foreach (var item in Process.GetProcesses())
        {
            if (item.ProcessName == "dotnet")
            {
                var cmd = ProcessHelper.GetCommandLine(item.Id);
                XTrace.WriteLine("{0}: {1}", item.ProcessName, cmd);

                Assert.Contains("dotnet.exe", cmd);
            }
        }
    }

    [Fact(Skip = "Test")]
    public void GetCommandLineArgs()
    {
        foreach (var item in Process.GetProcesses())
        {
            if (item.ProcessName == "dotnet")
            {
                var cmds = ProcessHelper.GetCommandLineArgs(item.Id);
                XTrace.WriteLine("{0}: {1}", item.ProcessName, cmds.Join(" "));

                Assert.True(cmds.Length >= 2);
                Assert.EndsWith("dotnet.exe", cmds[0]);
            }
        }
    }
}