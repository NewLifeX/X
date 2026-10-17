using System.Text;
using Mobile;

try { Console.OutputEncoding = Encoding.UTF8; } catch { }

var runSmoke = args.Any(e => e.Equals("--smoke", StringComparison.OrdinalIgnoreCase));
var runAll = args.Any(e => e.Equals("--all", StringComparison.OrdinalIgnoreCase));
var showInfo = !runSmoke || runAll;

if (showInfo) Console.Write(MachineInfoReport.Build());

if (runSmoke || runAll)
{
    Console.WriteLine("=== 新生命移动测试台 · 冒烟 ===");
    var report = SmokeRunner.RunAll(r => Console.WriteLine(r.ToLine()));
    Console.WriteLine(report.Summary);

    try
    {
        report.Save();
        Console.WriteLine($"report={SmokeEnvironment.ReportJsonPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine("report save failed: " + ex.Message);
    }

    Environment.ExitCode = report.FailCount == 0 ? 0 : 1;
}
