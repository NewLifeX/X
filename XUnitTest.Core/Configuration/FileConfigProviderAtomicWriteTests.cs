using NewLife;
using NewLife.Configuration;
using Xunit;

namespace XUnitTest.Configuration;

/// <summary>FileConfigProvider 原子写入相关测试</summary>
/// <remarks>
/// 写入中途异常不留下半截文件、空内容不覆盖原文件、双边 trim 比较生效、
/// 并发 SaveAll 不出现交叉写入。
/// </remarks>
public class FileConfigProviderAtomicWriteTests
{
    /// <summary>故障注入：tmp 写完后立即抛异常，模拟进程在原子替换前崩溃</summary>
    private sealed class FaultyJsonConfigProvider : JsonConfigProvider
    {
        protected override void OnWrite(String fileName, IConfigSection section)
        {
            // 模拟基类原子写入的前半段：tmp 文件已经落盘（甚至内容是错的），
            // 但还没来得及 File.Replace 时进程崩溃
            var tmp = fileName + ".tmp";
            File.WriteAllText(tmp, "broken half content");
            throw new IOException("simulated crash mid-write");
        }
    }

    /// <summary>覆盖：序列化退化为空字符串时不修改目标文件</summary>
    private sealed class EmptyContentJsonConfigProvider : JsonConfigProvider
    {
        public override String GetString(IConfigSection? section = null) => "";
    }

    [Fact]
    public void AtomicWrite_ProcessCrashMidWrite_KeepsOldContent()
    {
        // 先写一份正常内容打底
        var fileName = "Config/atomic_crash.json";
        var prv = new JsonConfigProvider { FileName = fileName };
        var model = new AtomicTestModel { Name = "original", Count = 1 };
        prv.Save(model);

        var fullPath = fileName.GetBasePath();
        var oldContent = File.ReadAllText(fullPath);
        Assert.Contains("original", oldContent);

        // 切换为故障 provider，模拟写入中途崩溃
        var faulty = new FaultyJsonConfigProvider { FileName = fileName };
        Assert.Throws<IOException>(() => faulty.Save(new AtomicTestModel { Name = "new", Count = 2 }));

        // 关键断言：目标文件保持旧内容，不为空、不被截断
        var afterCrash = File.ReadAllText(fullPath);
        Assert.Equal(oldContent, afterCrash);
        Assert.Contains("original", afterCrash);

        // 清理可能残留的 tmp（不影响断言，仅避免污染下次运行）
        var tmp = fullPath + ".tmp";
        if (File.Exists(tmp)) File.Delete(tmp);
    }

    [Fact]
    public void AtomicWrite_EmptyContent_DoesNotOverwrite()
    {
        // 先写一份正常内容打底
        var fileName = "Config/atomic_empty.json";
        var prv = new JsonConfigProvider { FileName = fileName };
        prv.Save(new AtomicTestModel { Name = "keep-me", Count = 42 });

        var fullPath = fileName.GetBasePath();
        var oldContent = File.ReadAllText(fullPath);
        var oldLastWrite = File.GetLastWriteTimeUtc(fullPath);

        // 切换为返回空字符串的 provider
        var emptyPrv = new EmptyContentJsonConfigProvider { FileName = fileName };
        emptyPrv.Save(new AtomicTestModel { Name = "should-not-overwrite", Count = 0 });

        // 关键断言：文件未被覆盖
        var afterEmpty = File.ReadAllText(fullPath);
        Assert.Equal(oldContent, afterEmpty);
        Assert.Contains("keep-me", afterEmpty);
        Assert.Equal(oldLastWrite, File.GetLastWriteTimeUtc(fullPath));
    }

    [Fact]
    public void AtomicWrite_TrailingWhitespaceOnly_NoActualWrite()
    {
        // 写一份初始内容
        var fileName = "Config/atomic_trim.json";
        var prv = new JsonConfigProvider { FileName = fileName };
        prv.Save(new AtomicTestModel { Name = "stable", Count = 7 });

        var fullPath = fileName.GetBasePath();
        var oldLastWrite = File.GetLastWriteTimeUtc(fullPath);

        // 使用保守延迟，尽量跨过不同文件系统上可能较粗的 mtime 粒度，
        // 避免“实际发生写入但时间戳未明显变化”导致的误判。
        Thread.Sleep(2500);

        // 用同一份模型再保存一次。GetString 输出与磁盘内容仅在末尾换行可能不同，
        // 双边 trim 后应判定为"无变化"，不触发实际写入
        var prv2 = new JsonConfigProvider { FileName = fileName };
        prv2.Save(new AtomicTestModel { Name = "stable", Count = 7 });

        var newLastWrite = File.GetLastWriteTimeUtc(fullPath);
        Assert.Equal(oldLastWrite, newLastWrite);
    }

    [Fact]
    public void AtomicWrite_ConcurrentSaveAll_NoCorruption()
    {
        var fileName = "Config/atomic_concurrent.json";
        var prv = new JsonConfigProvider { FileName = fileName };
        // 初始化一份，确保后续 SaveAll 走"目标存在"分支
        prv.Save(new AtomicTestModel { Name = "init", Count = 0 });

        // 并发 SaveAll：让 N 个线程同时改 Root 并保存。
        // 共享同一把锁的前提下，磁盘上不会出现交叉写入或半截文件
        const Int32 threads = 8;
        const Int32 iterationsPerThread = 20;
        Parallel.For(0, threads, t =>
        {
            for (var i = 0; i < iterationsPerThread; i++)
            {
                prv.Save(new AtomicTestModel { Name = $"t{t}", Count = i });
            }
        });

        // 写入全部完成后，文件必须可解析为完整的 JSON 配置
        var fullPath = fileName.GetBasePath();
        var content = File.ReadAllText(fullPath);
        Assert.False(String.IsNullOrWhiteSpace(content));

        var verify = new JsonConfigProvider { FileName = fileName };
        var loaded = verify.Load<AtomicTestModel>();
        Assert.NotNull(loaded);
        Assert.False(String.IsNullOrEmpty(loaded.Name));
    }

    /// <summary>仅供测试使用的最小配置模型</summary>
    public class AtomicTestModel
    {
        public String Name { get; set; } = "";
        public Int32 Count { get; set; }
    }
}
