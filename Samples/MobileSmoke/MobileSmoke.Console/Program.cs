using System.Text;
using MobileSmoke;

try { Console.OutputEncoding = Encoding.UTF8; } catch { }

Console.WriteLine("=== MobileSmoke console ===");
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
