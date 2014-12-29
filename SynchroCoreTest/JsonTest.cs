using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SynchroCore;

namespace SynchroCoreTest
{
    [TestClass]
    public class JsonTest
    {
        [TestMethod]
        public void TestInteger()
        {
            var stuff = new JObject();
            stuff["foo"] = new JValue(7);
            Assert.AreEqual(7, (int)stuff["foo"]);
        }

        [TestMethod]
        public void TestString()
        {
            var stuff = new JObject();
            stuff["bar"] = new JValue("kitty");
            Assert.AreEqual("kitty", (string)stuff["bar"]);
        }

        [TestMethod]
        public void TestArray()
        {
            var stuff = new JObject();
            stuff["baz"] = new JArray(){ new JValue(8), new JValue("dog") };
            Assert.AreEqual(8, (int)((JArray)stuff["baz"])[0]);
            Assert.AreEqual("dog", (string)((JArray)stuff["baz"])[1]);
        }

        [TestMethod]
        public void TestDeepClone()
        {
            var stuff = new JObject()
            {
                { "a", new JObject()
                    {
                        { "b", new JObject() 
                            { 
                                { "c", new JValue("d") }
                            }
                        }
                    }
                },
                { "e", new JArray()
                    {
                        new JObject()
                        {
                            { "f", new JValue("g") }
                        },
                        new JValue("h")
                    }
                }
            };

            var duplicateStuff = new JObject()
            {
                { "a", new JObject()
                    {
                        { "b", new JObject() 
                            { 
                                { "c", new JValue("d") }
                            }
                        }
                    }
                },
                { "e", new JArray()
                    {
                        new JObject()
                        {
                            { "f", new JValue("g") }
                        },
                        new JValue("h")
                    }
                }
            };

            var cloneStuff = stuff.DeepClone();

            Assert.IsTrue(stuff.DeepEquals(duplicateStuff));
            Assert.IsTrue(stuff.DeepEquals(cloneStuff));

            stuff["foo"] = new JValue("bar");

            Assert.IsFalse(stuff.DeepEquals(duplicateStuff));
            Assert.IsFalse(stuff.DeepEquals(cloneStuff));

            duplicateStuff["foo"] = new JValue("bar");

            Assert.IsTrue(stuff.DeepEquals(duplicateStuff));
            Assert.IsFalse(stuff.DeepEquals(cloneStuff));
        }

        [TestMethod]
        public void TestPath()
        {
            var stuff = new JObject()
            {
                { "a", new JObject()
                    {
                        { "b", new JObject() 
                            { 
                                { "c", new JValue("d") }
                            }
                        }
                    }
                },
                { "e", new JArray()
                    {
                        new JObject()
                        {
                            { "f", new JValue("g") }
                        },
                        new JValue("h")
                    }
                }
            };

            Assert.IsTrue(ReferenceEquals(((JObject)((JArray)stuff["e"])[0])["f"], stuff.SelectToken("e[0].f")));
        }

        [TestMethod]
        public void TestUpdate()
        {
            var stuff = new JObject();

            stuff["a"] = new JValue(null);
            stuff["b"] = new JValue(null);

            var vmItemValue = stuff.SelectToken("a");
            var rebindRequired = JToken.UpdateTokenValue(ref vmItemValue, new JObject() { { "baz", new JValue("Fraz") } });

            var expected = new JObject()
            {
                { "a", new JObject(){ { "baz", new JValue("Fraz") } } },
                { "b", new JValue(null) }
            };

            Assert.IsTrue(rebindRequired);
            Assert.IsTrue(expected.DeepEquals(stuff));
        }

        [TestMethod]
        public void TestArrayRemoveByObjectNotValue()
        {
            var red = new JValue("Red");
            var green1 = new JValue("Green");
            var green2 = new JValue("Green");

            var arr = new JArray() { red, green1, green2 };

            arr.Remove(green2);

            Assert.AreEqual(2, arr.Count);
            Assert.IsTrue(ReferenceEquals(red, arr[0]));
            Assert.IsTrue(ReferenceEquals(green1, arr[1]));
        }
    }
}
