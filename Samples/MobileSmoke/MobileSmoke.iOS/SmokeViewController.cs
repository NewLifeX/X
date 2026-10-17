using Foundation;
using UIKit;

namespace MobileSmoke.iOSHost;

public sealed class SmokeViewController : UIViewController
{
    private UITextView? _text;
    private Boolean _started;

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        View!.BackgroundColor = UIColor.SystemBackground;

        _text = new UITextView(View.Bounds)
        {
            Editable = false,
            Font = UIFont.GetMonospacedSystemFont(11, UIFontWeight.Regular),
            AutoresizingMask = UIViewAutoresizing.All,
            TextColor = UIColor.Label,
        };
        View.AddSubview(_text);
    }

    public override void ViewDidAppear(Boolean animated)
    {
        base.ViewDidAppear(animated);
        if (_started) return;

        _started = true;
        RunSmoke();
    }

    private void RunSmoke()
    {
        Task.Run(() =>
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
