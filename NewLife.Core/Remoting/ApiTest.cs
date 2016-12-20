using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;

namespace NewLife.Remoting
{
#if DEBUG
    /// <summary>Rpc测试</summary>
    public class ApiTest
    {
        /// <summary>测试主函数</summary>
        public static async void Main()
        {
            var svr = new ApiServer(3344);
            svr.Add("http://*:888/");
            //svr.Log = XTrace.Log;
            svr.Encoder = new JsonEncoder();
            //svr.Encoder = new ProtocolBuffer();
            GlobalFilters.Add(new FFAttribute { Name = "全局" });
            GlobalFilters.Add(new FEAttribute { Name = "全局" });
            svr.Register<HelloController>();
            svr.Start();


            var client = new ApiClient("udp://127.0.0.1:3344") { Encoder = new JsonEncoder() };
            //client.Log = XTrace.Log;
            //client.Encoder = new ProtocolBuffer();
            //client.Compress = new SevenZip();
            client.Open();
            client.Login("admin", "password");

            var msg = "NewLifeX";
            var rs = await client.Invoke<string>("Hello/Say", new { msg });
            Console.WriteLine(rs);

            try
            {
                msg = "报错";
                rs = await client.Invoke<string>("Hello/Say", new { msg });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadKey();

            client.Dispose();
            svr.Dispose();
        }

        [FF(Name = "类")]
        [FE(Name = "类")]
        private class HelloController : IApi
        {
            public IApiSession Session { get; set; }

            [FF(Name = "方法")]
            [FE(Name = "方法")]
            public string Say(string msg)
            {
                if (msg == "报错") throw new Exception("出错");

                return "收到：" + msg;
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
