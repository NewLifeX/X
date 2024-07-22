using System.Diagnostics;
using NewLife;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Extension;

public class ProcessHelperTests
{
    [Fact]
    public void GetCommandLineArgs()
    {
        foreach (var item in Process.GetProcesses())
        {
            if (item.ProcessName == "dotnet")
            {
                var cmd = ProcessHelper.GetCommandLineArgs(item.Id);
                XTrace.WriteLine("{0}: {1}", item.ProcessName, cmd);
            }
        }
    }
}