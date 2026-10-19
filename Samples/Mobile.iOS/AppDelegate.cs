using Foundation;
using NewLife;
using ObjCRuntime;
using UIKit;

namespace Mobile.iOSHost;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate
{
    public override Boolean FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        MachineInfo.Provider = new IOSMachineInfoProvider();
        return true;
    }

    public override UISceneConfiguration GetConfiguration(UIApplication application, UISceneSession connectingSceneSession, UISceneConnectionOptions options)
        => new("Default Configuration", connectingSceneSession.Role);

    public override void DidDiscardSceneSessions(UIApplication application, NSSet<UISceneSession> sceneSessions) { }
}
