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
