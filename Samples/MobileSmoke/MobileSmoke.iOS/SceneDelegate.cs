using Foundation;
using ObjCRuntime;
using UIKit;

namespace MobileSmoke.iOSHost;

[Register("SceneDelegate")]
public class SceneDelegate : UIResponder, IUIWindowSceneDelegate
{
    [Export("window")]
    public UIWindow? Window { get; set; }

    [Export("scene:willConnectToSession:options:")]
    public void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        if (scene is not UIWindowScene windowScene) return;

        Window ??= new UIWindow(windowScene);
        Window.RootViewController = new SmokeViewController();
        Window.MakeKeyAndVisible();
    }
}
