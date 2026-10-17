using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using MobileSmoke;

namespace MobileSmoke.AndroidHost;

[Activity(Label = "MobileSmoke", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
public class MainActivity : Activity
{
    private TextView? _text;
    private Button? _button;
    private Boolean _running;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        SmokeEnvironment.DiagnosticLog = line => Log.Info("MobileSmoke", line);

        var root = new LinearLayout(this) { Orientation = Orientation.Vertical };
        _button = new Button(this) { Text = "Run smoke tests" };
        _text = new TextView(this)
        {
            TextSize = 11,
            Typeface = global::Android.Graphics.Typeface.Monospace,
        };
        _text.SetTextIsSelectable(true);
        _text.SetTextColor(global::Android.Graphics.Color.Black);

        var scroll = new ScrollView(this);
        scroll.AddView(_text, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

        root.AddView(_button, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));
        root.AddView(scroll, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1));

        SetContentView(root);
        _button.Click += (_, _) => RunSmoke();
        RunSmoke();
    }

    private void RunSmoke()
    {
        if (_running) return;
        _running = true;
        if (_button != null) _button.Enabled = false;
        if (_text != null) _text.Text = "";

        Task.Run(() =>
        {
            try
            {
                var report = SmokeRunner.RunAll(r =>
                {
                    var line = r.ToLine();
                    Log.Info("MobileSmoke", line);
                    Append(line);
                });

                var summary = report.Summary;
                Log.Info("MobileSmoke", summary);
                Append(summary);

                try
                {
                    report.Save();
                    Log.Info("MobileSmoke", "report=" + SmokeEnvironment.ReportJsonPath);
                }
                catch (Exception ex)
                {
                    Log.Error("MobileSmoke", "report save failed: " + ex);
                }
            }
            catch (Exception ex)
            {
                Log.Error("MobileSmoke", "fatal: " + ex);
                Append("[FAIL] fatal -> " + ex.Message);
            }
            finally
            {
                _running = false;
                RunOnUiThread(() => { if (_button != null) _button.Enabled = true; });
            }
        });
    }

    private void Append(String line)
    {
        RunOnUiThread(() =>
        {
            if (_text == null) return;
            _text.Append(line + "\n");
        });
    }
}
