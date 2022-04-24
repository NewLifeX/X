using MobileApp.Services;
using MobileApp.Views;
using NewLife;
using NewLife.Log;
using NewLife.Serialization;
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

            var js = MachineInfo.GetCurrent().ToJson(true);
            XTrace.WriteLine(js);

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
    }
}
