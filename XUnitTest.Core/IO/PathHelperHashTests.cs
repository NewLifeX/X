using System;
using System.IO;
using NewLife;
using Xunit;

namespace XUnitTest.IO;

public class PathHelperHashTests
{
    [Fact]
    public void VerifyHash_AutoDetectAndPrefix_BothPass()
    {
        var file = "hash-test.txt".AsFile();
        if (file.Exists) file.Delete();
        File.WriteAllText(file.FullName, "Hello Hash");

        // MD5 32 位
        var md5 = SecurityHelper.MD5(file).ToHex();
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
}
