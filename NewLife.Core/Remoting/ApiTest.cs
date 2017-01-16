using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Remoting
{
#if DEBUG
    /// <summary>Rpc测试</summary>
    [Api(null)]
    public class ApiTest
    {
        /// <summary>测试主函数</summary>
        public static async void Main()
        {
            var svr = new ApiServer(3344);
            //svr.Add("http://*:888/");
            svr.Log = XTrace.Log;
            svr.Encoder = new JsonEncoder();
            svr.Filters.Add(new DeflateFilter());
            svr.Filters.Add(new RC4Filter { Key = "Pass$word".GetBytes() });
            //GlobalFilters.Add(new FFAttribute { Name = "全局" });
            //GlobalFilters.Add(new FEAttribute { Name = "全局" });
            svr.Register<HelloController>();
            svr.Register(typeof(ApiTest), nameof(Login));
            svr.Start();


            var client = new ApiClient("tcp://127.0.0.1:3344");
            //var client = new ApiClient("udp://127.0.0.1:3344");
            //var client = new ApiClient("http://127.0.0.1:888");
            client.Log = XTrace.Log;
            client.Encoder = new JsonEncoder();
            client.Filters.Add(new DeflateFilter());
            client.Filters.Add(new RC4Filter { Key = "Pass$word".GetBytes() });
            client.Open();

            var logined = await client.InvokeAsync<Object>("Login", new { user = "Stone", pass = "密码" });
            XTrace.WriteLine(logined + "");

            var msg = "NewLifeX";
            var rs = await client.InvokeAsync<string>("Hello/Say", new { msg });
            XTrace.WriteLine(rs);

            try
            {
                msg = "报错";
                rs = await client.InvokeAsync<string>("Hello/Say", new { msg });
            }
            catch (ApiException ex)
            {
                XTrace.WriteLine("服务端发生 {0} 错误：{1}", ex.Code, ex.Message);
            }

            var apis = await client.InvokeAsync<String[]>("Api/All");
            Console.WriteLine(apis.Join(","));

            var ps = await client.InvokeAsync<String>("Api/Detail", new { api = apis.Join(",") });
            Console.WriteLine(ps);

            Console.WriteLine("完成");
            Console.ReadKey();

            client.Dispose();
            svr.Dispose();
        }

        private static void Login(String user, String pass)
        {
            XTrace.WriteLine("user={0} pass={1}", user, pass);

            //return true;
        }

        //[FF(Name = "类")]
        //[FE(Name = "类")]
        private class HelloController : IApi
        {
            public IApiSession Session { get; set; }

            //[FF(Name = "方法")]
            //[FE(Name = "方法")]
            public string Say(string msg)
            {
                if (msg == "报错") throw new Exception("出错，上一次 " + Session["Last"]);

                Session["Last"] = msg;

                var ss = Session.AllSessions;

                return "收到：{0} 在线：{1}".F(msg, ss.Length);
            }
        }

        class FFAttribute : ActionFilterAttribute
        {
            public String Name { get; set; }

            public override void OnActionExecuting(ActionExecutingContext filterContext)
            {
                XTrace.WriteLine("{0} Executing", Name);

                base.OnActionExecuting(filterContext);
            }

            public override void OnActionExecuted(ActionExecutedContext filterContext)
            {
                XTrace.WriteLine("{0} Executed", Name);

                base.OnActionExecuted(filterContext);
            }
        }

        class FEAttribute : HandleErrorAttribute
        {
            public String Name { get; set; }

            public override void OnException(ExceptionContext filterContext)
            {
                XTrace.WriteLine("{0} Exception", Name);

                base.OnException(filterContext);

                if (Name == "方法")
                {
                    filterContext.Result = filterContext.Exception?.GetTrue()?.Message + " 异常已处理";
                    //filterContext.ExceptionHandled = true;
                }
            }
        }
    }
#endif
}
