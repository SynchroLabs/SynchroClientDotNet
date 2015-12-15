using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SynchroCore;

using MaaasClientWin;
using MaaasClientWin.Controls;
using Windows.UI.Xaml;

namespace SynchroCoreTest
{
    [TestClass]
    public class WinControlWrapperTest
    {
        WinPageView pageView = null;
        StateManager stateManager = null;

        JObject viewModelObj = new JObject()
        {
            {"num", new JValue(1)},
            {"str", new JValue("Words words words")},
            {"testStyle1", new JObject()
                {
                    { "attr1", new JValue("attr1fromStyle1") },
                    { "thicknessAttr", new JObject()
                        {
                            { "bottom", new JValue(9) }
                        }
                    },
                    { "font", new JObject()
                        {
                            { "face", new JValue("SanSerif") },
                            { "bold", new JValue(true) },
                            { "italic", new JValue(true) },
                        }
                    },
                    { "fontsize", new JValue(24) },
                }
            },
            {"testStyle2", new JObject()
                {
                    { "attr1", new JValue("attr1fromStyle2") },
                    { "attr2", new JValue("attr2fromStyle2") },
                    { "thicknessAttr", new JValue(10) },
                    { "font", new JObject()
                        {
                            { "size", new JValue(26) },
                        }
                    },
                }
            }
        };

        class TestFontSetter : FontSetter
        {
            public bool Bold = false;
            public FontFaceType FaceType = FontFaceType.FONT_DEFAULT;
            public bool Italic = false;
            public double Size = 12.0f;

            public override void SetBold(bool bold)
            {
                Bold = bold;
            }

            public override void SetFaceType(FontFaceType faceType)
            {
                FaceType = faceType;
            }

            public override void SetItalic(bool italic)
            {
                Italic = italic;
            }

            public override void SetSize(double size)
            {
                Size = size;
            }
        }

        class WinTestControlWrapper : WinControlWrapper
        {
            public String attr1;
            public String attr2;
            public Thickness thickness = new Thickness();
            public TestFontSetter fontSetter = new TestFontSetter();

            public WinTestControlWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
                base(parent, bindingContext, controlSpec)
            {
                processElementProperty(controlSpec, "attr1", value => attr1 = ToString(value));
                processElementProperty(controlSpec, "attr2", value => attr2 = ToString(value));
                processThicknessProperty(controlSpec, "thicknessAttr", () => thickness, value => thickness = (Thickness)value);
                processFontAttribute(controlSpec, fontSetter);
            }
        }

        [TestMethod]
        public void TestStyleExplicitNoStyle()
        {
            var viewModel = new ViewModel();
            viewModel.InitializeViewModelData(viewModelObj);

            var rootControl = new WinControlWrapper(pageView, stateManager, viewModel, viewModel.RootBindingContext, null);

            var controlSpec = new JObject() { { "attr1", new JValue("attr1val") } };
            var testControl = new WinTestControlWrapper(rootControl, rootControl.BindingContext, controlSpec);
            Assert.AreEqual("attr1val", testControl.attr1);
            Assert.AreEqual(null, testControl.attr2);
        }

        [TestMethod]
        public void TestStyleExplicitWithStyle()
        {
            var viewModel = new ViewModel();
            viewModel.InitializeViewModelData(viewModelObj);

            var rootControl = new WinControlWrapper(pageView, stateManager, viewModel, viewModel.RootBindingContext, null);

            var controlSpec = new JObject() { { "style", new JValue("testStyle1") }, { "attr1", new JValue("attr1val") } };
            var testControl = new WinTestControlWrapper(rootControl, rootControl.BindingContext, controlSpec);
            Assert.AreEqual("attr1val", testControl.attr1);
            Assert.AreEqual(null, testControl.attr2);
        }

