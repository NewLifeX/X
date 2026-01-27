using System;
using System.IO;
using NewLife;
using Xunit;

namespace XUnitTest.IO;

/// <summary>PathHelper Hash验证测试</summary>
[Collection("IO")]
public class PathHelperHashTests
{
    [Fact]
    public void VerifyHash_AutoDetectAndPrefix_BothPass()
    {
        // 使用临时目录和唯一文件名，避免并发测试冲突
        var tempPath = Path.Combine(Path.GetTempPath(), $"hash-test-{Guid.NewGuid():N}.txt");
        var file = tempPath.AsFile();
        try
        {
            File.WriteAllText(file.FullName, "Hello Hash");
            file.Refresh(); // 刷新文件信息

            // MD5 32 位
            var md5 = file.MD5().ToHex();
            Assert.True(file.VerifyHash(md5));
            Assert.True(file.VerifyHash("md5$" + md5));

            // MD5 16 位（前 16 个字符）
            var md516 = md5.Substring(0, 16);
            Assert.True(file.VerifyHash(md516));
            Assert.True(file.VerifyHash("md5$" + md516));

            // CRC32 自动识别（8 位）与前缀
            var data = File.ReadAllBytes(file.FullName);
            var crc = SecurityHelper.Crc(data).ToString("X8");
            Assert.True(file.VerifyHash(crc));
            Assert.True(file.VerifyHash("crc32$" + crc));
        }
        finally
        {
            // 清理临时文件
            if (file.Exists) file.Delete();
        }
    }
}
