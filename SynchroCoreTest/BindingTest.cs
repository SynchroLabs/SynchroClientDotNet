using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SynchroCore;

namespace SynchroCoreTest
{
    [TestClass]
    public class BindingTest
    {
        [TestMethod]
        public void TestBindingHelperPromoteValue()
        {
            // For an edit control with a default binding attribute of "value" a binding of:
            //
            //     binding: "username"
            //
            var controlSpec = new JObject(){ {"binding", new JValue("username")} };

            // becomes
            //
            //     binding: { value: "username" }
            //
            var expectedBindingSpec = new JObject(){ {"value", new JValue("username")} };

            var bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, defaultBindingAttribute: "value");
            Assert.IsTrue(bindingSpec.DeepEquals(expectedBindingSpec));
        }

        [TestMethod]
        public void TestBindingHelperPromoteImplicitCommand()
        {
            // For commands:
            //
            //     binding: "doSomething"
            //
            var controlSpec = new JObject(){ {"binding", new JValue("doSomething")} };

            // becomes
            //
            //     binding: { onClick: "doSomething" }
            //
            // becomes
            //
            //     binding: { onClick: { command: "doSomething" } }
            //
            var expectedBindingSpec = new JObject(){ {"onClick", new JObject(){ {"command", new JValue("doSomething")} } } };

            var bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, defaultBindingAttribute: "onClick", commandAttributes: new string[]{"onClick"});
            Assert.IsTrue(bindingSpec.DeepEquals(expectedBindingSpec));
        }

        [TestMethod]
        public void TestBindingHelperPromoteExplicitCommand()
        {
            // Also (default binding atttribute is 'onClick', which is also in command attributes list):
            //
            //     binding: { command: "doSomething" value: "theValue" }
            //
            var controlSpec = new JObject(){ {"binding", new JObject(){ {"command", new JValue("doSomething")}, {"value", new JValue("theValue")} } } };

            // becomes
            //
            //     binding: { onClick: { command: "doSomething", value: "theValue" } }
            //
            var expectedBindingSpec = new JObject(){ {"onClick", new JObject(){ {"command", new JValue("doSomething")}, {"value", new JValue("theValue")} } } };

            var bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, defaultBindingAttribute: "onClick", commandAttributes: new string[]{"onClick"});
            Assert.IsTrue(bindingSpec.DeepEquals(expectedBindingSpec));
        }

        [TestMethod]
        public void TestBindingHelperPromoteMultipleCommands()
        {
            // For multiple commands with implicit values...
            //
            //     binding: { onClick: "doClickCommand", onSelect: "doSelectCommand" }
            //
            var controlSpec = new JObject(){ {"binding", new JObject(){ {"onClick", new JValue("doClickCommand")}, {"onSelect", new JValue("doSelectCommand")} } } };
        
            // becomes
            //
            //     binding: { onClick: { command: "doClickCommand" }, onSelect: { command: "doSelectCommand" } }
            //
            var expectedBindingSpec = new JObject(){ {"onClick", new JObject(){ {"command", new JValue("doClickCommand")} } }, {"onSelect", new JObject(){ {"command", new JValue("doSelectCommand")} } } };
        
            var bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, defaultBindingAttribute: "onClick", commandAttributes: new string[]{"onClick", "onSelect"});
            Assert.IsTrue(bindingSpec.DeepEquals(expectedBindingSpec));
        }

        [TestMethod]
        public void TestPropertyValue()
        {
            var viewModel = new JObject()
            {
                {"serial", new JValue(0)},
                {"title", new JValue("Colors")},
                {"colors", new JArray()
                    {
                        new JObject(){ {"name", new JValue("Red")}, {"color", new JValue("red")}, {"value", new JValue("0xff0000")} },
                        new JObject(){ {"name", new JValue("Green")}, {"color", new JValue("green")}, {"value", new JValue("0x00ff00")} },
                        new JObject(){ {"name", new JValue("Blue")}, {"color", new JValue("blue")}, {"value", new JValue("0x0000ff")} }
                    }
                }
            };
        
            var bindingCtx = new BindingContext(viewModel);

            var propVal = new PropertyValue("The {title} are {colors[0].name}, {colors[1].name}, and {colors[2].name}", bindingContext: bindingCtx);
        
            Assert.AreEqual("The Colors are Red, Green, and Blue", (string)propVal.Expand());
        }
        
        [TestMethod]
        public void TestPropertyValueModelUpdate()
        {
            var viewModel = new JObject()
            {
                {"serial", new JValue(0)},
                {"title", new JValue("Colors")},
                {"colors", new JArray()
                    {
                        new JObject(){ {"name", new JValue("Red")}, {"color", new JValue("red")}, {"value", new JValue("0xff0000")} },
                        new JObject(){ {"name", new JValue("Green")}, {"color", new JValue("green")}, {"value", new JValue("0x00ff00")} },
                        new JObject(){ {"name", new JValue("Blue")}, {"color", new JValue("blue")}, {"value", new JValue("0x0000ff")} }
                    }
                }
            };
        
            var bindingCtx = new BindingContext(viewModel);
        
            var propVal = new PropertyValue("The {title} are {colors[0].name}, {colors[1].name}, and {colors[2].name}", bindingContext: bindingCtx);
        
            Assert.AreEqual("The Colors are Red, Green, and Blue", (string)propVal.Expand());
        
            ((JArray)viewModel["colors"])[1] = new JObject(){ {"name", new JValue("Greenish")}, {"color", new JValue("green")}, {"value", new JValue("0x00ff00")} };
            foreach (var bindingContext in propVal.BindingContexts)
            {
                bindingContext.Rebind();
            }

            Assert.AreEqual("The Colors are Red, Greenish, and Blue", (string)propVal.Expand());
        }

        [TestMethod]
        public void TestPropertyValueModelUpdateOneTimeToken()
        {
            var viewModel = new JObject()
            {
                {"serial", new JValue(0)},
                {"title", new JValue("Colors")},
                {"colors", new JArray()
                    {
                        new JObject(){ {"name", new JValue("Red")}, {"color", new JValue("red")}, {"value", new JValue("0xff0000")} },
                        new JObject(){ {"name", new JValue("Green")}, {"color", new JValue("green")}, {"value", new JValue("0x00ff00")} },
                        new JObject(){ {"name", new JValue("Blue")}, {"color", new JValue("blue")}, {"value", new JValue("0x0000ff")} }
                    }
                }
            };
        
            var bindingCtx = new BindingContext(viewModel);
        
            var propVal = new PropertyValue("The {title} are {colors[0].name}, {colors[1].name}, and {^colors[2].name}", bindingContext: bindingCtx);

            Assert.AreEqual("The Colors are Red, Green, and Blue", (string)propVal.Expand());
        
            ((JArray)viewModel["colors"])[1] = new JObject(){ {"name", new JValue("Greenish")}, {"color", new JValue("green")}, {"value", new JValue("0x00ff00")} };
            ((JArray)viewModel["colors"])[2] = new JObject(){ {"name", new JValue("Blueish")}, {"color", new JValue("blue")}, {"value", new JValue("0x0000ff")} };
            foreach (var bindingContext in propVal.BindingContexts)
            {
                bindingContext.Rebind();
            }

            Assert.AreEqual("The Colors are Red, Greenish, and Blue", (string)propVal.Expand());
        }

        [TestMethod]
        public void TestPropertyValueIntToken()
        {
            var viewModel = new JObject()
            {
                {"serial", new JValue(420)}
            };
        
            var bindingCtx = new BindingContext(viewModel);
        
            var propVal = new PropertyValue("{serial}", bindingContext: bindingCtx);
            var expandedPropValToken = propVal.Expand();
        
            Assert.AreEqual(JTokenType.Integer, expandedPropValToken.Type);
            Assert.AreEqual(420, (int)expandedPropValToken);
        }

        [TestMethod]
        public void TestPropertyValueFloatToken()
        {
            var viewModel = new JObject()
            {
                {"serial", new JValue(13.69)}
            };
        
            var bindingCtx = new BindingContext(viewModel);
        
            var propVal = new PropertyValue("{serial}", bindingContext: bindingCtx);
            var expandedPropValToken = propVal.Expand();
        
            Assert.AreEqual(JTokenType.Float, expandedPropValToken.Type);
            Assert.AreEqual(13.69, (double)expandedPropValToken);
        }

        [TestMethod]
        public void TestPropertyValueBoolToken()
        {
            var viewModel = new JObject()
            {
                {"serial", new JValue(true)},
            };
        
            var bindingCtx = new BindingContext(viewModel);
        
            var propVal = new PropertyValue("{serial}", bindingContext: bindingCtx);
            var expandedPropValToken = propVal.Expand();
        
            Assert.AreEqual(JTokenType.Boolean, expandedPropValToken.Type);
            Assert.AreEqual(true, (bool)expandedPropValToken);
        }

        [TestMethod]
        public void TestPropertyValueBoolTokenNegated()
        {
            var viewModel = new JObject()
            {
                {"serial", new JValue(true)}
            };
        
            var bindingCtx = new BindingContext(viewModel);
        
            var propVal = new PropertyValue("{!serial}", bindingContext: bindingCtx);
            var expandedPropValToken = propVal.Expand();
        
            Assert.AreEqual(JTokenType.Boolean, expandedPropValToken.Type);
            Assert.AreEqual(false, (bool)expandedPropValToken);
        }

        [TestMethod]
        public void TestPropertyValueStringToken()
        {
            var viewModel = new JObject()
            {
                {"serial", new JValue("foo")}
            };
        
            var bindingCtx = new BindingContext(viewModel);
        
            var propVal = new PropertyValue("{serial}", bindingContext: bindingCtx);
            var expandedPropValToken = propVal.Expand();
        
            Assert.AreEqual(JTokenType.String, expandedPropValToken.Type);
            Assert.AreEqual("foo", (string)expandedPropValToken);
        }

        [TestMethod]
        public void TestPropertyValueStringTokenNegated()
        {
            var viewModel = new JObject()
            {
                {"serial", new JValue("foo")}
            };
        
            var bindingCtx = new BindingContext(viewModel);
        
            var propVal = new PropertyValue("{!serial}", bindingContext: bindingCtx);
            var expandedPropValToken = propVal.Expand();
        
            // When we negate a string, the type is coerced (converted) to bool, then inverted...
            Assert.AreEqual(JTokenType.Boolean, expandedPropValToken.Type);
            Assert.AreEqual(false, (bool)expandedPropValToken);
        }

        [TestMethod]
        public void TestNumericFormattingIntNoSpec()
        {
            var viewModel = new JObject()
            {
                {"serial", new JValue(69)}
            };
        
            var bindingCtx = new BindingContext(viewModel);
        
            var propVal = new PropertyValue("The number is: {serial}", bindingContext: bindingCtx);
        
            Assert.AreEqual("The number is: 69", (string)propVal.Expand());
        }

        [TestMethod]
        public void TestNumericFormattingFloatNoSpec()
        {
            var viewModel = new JObject()
            {
                {"serial", new JValue(13.69)}
            };
        
            var bindingCtx = new BindingContext(viewModel);
        
            var propVal = new PropertyValue("The number is: {serial}", bindingContext: bindingCtx);
        
            Assert.AreEqual("The number is: 13.69", (string)propVal.Expand());
        }

        [TestMethod]
        public void TestNumericFormattingAsPercentage()
        {
            var viewModel = new JObject()
            {
                {"intVal", new JValue(13)},
                {"doubleVal", new JValue(0.69139876)},
                {"strVal", new JValue("threeve")},
            };
        
            var bindingCtx = new BindingContext(viewModel);
        
            var propVal = new PropertyValue("The int percentage is {intVal:P}, the double is: {doubleVal:P2}, and the str is {strVal:P2}", bindingContext: bindingCtx);
        
            // !!! On .NET we get a space between the value and the percent sign.  The iOS percent formatter does not do this.  Both formatters are
            //     using a locale-aware formatter and presumably know what they're doing, so I'm not inclined to try to "fix" one of them to make them
            //     match.
            //
            Assert.AreEqual("The int percentage is 1,300.00 %, the double is: 69.14 %, and the str is threeve", (string)propVal.Expand());
        }

        [TestMethod]
        public void TestNumericFormattingAsDecimal()
        {
            var viewModel = new JObject()
            {
                {"intVal", new JValue(-13420)},
                {"doubleVal", new JValue(69.139876)},
                {"strVal", new JValue("threeve")}
            };
        
            var bindingCtx = new BindingContext(viewModel);
        
            var propVal = new PropertyValue("The int val is {intVal:D}, the double val is: {doubleVal:D4}, and the str val is {strVal:D2}", bindingContext: bindingCtx);
        
            Assert.AreEqual("The int val is -13420, the double val is: 0069, and the str val is threeve", (string)propVal.Expand());
        }

        [TestMethod]
        public void TestNumericFormattingAsNumber()
        {
            var viewModel = new JObject()
            {
                {"intVal", new JValue(-13420)},
                {"doubleVal", new JValue(69.139876)},
                {"strVal", new JValue("threeve")}
            };
        
            var bindingCtx = new BindingContext(viewModel);
        
            var propVal = new PropertyValue("The int val is {intVal:N}, the double val is: {doubleVal:N4}, and the str val is {strVal:N2}", bindingContext: bindingCtx);
        
            Assert.AreEqual("The int val is -13,420.00, the double val is: 69.1399, and the str val is threeve", (string)propVal.Expand());
        }

        [TestMethod]
        public void TestNumericFormattingAsHex()
        {
            var viewModel = new JObject()
            {
                {"intVal", new JValue(254)},
                {"doubleVal", new JValue(254.139876)},
                {"strVal", new JValue("threeve")}
            };
        
            var bindingCtx = new BindingContext(viewModel);
        
            var propVal = new PropertyValue("The int val is {intVal:x}, the double val is: {doubleVal:X4}, and the str val is {strVal:X2}", bindingContext: bindingCtx);
        
            Assert.AreEqual("The int val is fe, the double val is: 00FE, and the str val is threeve", (string)propVal.Expand());
        }

        [TestMethod]
        public void TestNumericFormattingAsFixedPoint()
        {
            var viewModel = new JObject()
            {
                {"intVal", new JValue(-13420)},
                {"doubleVal", new JValue(254.139876)},
                {"strVal", new JValue("threeve")}
            };
        
            var bindingCtx = new BindingContext(viewModel);
        
            var propVal = new PropertyValue("The int val is {intVal:F2}, the double val is: {doubleVal:F4}, and the str val is {strVal:F2}", bindingContext: bindingCtx);
        
            Assert.AreEqual("The int val is -13420.00, the double val is: 254.1399, and the str val is threeve", (string)propVal.Expand());
        }

        [TestMethod]
        public void TestNumericFormattingAsExponential()
        {
            var viewModel = new JObject()
            {
                {"intVal", new JValue(-69)},
                {"doubleVal", new JValue(69.123456789)},
                {"strVal", new JValue("threeve")}
            };
        
            var bindingCtx = new BindingContext(viewModel);
        
            var propVal = new PropertyValue("The int val is {intVal:E2}, the double val is: {doubleVal:e4}, and the str val is {strVal:e2}", bindingContext: bindingCtx);
        
            // !!! .NET uses the "e+001" notation, whereas iOS uses "e1" notation.  Since they both use locale-aware built-in formatters for this,
            //     I'm not inclined to try to "fix" one of them to make them match.
            //
            Assert.AreEqual("The int val is -6.90E+001, the double val is: 6.9123e+001, and the str val is threeve", (string)propVal.Expand());
        }

        [TestMethod]
        public void TestNumericFormattingParsesStringAsNumber()
        {
            var viewModel = new JObject()
            {
                {"strVal", new JValue("13")},
            };
    
            var bindingCtx = new BindingContext(viewModel);
        
            var propVal = new PropertyValue("The numeric value is {strVal:F2}", bindingContext: bindingCtx);
        
            Assert.AreEqual("The numeric value is 13.00", (string)propVal.Expand());
        }
    }
}
