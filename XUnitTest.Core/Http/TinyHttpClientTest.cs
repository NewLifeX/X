using System;
using NewLife;
using NewLife.Http;
using NewLife.Log;
using Xunit;
using NewLife.Data;

namespace XUnitTest.Http;

public class TinyHttpClientTest
{
    private readonly TinyHttpClient _Client;

    public TinyHttpClientTest()
    {
        _Client = new TinyHttpClient();
    }

    //[Fact(DisplayName = "同步请求")]
    //public void SendTest()
    //{
    //    var uri = new Uri("http://newlifex.com");
    //    var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(3), Log = XTrace.Log };
    //    var html = client.Send(uri, null)?.ToStr();

    //    Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
    //    Assert.Equal(uri, client.BaseAddress);
    //}

    [Fact(DisplayName = "异步请求")]
    public async void SendAsyncTest()
    {
        var uri = new Uri("http://newlifex.com");
        var req = new HttpRequest { RequestUri = uri };
        var client = new TinyHttpClient { Timeout = TimeSpan.FromSeconds(3), Log = XTrace.Log };
        var html = (await client.SendAsync(req))?.Body.ToStr();

        Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
        Assert.Equal(uri, client.BaseAddress);
    }

    //[Fact(DisplayName = "同步字符串")]
    //public void GetString()
    //{
    //    var url = "http://x.newlifex.com";
    //    var client = new TinyHttpClient();
    //    var html = client.GetString(url);

    //    Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
    //}

    [Fact(DisplayName = "异步字符串")]
    public async void GetStringAsync()
    {
        var url = "http://x.newlifex.com";
        var client = new TinyHttpClient();
        var html = await client.GetStringAsync(url);

        Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
    }

    [Fact(DisplayName = "https")]
    public async void GetStringHttps()
    {
        var url = "https://newlifex.com";
        var client = new TinyHttpClient();
        var html = await client.GetStringAsync(url);

        Assert.True(!html.IsNullOrEmpty() && html.Length > 500);
    }
}
