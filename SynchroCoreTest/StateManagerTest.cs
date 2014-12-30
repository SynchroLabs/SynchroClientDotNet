using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SynchroCore;
using System.Threading.Tasks;
using System.Threading;

namespace SynchroCoreTest
{
    [TestClass]
    public class StateManagerTest
    {
        public class TestDeviceMetrics : MaaasDeviceMetrics
        {
            public override MaaasOrientation CurrentOrientation
            {
                get { return MaaasOrientation.Portrait; }
            }
        }

        // This could probably be more thourough.  We create a StateManager and use it to connect to the local server, starting
        // the samples app (which consists of getting the app definition from the server, then getting the "main" page), then on
        // receipt of that page (the Menu page), issue a command which navigates to the Hello page.
        //
        [TestMethod]
        public async Task TestStateManager()
        {
            // Force fresh load of AppManager from the bundled seed...
            //
            var appManager = new TestAppManager();
        
            var app = new MaaasApp(
                endpoint: "localhost:1337/api/samples",
                appDefinition: new JObject(){ {"name", new JValue("synchro-samples")}, {"description", new JValue("Synchro API Samples")} },
                sessionId: null
            );
        
            appManager.Apps.Add(app);
        
            var transport = new TransportHttp(uri: new Uri("http://" + app.Endpoint));
        
            var stateManager = new StateManager(appManager: appManager, app: app, transport: transport, deviceMetrics: new TestDeviceMetrics());
        
            AutoResetEvent AsyncCallComplete = new AutoResetEvent(false);
            var responseNumber = 0;
            JObject thePageView = null;
        
            ProcessPageView processPageView = (JObject pageView) =>
            {
                responseNumber++;
                thePageView = pageView;
                AsyncCallComplete.Set();
            };

            ProcessMessageBox processMessageBox = (JObject messageBox, CommandHandler commandHandler) =>
            {
               Assert.Fail("Unexpected message box call in test");
            };

            stateManager.SetProcessingHandlers(processPageView, processMessageBox);
            await stateManager.startApplicationAsync();

            AsyncCallComplete.WaitOne();

            Assert.AreEqual(1, responseNumber);
            AsyncCallComplete.Reset();

            await stateManager.sendCommandRequestAsync("goToView", parameters: new JObject(){{"view", new JValue("hello")}});

            AsyncCallComplete.WaitOne();

            Assert.AreEqual(2, responseNumber);
            Assert.AreEqual("Hello World", (string)thePageView["title"]);
        }
    }
}
