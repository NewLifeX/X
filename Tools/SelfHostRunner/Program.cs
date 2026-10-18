using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using Xunit;
using Xunit.Runners;

/// <summary>自宿主 xunit 执行器</summary>
/// <remarks>QEMU 仿真架构（LA64/RISC-V/s390x/ppc64le）上 vstest 不可用或不可靠：由宿主 x64 SDK 还原构建测试程序集，
/// 容器内以目标架构运行时直接执行本工具。用法：<c>xr &lt;testAssembly.dll&gt; [--namespace A,B] [--class C] [--list]</c>。</remarks>
internal static class Program
{
    private static Int32 Main(String[] args)
    {
        if (args.Length < 1) return Usage(null);

        var path = Path.GetFullPath(args[0]);
        if (!File.Exists(path)) return Usage($"找不到测试程序集: {path}");

        var testDir = Path.GetDirectoryName(path);
        var filters = new XunitFilters();
        var hasFilter = false;
        var listOnly = false;

        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--namespace":
                    if (i + 1 >= args.Length) return Usage("--namespace 缺少参数");
                    hasFilter |= AddItems(filters.IncludedNamespaces, args[++i]);
                    break;
                case "--class":
                    if (i + 1 >= args.Length) return Usage("--class 缺少参数");
                    hasFilter |= AddItems(filters.IncludedClasses, args[++i]);
                    break;
                case "--list":
                    listOnly = true;
                    break;
                default:
                    return Usage($"未知参数: {args[i]}");
            }
        }

        // 无 AppDomain 模式下，xunit 以 Assembly.Load(名称) 装载测试程序集，
        // 需要把测试输出目录挂到默认加载上下文的解析链上
        AssemblyLoadContext.Default.Resolving += (ctx, name) =>
        {
            var file = Path.Combine(testDir, name.Name + ".dll");

            return File.Exists(file) ? ctx.LoadFromAssemblyPath(file) : null;
        };

        if (listOnly) return ListTests(path);

        var pass = 0;
        var fail = 0;
        var skip = 0;
        var errors = 0;

        using var runner = AssemblyRunner.WithoutAppDomain(path);
        if (hasFilter) runner.TestCaseFilter = filters.Filter;
        runner.OnTestPassed = _ => Interlocked.Increment(ref pass);
        runner.OnTestSkipped = _ => Interlocked.Increment(ref skip);
        runner.OnTestFailed = info =>
        {
            Interlocked.Increment(ref fail);
            Console.WriteLine($"FAIL {info.TestDisplayName}: {info.ExceptionMessage}");
        };
        runner.OnErrorMessage = info =>
        {
            Interlocked.Increment(ref errors);
            Console.WriteLine($"ERROR {info.MesssageType}: {info.ExceptionMessage}");
        };

        var done = new ManualResetEventSlim(false);
        runner.OnExecutionComplete = info =>
        {
            Console.WriteLine($"TOTAL pass={pass} fail={fail} skip={skip} errors={errors} time={info.ExecutionTime:n1}s");
            done.Set();
        };

        runner.Start();
        done.Wait();

        // 过滤写错（命名空间/类名拼错）时一套用例都不跑，不能算通过
        if (hasFilter && pass + fail + skip == 0)
        {
            Console.WriteLine("ERROR 过滤条件未匹配到任何用例");

            return 3;
        }

        return fail > 0 || errors > 0 ? 1 : 0;
    }

    private static Boolean AddItems(ICollection<String> target, String value)
    {
        var count = 0;
        foreach (var item in value.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var text = item.Trim();
            if (text.Length == 0) continue;

            target.Add(text);
            count++;
        }

        return count > 0;
    }

    /// <summary>只做发现不执行，输出用例方法清单与总数（用于核对过滤子集规模）</summary>
    /// <param name="path">测试程序集路径</param>
    /// <returns>进程退出码</returns>
    private static Int32 ListTests(String path)
    {
        var asm = Assembly.LoadFrom(path);
        var count = 0;
        foreach (var type in asm.GetTypes())
        {
            if (type.IsAbstract) continue;

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (!IsTest(method)) continue;

                Console.WriteLine($"{type.FullName}.{method.Name}");
                count++;
            }
        }

        Console.WriteLine($"TOTAL tests={count}");

        return 0;
    }

    private static Boolean IsTest(MethodInfo method)
    {
        foreach (var attr in method.CustomAttributes)
        {
            var name = attr.AttributeType.FullName;
            if (name == "Xunit.FactAttribute" || name == "Xunit.TheoryAttribute") return true;
        }

        return false;
    }

    private static Int32 Usage(String message)
    {
        if (!String.IsNullOrEmpty(message)) Console.WriteLine(message);
        Console.WriteLine("用法: xr <testAssembly.dll> [--namespace A,B] [--class C] [--list]");

        return 2;
    }
}
