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
using NewLife.Log;

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
    private String _logDir = "";

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

        MachineInfo.Provider = new AndroidMachineInfoProvider(this);
        SetupLogging();
        RunDump();
    }

    private void SetupLogging()
    {
        var logs = new CompositeLog(new TextViewLog(AppendLine));

        try
        {
            var root = GetExternalFilesDir(null)?.AbsolutePath;
            if (root.IsNullOrEmpty()) root = FilesDir?.AbsolutePath;
            if (root.IsNullOrEmpty()) root = CacheDir?.AbsolutePath;

            if (!root.IsNullOrEmpty())
            {
                _logDir = System.IO.Path.Combine(root, "Log");
                Directory.CreateDirectory(_logDir);
                logs.Add(TextFileLog.Create(_logDir));
            }
        }
        catch (Exception ex)
        {
            Log.Error(Tag, "日志初始化失败: " + ex.Message);
        }

        XTrace.Log = logs;
    }

    private void AppendLine(String line) => RunOnUiThread(() =>
    {
        if (_console == null) return;
        _console.Append(line + "\n");
    });

    private void RunDump()
    {
        if (_running) return;
        _running = true;
        if (_refresh != null) _refresh.Enabled = false;
        if (_console != null) _console.Text = "";

        Task.Run(() =>
        {
            try
            {
                var text = ConsoleDump.Build() + BuildAndroidSection() + BuildLogSection();
                _report = text;

                // 通过 XTrace 同时写入日志文件、UI 文本控件与 logcat
                foreach (var line in text.Split('\n'))
                {
                    XTrace.WriteLine(line.TrimEnd('\r'));
                }
                XTrace.WriteLine("=== MobileConsole end ===");

                if (!_logDir.IsNullOrEmpty())
                {
                    try
                    {
                        File.WriteAllText(System.IO.Path.Combine(_logDir, "report.txt"), text);
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteLine("报告写入失败: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteLine("采集失败: " + ex);
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
        sb.AppendLine("说明: CPU/网络速度为实时采样值，空闲时可能为 0；Android 应用通常无法读取 /proc/stat");
        sb.AppendLine();

        return sb.ToString();
    }

    private String BuildLogSection()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[日志]");
        sb.AppendLine("目录: " + (_logDir.IsNullOrEmpty() ? "(不可用)" : _logDir));
        if (!_logDir.IsNullOrEmpty()) sb.AppendLine("报告: " + System.IO.Path.Combine(_logDir, "report.txt"));
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

    private sealed class TextViewLog : Logger
    {
        private readonly Action<String> _write;

        public TextViewLog(Action<String> write) => _write = write;

        protected override void OnWrite(LogLevel level, String format, params Object?[] args)
        {
            var message = Format(format, args);
            Log.Info(Tag, message);
            _write(message);
        }
    }

    private sealed class AndroidMachineInfoProvider : IMachineInfo
    {
        private readonly Activity _activity;

        public AndroidMachineInfoProvider(Activity activity) => _activity = activity;

        public void Init(MachineInfo info) { }

        public void Refresh(MachineInfo info)
        {
            try
            {
                if (_activity.GetSystemService(Context.ActivityService) is ActivityManager am)
                {
                    var memory = new ActivityManager.MemoryInfo();
                    am.GetMemoryInfo(memory);
                    if (memory.TotalMem > 0) info.Memory = (UInt64)memory.TotalMem;
                    if (memory.AvailMem > 0)
                    {
                        info.AvailableMemory = (UInt64)memory.AvailMem;
                        info.FreeMemory = (UInt64)memory.AvailMem;
                    }
                }
            }
            catch { }

            try
            {
                using var filter = new IntentFilter(Intent.ActionBatteryChanged);
                using var battery = _activity.RegisterReceiver(null, filter);
                if (battery == null) return;

                var level = battery.GetIntExtra(BatteryManager.ExtraLevel, -1);
                var scale = battery.GetIntExtra(BatteryManager.ExtraScale, -1);
                if (level >= 0 && scale > 0) info.Battery = (Double)level / scale;

                var temperature = battery.GetIntExtra(BatteryManager.ExtraTemperature, Int32.MinValue);
                if (temperature != Int32.MinValue) info.Temperature = temperature / 10.0;
            }
            catch { }
        }
    }
}
