using System;
using NewLife.Reflection;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using System.Runtime.InteropServices;

namespace MobileApp.Droid
{
    [Activity(Label = "MobileApp", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            var id = Build.Id;
            var serial = Build.Serial;
            var model = Build.Model;
            var prd = Build.Product;

            var deviceId = Android.Provider.Settings.Secure.GetString(Application.Context.ContentResolver, Android.Provider.Settings.Secure.AndroidId);

            var type = "Android.Provider.Settings".GetTypeEx();
            type = type.GetNestedType("Secure");
            var aid = type.GetValue("AndroidId");
            var resolver = "Android.App.Application".GetTypeEx().GetValue("Context").GetValue("ContentResolver");
            var did = type.Invoke("GetString", resolver, aid);

            var str1 = RuntimeInformation.FrameworkDescription;
            var str2 = RuntimeInformation.ProcessArchitecture;
            var str3 = RuntimeInformation.OSArchitecture;
            var str4 = RuntimeInformation.OSDescription;

            //var osName = typeof(RuntimeInformation).Invoke("GetOSName") as String;

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}