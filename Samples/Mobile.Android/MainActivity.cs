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
using NewLife;
using NewLife.Log;
#if STARDUST
using Mobile.Stardust;
#endif

namespace Mobile.AndroidHost;

/// <summary>把整个屏幕当作控制台：系统信息、冒烟测试、XTrace 日志三合一。</summary>
[Activity(Label = "新生命测试台", MainLauncher = true, Theme = "@android:style/Theme.Material.NoActionBar",
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
public class MainActivity : Activity
{
    private const String Tag = "Mobile";
    private const String Background = "#0B0F14";
    private const String Foreground = "#D7E2EA";

    private TextView? _console;
    private Button? _infoButton;
    private Button? _smokeButton;
    private Button? _copyButton;
    private Button? _clearButton;
    private Boolean _running;
    private Boolean _autoSmoke;
    private String _logDir = "";
#if STARDUST
    private Button? _stardustButton;
    private Boolean _autoStardust;
#endif

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        _autoSmoke = Intent?.GetBooleanExtra("smoke", false) == true;
#if STARDUST
        _autoStardust = Intent?.GetBooleanExtra("stardust", false) == true;
#endif

        var root = new LinearLayout(this) { Orientation = Orientation.Vertical };
        root.SetBackgroundColor(Color.ParseColor(Background));

        var bar = new LinearLayout(this) { Orientation = Orientation.Vertical };
        bar.SetPadding(Dp(12), Dp(4), Dp(12), Dp(4));

        var row1 = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        row1.SetGravity(GravityFlags.CenterVertical);

        var title = new TextView(this) { Text = "新生命移动测试台", TextSize = 15 };
        title.SetTextColor(Color.ParseColor("#7FD1AE"));
        title.SetTypeface(Typeface.Monospace, TypefaceStyle.Bold);
        row1.AddView(title, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1));

        _infoButton = new Button(this) { Text = "系统信息" };
        _infoButton.Click += (_, _) => RunInfo();
        row1.AddView(_infoButton);

        _smokeButton = new Button(this) { Text = "运行冒烟" };
        _smokeButton.Click += (_, _) => RunSmoke();
        row1.AddView(_smokeButton);

        var row2 = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        _copyButton = new Button(this) { Text = "复制" };
        _copyButton.Click += (_, _) => CopyReport();
        row2.AddView(_copyButton, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1));

        _clearButton = new Button(this) { Text = "清空" };
        _clearButton.Click += (_, _) => ClearReport();
        row2.AddView(_clearButton, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1));

#if STARDUST
        _stardustButton = new Button(this) { Text = "连接星尘" };
        _stardustButton.Click += (_, _) => ToggleStardust();
        row2.AddView(_stardustButton, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1));
