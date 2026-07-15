using NewLife.Configuration;
using Xunit;

namespace XUnitTest.Configuration;

/// <summary>Apollo配置提供者测试</summary>
public class ApolloConfigProviderTests
{
    [Fact(DisplayName = "构造和基本属性")]
    public void ConstructorAndBasicProperties()
    {
        var provider = new ApolloConfigProvider
        {
            Server = "http://127.0.0.1:8080",
            AppId = "test-app",
        };

        Assert.NotNull(provider);
        Assert.Equal("http://127.0.0.1:8080", provider.Server);
        Assert.Equal("test-app", provider.AppId);
    }

    [Fact(DisplayName = "SetApollo设置命名空间")]
    public void SetApolloSetsNamespace()
    {
        var provider = new ApolloConfigProvider();
        Assert.Null(provider.NameSpace);

        provider.SetApollo("application");
        Assert.Equal("application", provider.NameSpace);

        provider.SetApollo("application,myapp");
        Assert.Equal("application,myapp", provider.NameSpace);
    }

    [Fact(DisplayName = "ToString包含关键信息")]
    public void ToStringContainsInfo()
    {
        var provider = new ApolloConfigProvider
        {
            Server = "http://127.0.0.1:8080",
            AppId = "test-app",
        };

        var str = provider.ToString();
        Assert.Contains("ApolloConfigProvider", str);
        Assert.Contains("test-app", str);
        Assert.Contains("127.0.0.1", str);
    }
}
