using Foundation;
using UIKit;
#if STARDUST
using Mobile.Stardust;
#endif

namespace Mobile.iOSHost;

public sealed class MobileViewController : UIViewController
{
    private UILabel? _titleLabel;
    private UITextView? _text;
    private UIButton? _infoButton;
    private UIButton? _smokeButton;
    private Boolean _started;
    private Boolean _running;
#if STARDUST
    private UIButton? _stardustButton;
#endif

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

#if STARDUST
        _stardustButton = UIButton.FromType(UIButtonType.System);
        _stardustButton.SetTitle("连接星尘", UIControlState.Normal);
        _stardustButton.TouchUpInside += (_, _) => ToggleStardust();
        View.AddSubview(_stardustButton);
#endif

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
        var buttonWidth = (bounds.Width - 24) / 3;
        _infoButton!.Frame = new CGRect(8, buttonTop, buttonWidth, 36);
        _smokeButton!.Frame = new CGRect(12 + buttonWidth, buttonTop, buttonWidth, 36);
#if STARDUST
        _stardustButton!.Frame = new CGRect(16 + buttonWidth * 2, buttonTop, buttonWidth, 36);
#endif

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

#if STARDUST
        if (NSProcessInfo.ProcessInfo.Arguments.Any(e => e.Equals("--stardust", StringComparison.OrdinalIgnoreCase)))
            ToggleStardust();
#endif
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

#if STARDUST
    private void ToggleStardust()
    {
        if (_stardustButton != null) _stardustButton.Enabled = false;

        Task.Run(() =>
        {
            var on = MobileStardust.Toggle(Append);
            InvokeOnMainThread(() =>
            {
                if (_stardustButton != null)
                {
                    _stardustButton.SetTitle(on ? "断开星尘" : "连接星尘", UIControlState.Normal);
                    _stardustButton.Enabled = true;
                }
            });
        });
    }
#endif

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
