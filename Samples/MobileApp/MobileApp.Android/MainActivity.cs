using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using NewLife;
using NewLife.Reflection;
using NewLife.Serialization;
using Xamarin.Essentials;

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

            var path = ".".GetFullPath();
            var driveInfo = DriveInfo.GetDrives().FirstOrDefault(e => path.StartsWithIgnoreCase(e.Name));

            var deviceId = Android.Provider.Settings.Secure.GetString(Application.Context.ContentResolver, Android.Provider.Settings.Secure.AndroidId);

            var device = DeviceInfo.Model;
            var dic = new Dictionary<String, Object>();
            foreach (var item in typeof(DeviceInfo).GetProperties())
            {
                dic[item.Name] = item.GetValue(device);
            }
            var js = dic.ToJson(true);

            // 设置入口程序集
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            if (asm != null) AssemblyX.Entry = AssemblyX.Create(asm);

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