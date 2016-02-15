using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SynchroCore;

namespace SynchroCoreTest
{
    [TestClass]
    public class CommandInstanceTest
    {
        JObject viewModel = new JObject()
        {
            { "serial", new JValue(69) },
            { "title", new JValue("The Title") },
            { "board", new JArray() // A 2x2 array
                {
                    new JArray()
                    {
                        new JObject(){ {"name", new JValue("s00") } },
                        new JObject(){ {"name", new JValue("s01") } },
                    },
                    new JArray()
                    {
                        new JObject(){ {"name", new JValue("s10") } },
                        new JObject(){ {"name", new JValue("s11") } },
                    }
                }
            },
        };

        [TestMethod]
        public void TestResolveParameters()
        {
            var cmdInst = new CommandInstance("TestCmd");
            var bindingCtx = new BindingContext(viewModel);

            cmdInst.SetParameter("Literal", new JValue("literal"));
            cmdInst.SetParameter("Serial", new JValue("{serial}"));
            cmdInst.SetParameter("Title", new JValue("{title}"));
            cmdInst.SetParameter("Empty", new JValue(""));
            cmdInst.SetParameter("Obj", new JValue("{board[0][1]}"));
            cmdInst.SetParameter("NULL", new JValue(null));          // This can't happen in nature, but just for fun...
            cmdInst.SetParameter("Parent", new JValue("{$parent}")); // Token that can't be resolved ($parent from root)
            cmdInst.SetParameter("Nonsense", new JValue("{foo}"));   // Token that can't be resolved

            var resolvedParams = cmdInst.GetResolvedParameters(bindingCtx);
            Assert.AreEqual((string)(JValue)resolvedParams["Literal"], "literal");
            Assert.AreEqual((int)(JValue)resolvedParams["Serial"], 69);
            Assert.AreEqual((string)(JValue)resolvedParams["Title"], "The Title");
            Assert.AreEqual((string)(JValue)resolvedParams["Empty"], "");
            Assert.IsTrue(resolvedParams["Obj"].DeepEquals(viewModel["board"].SelectToken("0").SelectToken("1")));
            Assert.AreEqual(resolvedParams["NULL"].Type, JTokenType.Null);
            Assert.AreEqual(resolvedParams["Parent"].Type, JTokenType.Null);
            Assert.AreEqual(resolvedParams["Nonsense"].Type, JTokenType.Null);
        }
    }
}
