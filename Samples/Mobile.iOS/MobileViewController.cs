using Foundation;
using UIKit;

namespace Mobile.iOSHost;

public sealed class MobileViewController : UIViewController
{
    private UILabel? _titleLabel;
    private UITextView? _text;
    private UIButton? _infoButton;
    private UIButton? _smokeButton;
    private Boolean _started;
    private Boolean _running;

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        View!.BackgroundColor = UIColor.SystemBackground;
        Title = "新生命移动测试台";

        _titleLabel = new UILabel
        {
            Text = "新生命移动测试台",
            Font = UIFont.BoldSystemFontOfSize(20) ?? UIFont.SystemFontOfSize(20)!,
            TextColor = UIColor.Label,
            TextAlignment = UITextAlignment.Left,
        };
        View.AddSubview(_titleLabel);

        _infoButton = UIButton.FromType(UIButtonType.System);
        _infoButton.SetTitle("系统信息", UIControlState.Normal);
        _infoButton.TouchUpInside += (_, _) => RunInfo();
        View.AddSubview(_infoButton);

        _smokeButton = UIButton.FromType(UIButtonType.System);
        _smokeButton.SetTitle("运行冒烟", UIControlState.Normal);
        _smokeButton.TouchUpInside += (_, _) => RunSmoke();
        View.AddSubview(_smokeButton);

        _text = new UITextView
        {
            Editable = false,
            Font = UIFont.GetMonospacedSystemFont(11, UIFontWeight.Regular),
            TextColor = UIColor.Label,
            BackgroundColor = UIColor.SystemBackground,
        };
        View.AddSubview(_text);
    }

    public override void ViewDidLayoutSubviews()
    {
        base.ViewDidLayoutSubviews();
        var bounds = View!.Bounds;
        var top = View.SafeAreaInsets.Top + 8;

        _titleLabel!.Frame = new CGRect(16, top, bounds.Width - 32, 30);
        var buttonTop = top + 38;
        _infoButton!.Frame = new CGRect(12, buttonTop, bounds.Width / 2 - 16, 36);
        _smokeButton!.Frame = new CGRect(bounds.Width / 2 + 4, buttonTop, bounds.Width / 2 - 16, 36);

        var textTop = buttonTop + 44;
        _text!.Frame = new CGRect(0, textTop, bounds.Width, bounds.Height - textTop);
    }

    public override void ViewDidAppear(Boolean animated)
    {
        base.ViewDidAppear(animated);
        if (_started) return;

        _started = true;
        RunInfo();

        if (NSProcessInfo.ProcessInfo.Arguments.Any(e => e.Equals("--smoke", StringComparison.OrdinalIgnoreCase)))
            RunSmoke();
    }

    private void RunInfo()
    {
        if (_running) return;

        var text = MachineInfoReport.Build();
        if (_text != null) _text.Text = text;
        Console.WriteLine("=== Info end ===");
    }

    private void RunSmoke()
    {
        if (_running) return;
        _running = true;

        Append("");
        Append("=== Smoke ===");

        Task.Run(() =>
        {
            try
            {
                var report = SmokeRunner.RunAll(r => Append(r.ToLine()));
                Append(report.Summary);

                try
                {
                    report.Save();
                    Append("report=" + SmokeEnvironment.ReportJsonPath);
                }
                catch (Exception ex)
                {
                    Append("report save failed: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                Append("[FAIL] fatal -> " + ex.Message);
            }
            finally
            {
                _running = false;
            }
        });
    }

    private void Append(String line)
    {
        Console.WriteLine(line);
        InvokeOnMainThread(() =>
        {
            if (_text == null) return;

            _text.Text += line + "\n";
            var length = _text.Text.Length;
            if (length > 0) _text.ScrollRangeToVisible(new NSRange(length - 1, 1));
        });
    }
}
