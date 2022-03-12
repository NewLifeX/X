using System;
using App1.Services;
using App1.Views;
using NewLife.Log;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace App1
{
    public partial class App : Application
    {

        public App()
        {
            // 可以使用雷电模拟器测试，启动进入桌面时，PC上迅速执行 adb tcpip 5555

            var log = new NetworkLog { Server = "udp://255.255.255.255:514" };
            XTrace.Log = new CompositeLog(XTrace.Log, log);
            XTrace.WriteLine("App");

            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            MainPage = new AppShell();
        }

        protected override void OnStart()
        {
            XTrace.WriteLine("OnStart");
        }

        protected override void OnSleep()
        {
            XTrace.WriteLine("OnSleep");
        }

        protected override void OnResume()
        {
            XTrace.WriteLine("OnResume");
        }
    }
}
