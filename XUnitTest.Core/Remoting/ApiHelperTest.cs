using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using NewLife;
using NewLife.Data;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Remoting
{
    /// <summary>ApiHttp助手类测试</summary>
    public class ApiHelperTest : DisposeBase
    {
        private readonly ApiServer _Server;
        private readonly HttpClient _Client;

        public ApiHelperTest()
        {
            //var port = Rand.Next(10000, 50000);
            var port = 28080;

            // 使用ApiServer作为测试服务端
            _Server = new ApiServer(port)
            {
                //Log = XTrace.Log,
            };
            _Server.Start();

            _Client = new HttpClient
            {
                BaseAddress = new Uri($"http://127.0.0.1:{port}")
            };
        }

        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            _Server.TryDispose();
        }

        [Theory(DisplayName = "建立请求")]
        [InlineData("Get", "api/info")]
        [InlineData("Post", "api/info")]
        [InlineData("Put", "api/info")]
        [InlineData("Get", null)]
        public void BuildRequestTest(String method, String action)
        {
            // 基础建立请求，无参数
            var md = new HttpMethod(method);
            var request = ApiHelper.BuildRequest(md, action, null);

            Assert.NotNull(request);
            Assert.Equal(md, request.Method);
            Assert.Equal(action, request.RequestUri?.ToString());
            Assert.Null(request.Content);
        }

        [Theory(DisplayName = "带参数建立请求")]
        [InlineData("Get", "api/info", "[buffer]")]
        [InlineData("Get", "api/info", "[packet]")]
        [InlineData("Get", "api/info", "[object]")]
        [InlineData("Get", "api/info", "[dictionary]")]
        [InlineData("Post", "api/info", "[buffer]")]
        [InlineData("Post", "api/info", "[packet]")]
        [InlineData("Post", "api/info", "[object]")]
        [InlineData("Post", "api/info", "[dictionary]")]
        [InlineData("Put", "api/info", "[buffer]")]
        [InlineData("Put", "api/info", "[packet]")]
        [InlineData("Put", "api/info", "[object]")]
        [InlineData("Put", "api/info", "[dictionary]")]
        public void BuildRequestTest2(String method, String action, String argKind)
        {
            // 几大类型参数
            Object args = null;
            switch (argKind)
            {
                case "[buffer]":
                    args = Rand.NextBytes(16);
                    break;
                case "[packet]":
                    args = new Packet(Rand.NextBytes(16));
                    break;
                case "[object]":
                    args = new { name = Rand.NextString(8), code = Rand.Next() };
                    break;
                case "[dictionary]":
                    var dic = new Dictionary<String, Object>
                    {
                        ["aaa"] = Rand.NextString(16),
                        ["bbb"] = Rand.Next(1000, 9999),
                        ["ccc"] = Rand.Next()
                    };
                    args = dic;
                    break;
            }

            // 建立请求
            var md = new HttpMethod(method);
            var request = ApiHelper.BuildRequest(md, action, args);

            // 无论如何，请求方法不会错
            Assert.NotNull(request);
            Assert.Equal(method, request.Method.Method);

            // Get有url参数，而Post没有
            var uri = request.RequestUri + "";
            var query = uri.Substring("?");
            switch (method)
            {
                case "Get":
                    Assert.NotEqual(action, request.RequestUri + "");
                    Assert.NotEmpty(query);
                    Assert.Null(request.Content);

                    // 对象和字典有特殊处理方式
                    if (argKind == "[object]")
                        Assert.Equal(args.ToDictionary().Join("&", k => $"{k.Key}={k.Value}"), query);
                    else if (argKind == "[dictionary]" && args is IDictionary<String, Object> dic)
                        Assert.Equal(dic.Join("&", k => $"{k.Key}={k.Value}"), query);
                    break;
                case "Post":
                case "Put":
                    Assert.Equal(action, request.RequestUri + "");
                    Assert.Null(query);
                    Assert.NotNull(request.Content);

                    // 不同参数类型，有不同的请求内容类型
                    var content = request.Content;
                    switch (argKind)
                    {
                        case "[buffer]":
                            Assert.Equal("application/octet-stream", content.Headers.ContentType + "");
                            Assert.Equal((args as Byte[]).ToHex(), content.ReadAsByteArrayAsync().Result.ToHex());
                            break;
                        case "[packet]":
                            Assert.Equal("application/octet-stream", request.Content.Headers.ContentType + "");
                            Assert.Equal((args as Packet).ToHex(), content.ReadAsByteArrayAsync().Result.ToHex());
                            break;
                        case "[object]":
                        case "[dictionary]":
                            Assert.Equal("application/json", request.Content.Headers.ContentType + "");
                            Assert.Equal(args.ToJson(), content.ReadAsStringAsync().Result);
                            break;
                    }
                    break;
                default:
                    Assert.Equal(action, request.RequestUri + "");
                    Assert.Null(query);
                    Assert.Null(request.Content);
                    break;
            }
        }

        [Theory(DisplayName = "处理Http错误响应")]
        [InlineData(null)]
        [InlineData("12345678")]
        public async void ProcessErrorResponseTest(String content)
        {
            var msg = new HttpResponseMessage(HttpStatusCode.BadRequest);
            if (!content.IsNullOrEmpty()) msg.Content = new StringContent(content);

            // 返回原型，不抛出异常
            var rs = await ApiHelper.ProcessResponse<HttpResponseMessage>(msg);
            Assert.Equal(msg, rs);

            // 捕获Api异常
            var ex = await Assert.ThrowsAsync<ApiException>(async () => await ApiHelper.ProcessResponse<String>(msg));

            Assert.Equal(HttpStatusCode.BadRequest, (HttpStatusCode)ex.Code);
            if (!content.IsNullOrEmpty())
                Assert.Equal(content, ex.Message);
            else
                Assert.Equal(msg.ReasonPhrase, ex.Message);
        }

        [Theory(DisplayName = "处理应用错误响应")]
        [InlineData("{code:500,data:\"Stone\"}")]
        [InlineData("{code:501,message:\"error\"}")]
        [InlineData("{code:502,data:\"Stone\",msg:\"error\"}")]
        public async void ProcessErrorResponseTest2(String content)
        {
            var msg = new HttpResponseMessage(HttpStatusCode.OK);
            if (!content.IsNullOrEmpty()) msg.Content = new StringContent(content);

            // 返回原型，不抛出异常
            var rs = await ApiHelper.ProcessResponse<HttpResponseMessage>(msg);
            Assert.Equal(msg, rs);

            // 捕获Api异常
            var ex = await Assert.ThrowsAsync<ApiException>(async () => await ApiHelper.ProcessResponse<String>(msg));

            Assert.Equal(content.Substring("code:", ",").ToInt(), ex.Code);

            var error = content.Substring("message:\"", "\"}");
            if (error.IsNullOrEmpty()) error = content.Substring("msg:\"", "\"}");
            if (error.IsNullOrEmpty()) error = content.Substring("data:\"", "\"}");
            Assert.Equal(error, ex.Message);
        }

        [Theory(DisplayName = "处理Byte响应")]
        [InlineData(null)]
        [InlineData("12345678")]
        public async void ProcessByteResponseTest(String content)
        {
            var msg = new HttpResponseMessage(HttpStatusCode.OK);
            if (!content.IsNullOrEmpty()) msg.Content = new ByteArrayContent(content.ToHex());

            // 处理
            var rs = await ApiHelper.ProcessResponse<Byte[]>(msg);
            if (content != null)
            {
                Assert.NotNull(rs);
                Assert.Equal(content, rs.ToHex());
            }
            else
            {
                Assert.Null(rs);
            }
        }

        [Theory(DisplayName = "处理Packet响应")]
        [InlineData(null)]
        [InlineData("12345678")]
        public async void ProcessPacketResponseTest(String content)
        {
            var msg = new HttpResponseMessage(HttpStatusCode.OK);
            if (!content.IsNullOrEmpty()) msg.Content = new ByteArrayContent(content.ToHex());

            // 处理
            var rs = await ApiHelper.ProcessResponse<Packet>(msg);
            if (content != null)
            {
                Assert.NotNull(rs);
                Assert.Equal(content, rs.ToHex());
            }
            else
            {
                Assert.Null(rs);
            }
        }

        [Theory(DisplayName = "处理响应")]
        [InlineData("{code:0,data:12345678}")]
        [InlineData("{code:0,data:\"Stone\"}")]
        [InlineData("{code:0,data:{aaa:\"bbb\",xxx:1234}}")]
        [InlineData("{code:0,data:{OSName:\"win10\",OSVersion:\"10.0\"}}")]
        public async void ProcessResponseTest(String content)
        {
            var msg = new HttpResponseMessage(HttpStatusCode.OK);
            if (!content.IsNullOrEmpty()) msg.Content = new StringContent(content);

            var data = content.Substring("data:", "}");
            Assert.NotEmpty(data);

            // 处理，基本类型直接返回
            if (data == "12345678")
            {
                var rs = await ApiHelper.ProcessResponse<Int32>(msg);
                Assert.Equal(data.ToInt(), rs);
            }
            else if (data[0] == '\"' && data[^1] == '\"')
            {
                var rs = await ApiHelper.ProcessResponse<String>(msg);
                Assert.Equal(data.Trim('\"'), rs);
            }
            else if (content != null)
            {
                // 复杂类型Json序列化，或者字典
                if (content.Contains("win10"))
                {
                    var mi = await ApiHelper.ProcessResponse<MachineInfo>(msg);
                    Assert.NotNull(mi);
                    Assert.Equal("win10", mi.OSName);
                    Assert.Equal("10.0", mi.OSVersion);
                }
                else
                {
                    var rs = await ApiHelper.ProcessResponse<Object>(msg);
                    var dic = rs as IDictionary<String, Object>;
                    Assert.NotNull(dic);
                    Assert.Equal("bbb", dic["aaa"]);
                    Assert.Equal(1234, dic["xxx"]);
                }
            }
        }

        [Theory(DisplayName = "处理复杂响应")]
        [InlineData("{errcode:0,errmsg:\"ok\",access_token:\"12345678\"}")]
        public async void ProcessResponse_OtherData(String content)
        {
            var msg = new HttpResponseMessage(HttpStatusCode.OK);
            if (!content.IsNullOrEmpty()) msg.Content = new StringContent(content);

            var token = await ApiHelper.ProcessResponse<String>(msg, "access_token");
            Assert.Equal("12345678", token);
        }

        [Theory(DisplayName = "处理复杂响应")]
        [InlineData("{errcode:0,errmsg:\"ok\",access_token:\"12345678\",\"expires_in\": 7200}")]
        public void ProcessResponse_Text(String content)
        {
            var token = ApiHelper.ProcessResponse<MyToken>(content);
            Assert.Equal("12345678", token.AccessToken);
            Assert.Equal(7200, token.ExpiresIn);
        }

        class MyToken
        {
            [DataMember(Name = "access_token")]
            public String AccessToken { get; set; }
            [DataMember(Name = "expires_in")]
            public Int32 ExpiresIn { get; set; }
        }

        [Theory(DisplayName = "处理异常响应")]
        [InlineData("{errcode:500,errmsg:\"valid data\"}")]
        public async void ProcessResponse_Error(String content)
        {
            var msg = new HttpResponseMessage(HttpStatusCode.OK);
            if (!content.IsNullOrEmpty()) msg.Content = new StringContent(content);

            var ex = await Assert.ThrowsAsync<ApiException>(async () => await ApiHelper.ProcessResponse<String>(msg, "access_token"));
            Assert.NotNull(ex);
            Assert.Equal(500, ex.Code);
            Assert.Equal("valid data", ex.Message);
        }

        //[Fact]
        public async void ProcessResponse_DingTalk()
        {
            var key = "dingbvcq0mz3pidpwtch";
            var secret = "7OTdnimQwf5LJnVp8e0udX1wPxKyCsspLqM2YcBDawvg3BlIkzxIsOs1YhDjiOxj";
            var url = "https://oapi.dingtalk.com/gettoken?appkey={key}&appsecret={secret}";
            url = url.Replace("{key}", key).Replace("{secret}", secret);

            var http = new HttpClient();
            var token = await http.InvokeAsync<String>(HttpMethod.Get, url, null, null, "access_token");
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            var url2 = "https://oapi.dingtalk.com/user/listbypage?access_token={token}&department_id=1&offset=0&size=100";
            url2 = url2.Replace("{token}", token);

            var users = await http.InvokeAsync<IList>(HttpMethod.Get, url2, null, null, "userlist");
            Assert.NotNull(users);
            Assert.True(users.Count > 0);
        }

        [Fact(DisplayName = "异步请求")]
        public async void SendAsyncTest()
        {
            var dic = await _Client.GetAsync<IDictionary<String, Object>>("api/info");
            Assert.NotNull(dic);
            Assert.True(dic.Count >= 10);
            Assert.StartsWith("testhost", (dic["Name"] + ""));

            var pk = await _Client.GetAsync<Packet>("api/info");
            Assert.NotNull(pk);
            Assert.True(pk.Total > 100);

            var ss = await _Client.PostAsync<String[]>("Api/All");
            Assert.NotNull(ss);
            Assert.True(ss.Length >= 2);
        }

        [Fact(DisplayName = "异常请求")]
        public async void ErrorTest()
        {
            var msg = await _Client.GetAsync<HttpResponseMessage>("api/info");
            Assert.NotNull(msg);
            Assert.Equal(HttpStatusCode.OK, msg.StatusCode);

            //msg = await _Client.InvokeAsync<HttpResponseMessage>("api/info3");
            //Assert.NotNull(msg);
            //Assert.Equal(HttpStatusCode.NotFound, msg.StatusCode);

            //var str = await msg.Content.ReadAsStringAsync();
            //Assert.Equal("\"无法找到名为[api/info3]的服务！\"", str);

            var ex = await Assert.ThrowsAsync<ApiException>(() => _Client.GetAsync<Object>("api/info3"));

            Assert.Equal(404, ex.Code);
            Assert.Equal("无法找到名为[api/info3]的服务！", ex.Message);
            //Assert.Equal(_Client.BaseAddress + "api/info3", ex.Source);
        }

        [Fact(DisplayName = "上传数据")]
        public async void PostAsyncTest()
        {
            var state = Rand.NextString(8);
            var state2 = Rand.NextString(8);
            var dic = await _Client.GetAsync<IDictionary<String, Object>>("api/info", new { state, state2 });
            Assert.NotNull(dic);
            Assert.Equal(state, dic[nameof(state)]);
            Assert.NotEqual(state2, dic[nameof(state2)]);

            var msg = await _Client.GetAsync<HttpResponseMessage>("api/info", new { state, state2 });
            Assert.NotNull(msg);
            Assert.Equal(HttpMethod.Get, msg.RequestMessage.Method);

            state = Rand.NextString(1000 + 8);
            msg = await _Client.PostAsync<HttpResponseMessage>("api/info", new { state, state2 });
            Assert.NotNull(msg);
            Assert.Equal(HttpMethod.Post, msg.RequestMessage.Method);
        }

        [Fact(DisplayName = "令牌请求")]
        public async void TokenTest()
        {
            var auth = new AuthenticationHeaderValue("Bearer", "12345678");
            //var headers = new Dictionary<String, String>();
            //headers["Authorization"] = auth + "";

            var dic = await _Client.InvokeAsync<IDictionary<String, Object>>(HttpMethod.Get, "api/info", null, r => r.Headers.Authorization = auth);
            Assert.NotNull(dic);
            Assert.True(dic.Count > 10);
            Assert.StartsWith("testhost", (dic["Name"] + ""));
            Assert.Equal("12345678", (dic["token"] + ""));

            var pk = await _Client.GetAsync<Packet>("api/info");
            Assert.NotNull(pk);
            Assert.True(pk.Total > 100);

            var ss = await _Client.PostAsync<String[]>("Api/All");
            Assert.NotNull(ss);
            Assert.True(ss.Length >= 2);
        }
    }
}