#endif

        bar.AddView(row1, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
        bar.AddView(row2, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

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
        SmokeEnvironment.DiagnosticLog = AppendSmokeLine;
        SetupLogging();
        RunInfo();

#if STARDUST
        if (_autoStardust) ToggleStardust();
#endif
    }

    private void SetupLogging()
    {
        var logs = new CompositeLog(new TextViewLog(Append));

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

    private void RunInfo()
    {
        if (_running) return;
        _running = true;
        SetBusy(true);
        if (_console != null) _console.Text = "";

        Task.Run(() =>
        {
            try
            {
                var text = MachineInfoReport.Build() + BuildAndroidSection() + BuildLogSection();
                foreach (var line in text.Split('\n'))
                {
                    XTrace.WriteLine(line.TrimEnd('\r'));
                }
                XTrace.WriteLine("=== Info end ===");

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
                SetBusy(false);

                if (_autoSmoke)
                {
                    _autoSmoke = false;
                    RunSmoke();
                }
            }
        });
    }

    private void RunSmoke()
    {
        if (_running) return;
        _running = true;
        SetBusy(true);
        Append("");
        Append("=== Smoke ===");
        Log.Info(Tag, "=== Smoke ===");

        Task.Run(() =>
        {
            try
            {
                var report = SmokeRunner.RunAll(r =>
                {
                    var line = r.ToLine();
                    Log.Info(Tag, line);
                    Append(line);
                });

                var summary = report.Summary;
                Log.Info(Tag, summary);
                Append(summary);

                try
                {
                    report.Save();
                    Log.Info(Tag, "report=" + SmokeEnvironment.ReportJsonPath);
                }
                catch (Exception ex)
                {
                    Log.Error(Tag, "smoke report save failed: " + ex);
                }

                if (!_logDir.IsNullOrEmpty())
                {
                    try
                    {
                        File.WriteAllText(System.IO.Path.Combine(_logDir, "smoke.txt"), report.ToText());
                    }
                    catch (Exception ex)
                    {
                        Log.Error(Tag, "smoke text save failed: " + ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(Tag, "smoke fatal: " + ex);
                Append("[FAIL] fatal -> " + ex.Message);
            }
            finally
            {
                _running = false;
                SetBusy(false);
            }
        });
    }

    private void AppendSmokeLine(String line)
    {
        Log.Info(Tag, line);
        Append(line);
    }

    private void Append(String line) => RunOnUiThread(() =>
    {
        if (_console == null) return;
        _console.Append(line + "\n");
    });

#if STARDUST
    private void ToggleStardust()
    {
        if (_stardustButton != null) _stardustButton.Enabled = false;

        Task.Run(() =>
        {
            var on = MobileStardust.Toggle(Append);
            RunOnUiThread(() =>
            {
                if (_stardustButton != null)
                {
                    _stardustButton.Text = on ? "断开星尘" : "连接星尘";
                    _stardustButton.Enabled = true;
                }
            });
        });
    }
#endif

    private void SetBusy(Boolean busy) => RunOnUiThread(() =>
    {
        if (_infoButton != null) _infoButton.Enabled = !busy;
        if (_smokeButton != null) _smokeButton.Enabled = !busy;
        if (_copyButton != null) _copyButton.Enabled = !busy;
        if (_clearButton != null) _clearButton.Enabled = !busy;
    });

    private String BuildAndroidSection()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Android 原生信息]");
        sb.AppendLine("制造商: " + Build.Manufacturer);
        sb.AppendLine("品牌: " + Build.Brand);
        sb.AppendLine("型号: " + Build.Model);
        sb.AppendLine("市场名称: " + (AndroidMachineInfoProvider.GetMarketName() ?? ""));
        sb.AppendLine("设备名称: " + (AndroidMachineInfoProvider.GetDeviceName(ContentResolver) ?? ""));
        sb.AppendLine("序列号: " + (AndroidMachineInfoProvider.GetSerial() ?? "(不可读)"));
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

        // Android 10 起普通应用无法读取 IMEI/MEID，Serial 也通常返回 unknown
        sb.AppendLine("IMEI/MEID: Android 10+ 普通应用受系统限制不可读取");

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
        if (!_logDir.IsNullOrEmpty())
        {
            sb.AppendLine("报告: " + System.IO.Path.Combine(_logDir, "report.txt"));
            sb.AppendLine("冒烟: " + System.IO.Path.Combine(_logDir, "smoke.txt"));
        }
        sb.AppendLine();

        return sb.ToString();
    }

    private void CopyReport()
    {
        var text = _console?.Text;
        if (text.IsNullOrEmpty()) return;

        try
        {
            if (GetSystemService(ClipboardService) is ClipboardManager clipboard)
            {
                clipboard.PrimaryClip = ClipData.NewPlainText("Mobile", text);
                Toast.MakeText(this, "已复制", ToastLength.Short)?.Show();
            }
        }
        catch (Exception ex)
        {
            Log.Error(Tag, "copy failed: " + ex.Message);
        }
    }

    private void ClearReport()
    {
        if (_console != null) _console.Text = "";
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
        private static String? _marketName;
        private readonly Activity _activity;

        public AndroidMachineInfoProvider(Activity activity) => _activity = activity;

        public void Init(MachineInfo info)
        {
            // Build.Model 是 YAL-AL10 之类的型号代码，优先取厂商提供的市场名称
            var model = GetMarketName();
            if (!model.IsNullOrEmpty()) info.Product = model;

            if (!Build.Manufacturer.IsNullOrEmpty()) info.Vendor = Build.Manufacturer;

            // Android 10+ 普通应用通常读不到 Serial；能读到就作为硬件唯一标识，否则保留 android_id
            var serial = GetSerial();
            if (!serial.IsNullOrEmpty())
            {
                info.Serial = serial;
                info.UUID = serial;
            }
            else if (info.UUID.IsNullOrEmpty() || info.UUID.StartsWith("0-"))
            {
                // android_id 也不可用时，用应用私有目录持久化的 GUID，保证重启后不变
                var id = GetPersistedDeviceId();
                if (!id.IsNullOrEmpty())
                {
                    info.UUID = id;
                    if (info.Guid.IsNullOrEmpty() || info.Guid.StartsWith("0-")) info.Guid = id;
                }
            }

            var deviceName = GetDeviceName(_activity.ContentResolver);
            if (!deviceName.IsNullOrEmpty()) info["DeviceName"] = deviceName;

            if (!Build.Board.IsNullOrEmpty()) info.Board = Build.Board;
        }

        /// <summary>读取厂商市场名称，读取一次后缓存</summary>
        /// <returns>如 HONOR 20 PRO；读取失败时退化为 Build.Model</returns>
        internal static String? GetMarketName()
        {
            if (!_marketName.IsNullOrEmpty()) return _marketName;

            var name = GetProp("ro.product.marketname");
            if (name.IsNullOrEmpty()) name = GetProp("ro.config.marketing_name");
            if (name.IsNullOrEmpty()) name = Build.Model;

            _marketName = name;
            return name;
        }

        /// <summary>读取 Android 系统属性</summary>
        /// <param name="name">属性名，如 ro.product.marketname</param>
        /// <returns>属性值，读取失败时返回 null</returns>
        internal static String? GetProp(String name)
        {
            try
            {
                using var process = Java.Lang.Runtime.GetRuntime()?.Exec(["getprop", name]);
                if (process?.InputStream == null) return null;

                using var reader = new Java.IO.BufferedReader(new Java.IO.InputStreamReader(process.InputStream));
                var value = reader.ReadLine()?.Trim();
                process.WaitFor();

                return value;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>读取设备序列号</summary>
        /// <returns>序列号，Android 10+ 通常不可读</returns>
        internal static String? GetSerial()
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                try
                {
                    var serial = Build.GetSerial();
                    if (!serial.IsNullOrEmpty() && !serial.EqualIgnoreCase("unknown")) return serial;
                }
                catch { }
            }

            var prop = GetProp("ro.serialno");
            return !prop.IsNullOrEmpty() && !prop.EqualIgnoreCase("unknown") ? prop : null;
        }

        /// <summary>读取应用私有目录中持久化的设备标识</summary>
        /// <returns>首次调用时生成并保存，后续保持不变</returns>
        private String? GetPersistedDeviceId()
        {
            try
            {
                var sp = _activity.GetSharedPreferences("mobile", FileCreationMode.Private);
                var id = sp?.GetString("device_id", null);
                if (id.IsNullOrEmpty())
                {
                    id = Guid.NewGuid().ToString("N");
                    sp?.Edit()?.PutString("device_id", id)?.Apply();
                }

                return id;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>读取系统设置中的设备名称</summary>
        /// <param name="resolver">内容解析器</param>
        /// <returns>设备名称，读取失败时返回 null</returns>
        internal static String? GetDeviceName(ContentResolver? resolver)
        {
            if (resolver == null) return null;

            try
            {
                var name = Settings.Secure.GetString(resolver, "bluetooth_name");
                if (!name.IsNullOrEmpty()) return name;
            }
            catch { }

            try
            {
                var name = Settings.Global.GetString(resolver, "device_name");
                if (!name.IsNullOrEmpty()) return name;
            }
            catch { }

            var prop = GetProp("persist.sys.device_name");
            if (!prop.IsNullOrEmpty()) return prop;

            return GetProp("ro.product.marketname");
        }

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
