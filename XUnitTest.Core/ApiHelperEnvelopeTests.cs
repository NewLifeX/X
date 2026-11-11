using System;
using System.Collections.Generic;
using NewLife;
using NewLife.Remoting;
using Xunit;

namespace XUnitTest;

/// <summary>业务错误信封测试。错误响应中的附加字段（如服务端时间）需要保留给调用方</summary>
public class ApiHelperEnvelopeTests
{
    [Fact(DisplayName = "错误信封保留原始响应字典")]
    public void ProcessResponse_ErrorKeepsEnvelope()
    {
        // 服务端在错误响应里附带 serverTime，客户端登录失败时需要读出来校准时间差
        var json = "{\"action\":\"Login\",\"code\":500,\"data\":\"boom\",\"serverTime\":1762790400000}";

        var ex = Assert.Throws<ApiException>(() => ApiHelper.ProcessResponse<Object>(json, null, "data"));

        Assert.Equal(500, ex.Code);
        Assert.Equal("boom", ex.Message);

        var dic = ex.Data["response"] as IDictionary<String, Object?>;
        Assert.NotNull(dic);
        Assert.Equal(1762790400000L, dic!["serverTime"].ToLong());
    }

    [Fact(DisplayName = "成功响应不抛异常也不带信封")]
    public void ProcessResponse_Success_NoException()
    {
        var rs = ApiHelper.ProcessResponse<String>("{\"action\":\"Info\",\"code\":0,\"data\":\"ok\"}", null, "data");

        Assert.Equal("ok", rs);
    }
}