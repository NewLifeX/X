using NewLife.Caching;
using NewLife.Data;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Caching;

/// <summary>MemoryCache 大容量保存测试</summary>
/// <remarks>
/// 与 <see cref="MemoryCacheTests"/> 拆成两个类是为了并行：两个用例各写入 50 万项并落盘，
/// 同在一个类内串行执行需要十余秒，独立成集合后可与其它的缓存用例并行。
/// </remarks>
public class MemoryCacheBigSaveTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BigSave(Boolean compressed)
    {
        var mc = new MemoryCache();

        for (var i = 0; i < 500_000; i++)
        {
            var ga = new GeoArea { Code = Rand.Next(100000, 999999), Name = Rand.NextString(8) };
            mc.Set(ga.Name, ga);
        }

        if (compressed)
            mc.Save("data/bigsave.gz", true);
        else
            mc.Save("data/bigsave.cache", false);
    }
}