        [TestMethod]
        public void TestStyleFromStyle()
        {
            var viewModel = new ViewModel();
            viewModel.InitializeViewModelData(viewModelObj);

            var rootControl = new WinControlWrapper(pageView, stateManager, viewModel, viewModel.RootBindingContext, null);

            var controlSpec = new JObject() { { "style", new JValue("testStyle1") } };
            var testControl = new WinTestControlWrapper(rootControl, rootControl.BindingContext, controlSpec);
            Assert.AreEqual("attr1fromStyle1", testControl.attr1);
            Assert.AreEqual(null, testControl.attr2);
        }

        [TestMethod]
        public void TestStyleFromStyles()
        {
            var viewModel = new ViewModel();
            viewModel.InitializeViewModelData(viewModelObj);

            var rootControl = new WinControlWrapper(pageView, stateManager, viewModel, viewModel.RootBindingContext, null);

            var controlSpec = new JObject() { { "style", new JValue("testStyle1, testStyle2") } };
            var testControl = new WinTestControlWrapper(rootControl, rootControl.BindingContext, controlSpec);
            Assert.AreEqual("attr1fromStyle1", testControl.attr1);
            Assert.AreEqual("attr2fromStyle2", testControl.attr2);
        }

        [TestMethod]
        public void TestStyleFromStylesPriority()
        {
            var viewModel = new ViewModel();
            viewModel.InitializeViewModelData(viewModelObj);

            var rootControl = new WinControlWrapper(pageView, stateManager, viewModel, viewModel.RootBindingContext, null);

            var controlSpec = new JObject() { { "style", new JValue("testStyle2, testStyle1") } };
            var testControl = new WinTestControlWrapper(rootControl, rootControl.BindingContext, controlSpec);
            Assert.AreEqual("attr1fromStyle2", testControl.attr1);
            Assert.AreEqual("attr2fromStyle2", testControl.attr2);
        }

        [TestMethod]
        public void TestStyleExplicitThicknessNoStyle()
        {
            var viewModel = new ViewModel();
            viewModel.InitializeViewModelData(viewModelObj);

            var rootControl = new WinControlWrapper(pageView, stateManager, viewModel, viewModel.RootBindingContext, null);

            var controlSpec = new JObject() { { "thicknessAttr", new JValue(5) } };
            var testControl = new WinTestControlWrapper(rootControl, rootControl.BindingContext, controlSpec);
            Assert.AreEqual(5, testControl.thickness.Top);
            Assert.AreEqual(5, testControl.thickness.Left);
            Assert.AreEqual(5, testControl.thickness.Bottom);
            Assert.AreEqual(5, testControl.thickness.Right);
        }

        [TestMethod]
        public void TestStyleExplicitThicknessObjNoStyle()
        {
            var viewModel = new ViewModel();
            viewModel.InitializeViewModelData(viewModelObj);

            var rootControl = new WinControlWrapper(pageView, stateManager, viewModel, viewModel.RootBindingContext, null);

            var controlSpec = new JObject() { { "thicknessAttr", new JObject() { { "top", new JValue(5) }, { "left", new JValue(6) }, { "bottom", new JValue(7) }, { "right", new JValue(8) } } } };
            var testControl = new WinTestControlWrapper(rootControl, rootControl.BindingContext, controlSpec);
            Assert.AreEqual(5, testControl.thickness.Top);
            Assert.AreEqual(6, testControl.thickness.Left);
            Assert.AreEqual(7, testControl.thickness.Bottom);
            Assert.AreEqual(8, testControl.thickness.Right);
        }

        [TestMethod]
        public void TestStyleExplicitThicknessObjAndStyles()
        {
            var viewModel = new ViewModel();
            viewModel.InitializeViewModelData(viewModelObj);

            var rootControl = new WinControlWrapper(pageView, stateManager, viewModel, viewModel.RootBindingContext, null);

            var controlSpec = new JObject() { { "style", new JValue("testStyle1, testStyle2") }, { "thicknessAttr", new JObject() { { "top", new JValue(5) }, { "left", new JValue(6) } } } };
            var testControl = new WinTestControlWrapper(rootControl, rootControl.BindingContext, controlSpec);
            Assert.AreEqual(5, testControl.thickness.Top);
            Assert.AreEqual(6, testControl.thickness.Left);
            Assert.AreEqual(9, testControl.thickness.Bottom);
            Assert.AreEqual(10, testControl.thickness.Right);
        }

