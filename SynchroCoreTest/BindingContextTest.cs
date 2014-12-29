using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SynchroCore;

namespace SynchroCoreTest
{
    [TestClass]
    public class BindingContextTest
    {
        JObject viewModel = new JObject()
        {
            { "serial", new JValue(0) },
            { "title", new JValue("Colors") },
            { "colors", new JArray()
                {
                    new JObject(){ {"name", new JValue("Red") }, {"color", new JValue("red") }, {"value", new JValue("0xff0000")} },
                    new JObject(){ {"name", new JValue("Green") }, {"color", new JValue("green") }, {"value", new JValue("0x00ff00")} },
                    new JObject(){ {"name", new JValue("Blue") }, {"color", new JValue("blue") }, {"value", new JValue("0x0000ff")} }
                }
            }
        };

        [TestMethod]
        public void TestSelectChild()
        {
            var bindingCtx = new BindingContext(viewModel);

            var titleCtx = bindingCtx.Select("title");
            Assert.IsTrue(titleCtx.GetValue().DeepEquals(viewModel["title"]));
        }

        [TestMethod]
        public void testSelectChildren()
        {
            var bindingCtx = new BindingContext(viewModel);

            var colorsCtx = bindingCtx.Select("colors");
            var colors = colorsCtx.SelectEach("");
            Assert.AreEqual(3, colors.Count);
        }

        [TestMethod]
        public void testSelectChildWithPath()
        {
            var bindingCtx = new BindingContext(viewModel);
        
            Assert.AreEqual("Green", (string)bindingCtx.Select("colors[1].name").GetValue());
        }

        [TestMethod]
        public void testDataElement()
        {
            var bindingCtx = new BindingContext(viewModel);
        
            Assert.AreEqual("Green", (string)bindingCtx.Select("colors[1].name").Select("$data").GetValue());
        }

        [TestMethod]
        public void testParentElement()
        {
            var bindingCtx = new BindingContext(viewModel);
        
            Assert.AreEqual("Colors", (string)bindingCtx.Select("colors[1].name").Select("$parent.$parent.title").GetValue());
        }

        [TestMethod]
        public void testRootElement()
        {
            var bindingCtx = new BindingContext(viewModel);
        
            Assert.AreEqual("Colors", (string)bindingCtx.Select("colors[1].name").Select("$root.title").GetValue());
        }

        [TestMethod]
        public void testIndexElementOnArrayItem()
        {
            var bindingCtx = new BindingContext(viewModel);
        
            Assert.AreEqual(1, (int)bindingCtx.Select("colors[1]").Select("$index").GetValue());
        }

        [TestMethod]
        public void testIndexElementInsideArrayItem()
        {
            var bindingCtx = new BindingContext(viewModel);
        
            Assert.AreEqual(1, (int)bindingCtx.Select("colors[1].name").Select("$index").GetValue());
        }

        [TestMethod]
        public void testSetValue()
        {
            JObject testViewModel = (JObject)viewModel.DeepClone();
            ((JArray)testViewModel["colors"])[1] = new JObject(){ {"name", new JValue("Greenish")}, {"color", new JValue("green")}, {"value", new JValue("0x00ff00")} };
            Assert.IsFalse(testViewModel.DeepEquals(viewModel));
        
            var bindingCtx = new BindingContext(testViewModel);
            var colorNameCtx = bindingCtx.Select("colors[1].name");

            colorNameCtx.SetValue(new JValue("Green"));
            Assert.IsTrue(testViewModel.DeepEquals(viewModel));
        }

        [TestMethod]
        public void testRebind()
        {
            JObject testViewModel = (JObject)viewModel.DeepClone();

            var bindingCtx = new BindingContext(testViewModel);
            var colorNameCtx = bindingCtx.Select("colors[1].name");
        
            ((JArray)testViewModel["colors"])[1] = new JObject(){ {"name", new JValue("Purple")}, {"color", new JValue("purp")}, {"value", new JValue("0x696969")} };

            Assert.AreEqual("Green", (string)colorNameCtx.GetValue());
            colorNameCtx.Rebind();
            Assert.AreEqual("Purple", (string)colorNameCtx.GetValue());
        }
    }
}
