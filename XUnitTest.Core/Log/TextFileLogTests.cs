using NewLife.Log;
using Xunit;

namespace XUnitTest.Log;

/// <summary>文本文件日志测试</summary>
public class TextFileLogTests : IDisposable
{
    private readonly String _logDir;

    public TextFileLogTests()
    {
        _logDir = Path.Combine(Path.GetTempPath(), "NewLife_TextFileLogTests_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_logDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_logDir))
                Directory.Delete(_logDir, true);
        }
        catch { }
    }

    [Fact(DisplayName = "Create创建日志实例")]
    public void CreateReturnsInstance()
    {
        var log = TextFileLog.Create(_logDir);
        Assert.NotNull(log);
        Assert.Equal(_logDir, log.LogPath);
    }

    [Fact(DisplayName = "默认FileFormat不为空")]
    public void DefaultFileFormat()
    {
        var log = TextFileLog.Create(_logDir);
        Assert.False(String.IsNullOrEmpty(log.FileFormat));
    }

    [Fact(DisplayName = "MaxBytes默认值")]
    public void MaxBytesDefault()
    {
        var log = new TextFileLog();
        Assert.True(log.MaxBytes >= 0);
    }

    [Fact(DisplayName = "Backups默认值")]
    public void BackupsDefault()
    {
        var log = new TextFileLog();
        Assert.True(log.Backups >= 0);
    }

    [Fact(DisplayName = "写日志到文件")]
    public void WriteLogToFile()
    {
        var log = TextFileLog.Create(_logDir);
        log.Info("TextFileLogTests: 测试写入消息");

        // 日志通过线程池异步写入，通常毫秒级完成；轮询等待（兜底 5s 定时器刷盘路径）
        var content = String.Empty;
        var deadline = DateTime.Now.AddSeconds(6);
        while (DateTime.Now < deadline)
        {
            var files = Directory.GetFiles(_logDir, "*.log");
            if (files.Length > 0)
            {
                // 日志文件可能被TextFileLog持有锁，使用FileShare.ReadWrite读取
                try
                {
                    using var fs = new FileStream(files[0], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var sr = new StreamReader(fs);
                    content = sr.ReadToEnd();
                    if (content.Contains("测试写入消息")) break;
                }
                catch (IOException) { }
            }

            Thread.Sleep(50);
        }

        var files2 = Directory.GetFiles(_logDir, "*.log");
        Assert.True(files2.Length > 0, "应生成日志文件");

        // 日志文件可能被TextFileLog持有锁，使用FileShare.ReadWrite读取
        using var fs2 = new FileStream(files2[0], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr2 = new StreamReader(fs2);
        content = sr2.ReadToEnd();
        Assert.Contains("测试写入消息", content);
    }

    [Fact(DisplayName = "CreateFile创建文件日志")]
    public void CreateFileReturnsInstance()
    {
        var filePath = Path.Combine(_logDir, "test.log");
        var log = TextFileLog.CreateFile(filePath);
        Assert.NotNull(log);
    }
}
