using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MobileApp.Services;
using MobileApp.Views;
using NewLife.Log;

namespace MobileApp
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            XTrace.UseConsole(false, false);
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
    }
}
