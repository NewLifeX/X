using System.Text;
using NewLife;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Security;

public class ProtectedKeyTests
{
    [Fact]
    public void ProtectedData()
    {
        var pd = new ProtectedKey { Secret = "NewLife".GetBytes() };

        var plain = "Hello Stone";
        var str = pd.Protect(plain);
        Assert.Equal("$AES$RmqCGPyEC6W98aGJwUStpQ", str);

        var rs = pd.Unprotect(str);
        Assert.Equal(plain, rs);
    }

    [Fact]
    public void ProtectedData2()
    {
        var pd = new ProtectedKey { Secret = "NewLife".GetBytes() };

        var plain = "server=.;uid=root;pwd=\"Hello Stone\";database=iot";
        var str = pd.Protect(plain);
        Assert.Equal("server=.;uid=root;pwd=$AES$RmqCGPyEC6W98aGJwUStpQ;database=iot", str);

        plain = "server=.;uid=root;pwd=Hello Stone;database=iot";
        str = pd.Protect(plain);
        Assert.Equal("server=.;uid=root;pwd=$AES$RmqCGPyEC6W98aGJwUStpQ;database=iot", str);

        var rs = pd.Unprotect(str);
        Assert.Equal(plain, rs);
    }

    [Fact]
    public void ProtectedData3()
    {
        var pd = new ProtectedKey { Secret = "NewLife".GetBytes() };

        var plain = "server=.;uid=root;pwd=\"Hello Stone\";database=iot";
        var str = pd.Unprotect(plain);
        Assert.Equal("server=.;uid=root;pwd=\"Hello Stone\";database=iot", str);
    }

    [Fact]
    public void Hide()
    {
        var pd = new ProtectedKey { Secret = "NewLife".GetBytes() };

        var plain = "server=.;uid=root;pwd=\"Hello Stone\";database=iot";
        var str = pd.Hide(plain);
        Assert.Equal("server=.;uid=root;pwd={***};database=iot", str);
    }
}
