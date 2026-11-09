using System.ComponentModel;
using System.Globalization;
using NewLife;
using NewLife.Http;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Http;

/// <summary>HTTP请求语言测试。与 LocalizationTests 同集合，避免全局语言状态互相干扰</summary>
[Collection("GlobalState")]
public class HttpRequestLanguageTests
{
    private static HttpServer CreateServer()
    {
        var server = new HttpServer
        {
            Port = 0,
            Log = XTrace.Log,
            SessionLog = XTrace.Log,
        };
        server.MapGet("/lang", ctx => ctx.Response.SetResult((Localization.Culture ?? CultureInfo.CurrentUICulture).Name));
        server.Start();

        return server;
    }

    [Fact]
    [DisplayName("HTTP请求_默认按Accept-Language的q权重设置当前UI文化")]
    public async Task RequestLanguage_DefaultsToAcceptLanguage()
    {
        var saved = Localization.Culture;
        try
        {
            Localization.Culture = null;

            using var server = CreateServer();
            using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{server.Port}") };

            var request = new HttpRequestMessage(HttpMethod.Get, "/lang");
            request.Headers.TryAddWithoutValidation("Accept-Language", "en-US;q=0.5, de-DE;q=0.9");

            var response = await client.SendAsync(request);

            Assert.Equal("de-DE", await response.Content.ReadAsStringAsync());
        }
        finally
        {
            Localization.Culture = saved;
        }
    }

    [Fact]
    [DisplayName("HTTP请求_固定全局语言时忽略Accept-Language")]
    public async Task RequestLanguage_IgnoresFixedCulture()
    {
        var saved = Localization.Culture;
        try
        {
            Localization.Culture = CultureInfo.GetCultureInfo("en-US");

            using var server = CreateServer();
            using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{server.Port}") };

            var request = new HttpRequestMessage(HttpMethod.Get, "/lang");
            request.Headers.TryAddWithoutValidation("Accept-Language", "de-DE");

            var response = await client.SendAsync(request);

            Assert.Equal("en-US", await response.Content.ReadAsStringAsync());
        }
        finally
        {
            Localization.Culture = saved;
        }
    }

    [Fact]
    [DisplayName("HTTP请求_无Accept-Language时回退默认语言")]
    public async Task RequestLanguage_FallsBackToDefaultCulture()
    {
        var saved = Localization.Culture;
        var savedDefault = Localization.DefaultCulture;
        var savedThread = CultureInfo.DefaultThreadCurrentUICulture;
        try
        {
            Localization.Culture = null;
            Localization.SetDefaultLanguage("zh-CN");

            using var server = CreateServer();
            using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{server.Port}") };

            var response = await client.GetAsync("/lang");

            Assert.Equal("zh-CN", await response.Content.ReadAsStringAsync());
        }
        finally
        {
            Localization.Culture = saved;
            Localization.SetDefaultLanguage(savedDefault?.Name);
            CultureInfo.DefaultThreadCurrentUICulture = savedThread;
        }
    }

    [Fact]
    [DisplayName("HTTP请求_不支持的语言回退默认语言")]
    public async Task RequestLanguage_FallsBackToDefaultCultureForUnsupportedLanguage()
    {
        var saved = Localization.Culture;
        var savedDefault = Localization.DefaultCulture;
        var savedThread = CultureInfo.DefaultThreadCurrentUICulture;
        try
        {
            Localization.Culture = null;
            Localization.SetDefaultLanguage("zh-CN");

            using var server = CreateServer();
            using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{server.Port}") };

            var request = new HttpRequestMessage(HttpMethod.Get, "/lang");
            request.Headers.TryAddWithoutValidation("Accept-Language", "sw-KE");

            var response = await client.SendAsync(request);

            Assert.Equal("zh-CN", await response.Content.ReadAsStringAsync());
        }
        finally
        {
            Localization.Culture = saved;
            Localization.SetDefaultLanguage(savedDefault?.Name);
            CultureInfo.DefaultThreadCurrentUICulture = savedThread;
        }
    }
}
