using NewLife;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net;

/// <summary>升级包缓存与哈希校验</summary>
public class UpgradeHashTests
{
    [Fact(DisplayName = "升级包缓存哈希不符时必须重新下载")]
    public void NeedDownload_BadCacheDownloadsAgain()
    {
        var file = Path.Combine(Path.GetTempPath(), "upg-" + Guid.NewGuid().ToString("N") + ".zip");
        File.WriteAllText(file, "broken package");
        try
        {
            // 哈希对不上，必须重下
            Assert.True(Upgrade.NeedDownload(file, "md5$00000000000000000000000000000000"));

            // 哈希对得上，直接用缓存
            var md5 = file.AsFile().MD5().ToHex();
            Assert.False(Upgrade.NeedDownload(file, "md5$" + md5));

            // 没有哈希要求，直接用缓存
            Assert.False(Upgrade.NeedDownload(file, null));

            // 文件不存在，必须下载
            Assert.True(Upgrade.NeedDownload(file + ".missing", null));
        }
        finally
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }
}
