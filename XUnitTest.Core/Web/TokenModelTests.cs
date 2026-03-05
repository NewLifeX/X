using NewLife.Web;
using Xunit;

namespace XUnitTest.Web;

/// <summary>访问令牌模型测试</summary>
public class TokenModelTests
{
    [Fact(DisplayName = "默认构造函数")]
    public void DefaultConstructor()
    {
        var model = new TokenModel();
        Assert.Null(model.AccessToken);
        Assert.Null(model.TokenType);
        Assert.Null(model.RefreshToken);
        Assert.Null(model.Scope);
        Assert.Equal(0, model.ExpireIn);
    }

    [Fact(DisplayName = "属性赋值")]
    public void PropertyAssignment()
    {
        var model = new TokenModel
        {
            AccessToken = "abc123",
            TokenType = "Bearer",
            ExpireIn = 7200,
            RefreshToken = "refresh456",
            Scope = "read write"
        };

        Assert.Equal("abc123", model.AccessToken);
        Assert.Equal("Bearer", model.TokenType);
        Assert.Equal(7200, model.ExpireIn);
        Assert.Equal("refresh456", model.RefreshToken);
        Assert.Equal("read write", model.Scope);
    }

    [Fact(DisplayName = "实现IToken接口")]
    public void ImplementsIToken()
    {
        var model = new TokenModel { AccessToken = "token" };
        Assert.IsAssignableFrom<IToken>(model);
    }
}
