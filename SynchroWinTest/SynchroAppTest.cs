using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SynchroCore;
using System.Threading.Tasks;

namespace SynchroCoreTest
{
    public class TestAppManager : MaaasAppManager
    {
        string _bundledState = @"
        {
          /*
          ""seed"":
          {
            ""endpoint"": ""api.synchro.io/api/samples"",
            ""definition"": { ""name"": ""synchro-samples"", ""description"": ""Synchro API Samples"" }
          }
          */
          ""apps"":
          [
            {
              ""endpoint"": ""api.synchro.io/api/samples"",
              ""definition"": { ""name"": ""synchro-samples"", ""description"": ""Synchro API Samples"" }
            }
          ]
        }
        ";

        string _localState = null;

        protected override Task<string> loadBundledState()
        {
            return Task.FromResult(_bundledState);
        }

        protected override Task<string> loadLocalState()
        {
            return Task.FromResult(_localState);
        }

        protected override Task<bool> saveLocalState(string state)
        {
            _localState = state;
            return Task.FromResult(true);
        }
    }


    [TestClass]
    public class SynchroAppTest
    {
        [TestMethod]
        public async Task TestLoadBundledState()
        {
            var appManager = new TestAppManager();
        
            await appManager.loadState();
        
            Assert.IsNull(appManager.AppSeed);
            Assert.AreEqual(1, appManager.Apps.Count);
        
            var app = appManager.Apps[0];
                
            Assert.AreEqual("synchro-samples", app.Name);
            Assert.AreEqual("Synchro API Samples", app.Description);
            Assert.AreEqual("api.synchro.io/api/samples", app.Endpoint);
        
            var expected = new JObject()
            {
                { "name", new JValue("synchro-samples") },
                { "description", new JValue("Synchro API Samples") }
            };
                
            Assert.IsTrue(app.AppDefinition.DeepEquals(expected));
        }
    }
}
