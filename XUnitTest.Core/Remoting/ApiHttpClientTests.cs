using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using NewLife;
using NewLife.Data;
using NewLife.Http;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Remoting
{
    public class ApiHttpClientTests : DisposeBase
    {
        private readonly ApiServer _Server;
        private readonly String _Address;
        private readonly IApiClient _Client;

        public ApiHttpClientTests()
        {
            _Server = new ApiServer(12347)
            {
                //Log = XTrace.Log,
                //EncoderLog = XTrace.Log,
            };
            _Server.Handler = new TokenApiHandler { Host = _Server };
            _Server.Start();

            _Address = "http://127.0.0.1:12347";

            //_Client = new ApiHttpClient();
            //_Client.Add("addr1", new Uri("http://127.0.0.1:12347"));
            _Client = new ApiHttpClient(_Address);
        }

        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            _Server.TryDispose();
        }

        [Fact(DisplayName = "基础Api测试")]
        public async void BasicTest()
        {
            var apis = await _Client.InvokeAsync<String[]>("api/all");
            Assert.NotNull(apis);
            Assert.Equal(2, apis.Length);
            Assert.Equal("String[] Api/All()", apis[0]);
            Assert.Equal("Object Api/Info(String state)", apis[1]);
            //Assert.Equal("Packet Api/Info2(Packet state)", apis[2]);
        }

        [Fact(DisplayName = "参数测试")]
        public async void InfoTest()
        {
            var state = Rand.NextString(8);
            var state2 = Rand.NextString(8);

            var infs = await _Client.InvokeAsync<IDictionary<String, Object>>("api/info", new { state, state2 });
            Assert.NotNull(infs);
            Assert.Equal(Environment.MachineName, infs["MachineName"]);
            //Assert.Equal(Environment.UserName, infs["UserName"]);

            Assert.Equal(state, infs["state"]);
            Assert.Null(infs["state2"]);
        }

        //[Fact(DisplayName = "二进制测试")]
        //public async void Info2Test()
        //{
        //    var buf = Rand.NextBytes(32);

        //    var pk = await _Client.InvokeAsync<Packet>("api/info2", buf);
        //    Assert.NotNull(pk);
        //    Assert.True(pk.Total > buf.Length);
        //    Assert.Equal(buf, pk.Slice(pk.Total - buf.Length, -1).ToArray());
        //}

        [Fact(DisplayName = "异常请求")]
        public async void ErrorTest()
        {
            var ex = await Assert.ThrowsAsync<ApiException>(() => _Client.InvokeAsync<Object>("api/info3"));

            Assert.NotNull(ex);
            Assert.Equal(404, ex.Code);
            //Assert.True(ex.Message.EndsWith("无法找到名为[api/info3]的服务！"));
            Assert.EndsWith("无法找到名为[api/info3]的服务！", ex.Message);
        }

        [Theory(DisplayName = "令牌测试")]
        [InlineData("12345678", "ABCDEFG")]
        [InlineData("ABCDEFG", "12345678")]
        public async void TokenTest(String token, String state)
        {
            var client = new ApiHttpClient(_Address) { Token = token };
            var ac = client as IApiClient;

            var infs = await ac.InvokeAsync<IDictionary<String, Object>>("api/info", new { state });
            Assert.NotNull(infs);
            Assert.Equal(token, infs["token"]);

            // 另一个客户端，共用令牌，应该可以拿到上一次状态数据
            var client2 = new ApiHttpClient(_Address) { Token = token };

            infs = await client2.GetAsync<IDictionary<String, Object>>("api/info");
            Assert.NotNull(infs);
            //Assert.Equal(state, infs["LastState"]);
        }

        [Fact]
        public void SlaveTest()
        {
            var client = new ApiHttpClient("http://127.0.0.1:10000,http://127.0.0.1:20000," + _Address)
            {
                Timeout = 3_000
            };
            var ac = client as IApiClient;

            var infs = ac.Invoke<IDictionary<String, Object>>("api/info");
            Assert.NotNull(infs);
        }

        [Fact]
        public async void SlaveAsyncTest()
        {
            var filter = new TokenHttpFilter
            {
                UserName = "test",
                Password = "",
            };
            var client = new ApiHttpClient("http://127.0.0.1:10001,http://127.0.0.1:20001,http://star.newlifex.com:6600")
            {
                Filter = filter,
                Timeout = 3_000
            };

            var rs = await client.PostAsync<Object>("config/getall", new { appid = "starweb" });
            Assert.NotNull(rs);

            var ss = client.Services;
            Assert.Equal(3, ss.Count);
            Assert.Equal(1, ss[0].Times);
            Assert.Equal(1, ss[0].Errors);
            Assert.Equal(1, ss[1].Times);
            Assert.Equal(1, ss[1].Errors);
            Assert.Equal(1, ss[2].Times);
            Assert.Equal(0, ss[2].Errors);
        }

        [Fact]
        public async void RoundRobinTest()
        {
            var client = new ApiHttpClient("test1=3*http://127.0.0.1:10000,test2=7*http://127.0.0.1:20000,")
            {
                RoundRobin = true,
                Timeout = 3_000,
                Log = XTrace.Log,
            };

            Assert.Equal(2, client.Services.Count);

            // 再加两个
            client.Add("test3", "2*" + _Address);
            client.Add("test4", "1*" + _Address);

            Assert.Equal(4, client.Services.Count);

            {
                var svc = client.Services[0];
                Assert.Equal("test1", svc.Name);
                Assert.Equal(3, svc.Weight);
                Assert.Equal("http://127.0.0.1:10000/", svc.Address + "");

                svc = client.Services[1];
                Assert.Equal("test2", svc.Name);
                Assert.Equal(7, svc.Weight);
                Assert.Equal("http://127.0.0.1:20000/", svc.Address + "");

                svc = client.Services[2];
                Assert.Equal("test3", svc.Name);
                Assert.Equal(2, svc.Weight);
                Assert.Equal(_Address + "/", svc.Address + "");
            }

            var ac = client as IApiClient;

            {
                var infs = await ac.InvokeAsync<IDictionary<String, Object>>("api/info");
                Assert.NotNull(infs);
            }
            {
                var infs = await ac.InvokeAsync<IDictionary<String, Object>>("api/info");
                Assert.NotNull(infs);
            }
            {
                var infs = await ac.InvokeAsync<IDictionary<String, Object>>("api/info");
                Assert.NotNull(infs);
            }
            {
                var infs = await ac.InvokeAsync<IDictionary<String, Object>>("api/info");
                Assert.NotNull(infs);
            }

            // 判断结果
            {
                var svc = client.Services[0];
                Assert.Null(svc.Client);
                Assert.True(svc.NextTime > DateTime.Now.AddSeconds(55));
                Assert.Equal(1, svc.Times);
            }
            {
                var svc = client.Services[1];
                Assert.Null(svc.Client);
                Assert.True(svc.NextTime > DateTime.Now.AddSeconds(55));
                Assert.Equal(1, svc.Times);
            }
            {
                var svc = client.Services[2];
                Assert.NotNull(svc.Client);
                Assert.True(svc.NextTime.Year < 2000);
                Assert.Equal(3, svc.Times);
            }
            {
                var svc = client.Services[3];
                Assert.NotNull(svc.Client);
                Assert.True(svc.NextTime.Year < 2000);
                Assert.Equal(1, svc.Times);
            }
        }

        [Fact]
        public async void FilterTest()
        {
            var filter = new TokenHttpFilter
            {
                UserName = "test",
                Password = "",
            };

            var client = new ApiHttpClient("http://star.newlifex.com:6600")
            {
                Filter = filter,

                Log = XTrace.Log,
            };

            var rs = await client.PostAsync<Object>("config/getall", new { appid = "starweb" });

            Assert.NotNull(rs);
            Assert.NotNull(filter.Token);
        }
    }
}