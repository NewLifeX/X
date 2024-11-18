using NewLife.Common;
using Xunit;

namespace XUnitTest.Common;

public class SysConfigTest
{
    [Fact(DisplayName = "系统名称")]
    public void DisplayNameTest()
    {
        var asm = SysConfig.SysAssembly;
        Assert.NotNull(asm);
    }
}
