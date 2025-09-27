using NewLife;
using NewLife.Http;
using Xunit;

namespace XUnitTest.Http;

/// <summary>HttpBase 公共逻辑测试</summary>
public class HttpBaseTests
{
    [Theory]
    [InlineData("GET / ")]
    [InlineData("HTTP/1.1 200")]
    [InlineData("POST /api ")]
    public void FastValidHeader_Positive(String first)
    {
        var data = first.GetBytes();
        Assert.True(HttpBase.FastValidHeader(data));
    }
}
