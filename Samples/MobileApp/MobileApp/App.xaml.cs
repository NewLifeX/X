using System;
using System.Threading.Tasks;
using MobileApp.Services;
using MobileApp.Views;
using NewLife;
using NewLife.Log;
using NewLife.Serialization;
using NewLife.Threading;
using Stardust;
using Xamarin.Forms;

namespace MobileApp
{
    public partial class App : Application
    {

        public App()
        {
#if DEBUG
            XTrace.UseConsole();
#else
            XTrace.UseConsole(false, false);
#endif

            var log = new NetworkLog { Server = "udp://255.255.255.255:514" };
            XTrace.Log = new CompositeLog(XTrace.Log, log);

            var js = MachineInfo.GetCurrent().ToJson(true);
            XTrace.WriteLine(js);

            StartClient();

            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
            XTrace.WriteLine("OnStart");
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
            XTrace.WriteLine("OnSleep");
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
            XTrace.WriteLine("OnResume");
        }

        static TimerX _timer;
        static StarClient _Client;
        private static void StartClient()
        {
            var set = StarSetting.Current;
            var server = "http://star.newlifex.com:6600";

            XTrace.WriteLine("初始化服务端地址：{0}", server);

            var client = new StarClient(server)
            {
                Code = set.AppKey,
                Secret = set.Secret,
                ProductCode = "MobileApp",
                Log = XTrace.Log,
            };

            // 登录后保存证书
            client.OnLogined += (s, e) =>
            {
                var inf = client.Info;
                if (inf != null && !inf.Code.IsNullOrEmpty())
                {
                    set.AppKey = inf.Code;
                    set.Secret = inf.Secret;
                    set.Save();
                }
            };

            client.UseTrace();

            //Application.ApplicationExit += (s, e) => client.Logout("ApplicationExit");

            // 可能需要多次尝试
            _timer = new TimerX(TryConnectServer, client, 0, 5_000) { Async = true };

            _Client = client;
        }

        private static async Task TryConnectServer(Object state)
        {
            var client = state as StarClient;
            //var node = client.GetNodeInfo();
            await client.Login();

            // 登录成功，销毁定时器
            _timer.TryDispose();
            _timer = null;
        }
    }
}
