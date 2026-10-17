using System.Globalization;
using System.Text;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Util;
using Android.Views;
using Android.Widget;
using MobileConsole;
using NewLife;

namespace MobileConsole.AndroidHost;

/// <summary>把整个屏幕当作控制台：等宽字体、可滚动、可复制，同时把内容写入 logcat。</summary>
[Activity(Label = "MobileConsole", MainLauncher = true, Theme = "@android:style/Theme.Material.NoActionBar",
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
public class MainActivity : Activity
{
    private const String Tag = "MobileConsole";
    private const String Background = "#0B0F14";
    private const String Foreground = "#D7E2EA";

    private TextView? _console;
    private Button? _refresh;
    private Boolean _running;
    private String _report = "";

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var root = new LinearLayout(this) { Orientation = Orientation.Vertical };
        root.SetBackgroundColor(Color.ParseColor(Background));

        var bar = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        bar.SetGravity(GravityFlags.CenterVertical);
        bar.SetPadding(Dp(12), Dp(4), Dp(12), Dp(4));

        var title = new TextView(this) { Text = "MobileConsole", TextSize = 15 };
        title.SetTextColor(Color.ParseColor("#7FD1AE"));
        title.SetTypeface(Typeface.Monospace, TypefaceStyle.Bold);
        bar.AddView(title, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1));

        _refresh = new Button(this) { Text = "刷新" };
        _refresh.Click += (_, _) => RunDump();
        bar.AddView(_refresh);

        var copy = new Button(this) { Text = "复制" };
        copy.Click += (_, _) => CopyReport();
        bar.AddView(copy);

        _console = new TextView(this)
        {
            TextSize = 11,
            Typeface = Typeface.Monospace,
        };
        _console.SetTextColor(Color.ParseColor(Foreground));
        _console.SetBackgroundColor(Color.ParseColor(Background));
        _console.SetPadding(Dp(12), Dp(8), Dp(12), Dp(24));
        _console.SetTextIsSelectable(true);

        var scroll = new ScrollView(this);
        scroll.AddView(_console, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

        root.AddView(bar, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
        root.AddView(scroll, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1));
        SetContentView(root);

        RunDump();
    }

    private void RunDump()
    {
        if (_running) return;
        _running = true;
        if (_refresh != null) _refresh.Enabled = false;
        if (_console != null) _console.Text = "采集中...";

        Task.Run(() =>
        {
            try
            {
                var text = ConsoleDump.Build() + BuildAndroidSection();
                _report = text;

                foreach (var line in text.Split('\n'))
                {
                    Log.Info(Tag, line.TrimEnd('\r'));
                }
                Log.Info(Tag, "=== MobileConsole end ===");

                RunOnUiThread(() => { if (_console != null) _console.Text = text; });
            }
            catch (Exception ex)
            {
                Log.Error(Tag, ex.ToString());
                RunOnUiThread(() => { if (_console != null) _console.Text = "采集失败:\n" + ex; });
            }
            finally
            {
                _running = false;
                RunOnUiThread(() => { if (_refresh != null) _refresh.Enabled = true; });
            }
        });
    }

    private String BuildAndroidSection()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Android 原生信息]");
        sb.AppendLine("制造商: " + Build.Manufacturer);
        sb.AppendLine("品牌: " + Build.Brand);
        sb.AppendLine("型号: " + Build.Model);
        sb.AppendLine("设备: " + Build.Device);
        sb.AppendLine("产品: " + Build.Product);
        sb.AppendLine("硬件: " + Build.Hardware);
        sb.AppendLine("系统: Android " + Build.VERSION.Release + " (API " + (Int32)Build.VERSION.SdkInt + ")");
        sb.AppendLine("ABI: " + (Build.SupportedAbis is { Count: > 0 } abis ? String.Join(", ", abis) : ""));
        sb.AppendLine("指纹: " + Build.Fingerprint);

        var dm = Resources?.DisplayMetrics;
        if (dm != null) sb.AppendLine($"屏幕: {dm.WidthPixels}x{dm.HeightPixels} @ {(Int32)dm.DensityDpi}dpi");

        try
        {
            if (GetSystemService(ActivityService) is ActivityManager am)
            {
                var memory = new ActivityManager.MemoryInfo();
                am.GetMemoryInfo(memory);
                sb.AppendLine("总内存: " + ((UInt64)memory.TotalMem).ToGMK());
                sb.AppendLine("可用内存: " + ((UInt64)memory.AvailMem).ToGMK());
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine("内存读取失败: " + ex.Message);
        }

        try
        {
            using var filter = new IntentFilter(Intent.ActionBatteryChanged);
            using var battery = RegisterReceiver(null, filter);
            if (battery != null)
            {
                var level = battery.GetIntExtra(BatteryManager.ExtraLevel, -1);
                var scale = battery.GetIntExtra(BatteryManager.ExtraScale, -1);
                if (level >= 0 && scale > 0) sb.AppendLine("电池: " + (level * 100 / scale) + "%");

                var temperature = battery.GetIntExtra(BatteryManager.ExtraTemperature, Int32.MinValue);
                if (temperature != Int32.MinValue) sb.AppendLine("电池温度: " + (temperature / 10.0).ToString("n1", CultureInfo.InvariantCulture) + " °C");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine("电池读取失败: " + ex.Message);
        }

        try
        {
            var androidId = Settings.Secure.GetString(ContentResolver, Settings.Secure.AndroidId);
            sb.AppendLine("android_id: " + androidId);
        }
        catch (Exception ex)
        {
            sb.AppendLine("android_id 读取失败: " + ex.Message);
        }

        sb.AppendLine("包名: " + PackageName);
        sb.AppendLine("应用数据: " + FilesDir?.AbsolutePath);
        sb.AppendLine();

        return sb.ToString();
    }

    private void CopyReport()
    {
        if (_report.IsNullOrEmpty()) return;

        try
        {
            if (GetSystemService(ClipboardService) is ClipboardManager clipboard)
            {
                clipboard.PrimaryClip = ClipData.NewPlainText("MobileConsole", _report);
                Toast.MakeText(this, "已复制", ToastLength.Short)?.Show();
            }
        }
        catch (Exception ex)
        {
            Log.Error(Tag, "copy failed: " + ex.Message);
        }
    }

    private Int32 Dp(Int32 value) => (Int32)Math.Round(value * (Resources?.DisplayMetrics?.Density ?? 1));
}