        [TestMethod]
        public void TestStyleExplicitFontSize()
        {
            var viewModel = new ViewModel();
            viewModel.InitializeViewModelData(viewModelObj);

            var rootControl = new WinControlWrapper(pageView, stateManager, viewModel, viewModel.RootBindingContext, null);

            var controlSpec = new JObject() { { "fontsize", new JValue(20) } };
            var testControl = new WinTestControlWrapper(rootControl, rootControl.BindingContext, controlSpec);
            Assert.AreEqual(20, testControl.fontSetter.Size);
        }

        [TestMethod]
        public void TestStyleExplicitFontSizeFromObject()
        {
            var viewModel = new ViewModel();
            viewModel.InitializeViewModelData(viewModelObj);

            var rootControl = new WinControlWrapper(pageView, stateManager, viewModel, viewModel.RootBindingContext, null);

            var controlSpec = new JObject() { { "font", new JObject() { { "size", new JValue(22) } } } };
            var testControl = new WinTestControlWrapper(rootControl, rootControl.BindingContext, controlSpec);
            Assert.AreEqual(22, testControl.fontSetter.Size);
        }

        [TestMethod]
        public void TestStyleFontFromStyle()
        {
            var viewModel = new ViewModel();
            viewModel.InitializeViewModelData(viewModelObj);

            var rootControl = new WinControlWrapper(pageView, stateManager, viewModel, viewModel.RootBindingContext, null);

            var controlSpec = new JObject() { { "style", new JValue("testStyle1, testStyle2") } };
            var testControl = new WinTestControlWrapper(rootControl, rootControl.BindingContext, controlSpec);
            Assert.AreEqual(24, testControl.fontSetter.Size);
            Assert.AreEqual(true, testControl.fontSetter.Bold);
            Assert.AreEqual(true, testControl.fontSetter.Italic);
            Assert.AreEqual(FontFaceType.FONT_SANSERIF, testControl.fontSetter.FaceType);
        }

        [TestMethod]
        public void TestStyleFontFromStylePriority()
        {
            var viewModel = new ViewModel();
            viewModel.InitializeViewModelData(viewModelObj);

            var rootControl = new WinControlWrapper(pageView, stateManager, viewModel, viewModel.RootBindingContext, null);

            var controlSpec = new JObject() { { "style", new JValue("testStyle2, testStyle1") } };
            var testControl = new WinTestControlWrapper(rootControl, rootControl.BindingContext, controlSpec);
            Assert.AreEqual(26, testControl.fontSetter.Size);
            Assert.AreEqual(true, testControl.fontSetter.Bold);
            Assert.AreEqual(true, testControl.fontSetter.Italic);
            Assert.AreEqual(FontFaceType.FONT_SANSERIF, testControl.fontSetter.FaceType);
        }

        [TestMethod]
        public void TestStyleFontFromStyleExplicitOverride()
        {
            var viewModel = new ViewModel();
            viewModel.InitializeViewModelData(viewModelObj);

            var rootControl = new WinControlWrapper(pageView, stateManager, viewModel, viewModel.RootBindingContext, null);

            var controlSpec = new JObject() { { "style", new JValue("testStyle1") }, { "font", new JObject() { { "size", new JValue(28) }, { "italic", new JValue(false) } } } };
            var testControl = new WinTestControlWrapper(rootControl, rootControl.BindingContext, controlSpec);
            Assert.AreEqual(28, testControl.fontSetter.Size);
            Assert.AreEqual(true, testControl.fontSetter.Bold);
            Assert.AreEqual(false, testControl.fontSetter.Italic);
            Assert.AreEqual(FontFaceType.FONT_SANSERIF, testControl.fontSetter.FaceType);
        }
    }
}

