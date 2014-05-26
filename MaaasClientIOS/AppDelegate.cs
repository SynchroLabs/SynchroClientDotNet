using System;
using System.Collections.Generic;
using System.Linq;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MaaasCore;
using System.Threading.Tasks;

namespace MaaasClientIOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate
    {
        UIWindow window;
        UIViewController viewController;

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            var rootNavigationController = new UINavigationController(); 

            window = new UIWindow(UIScreen.MainScreen.Bounds);

            MaaasAppManager appManager = new iOSAppManager();

            // Since we can't mark this method as async, we have to manually wait on the (possibly) asynchronous 
            // app manager state loader to finish before we use the app manager...
            //
            Task stateLoader = appManager.loadState();
            stateLoader.Wait();

            if (appManager.AppSeed != null)
            {
                viewController = new MaaasPageViewController(appManager.AppSeed);
            }
            else
            {
                viewController = new LauncherViewController(appManager);
            }

            rootNavigationController.PushViewController(viewController, false);
            window.RootViewController = rootNavigationController; // !!! viewController;
            window.MakeKeyAndVisible();

            return true;
        }
    }
}
