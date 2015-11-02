using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SynchroCore;

namespace SynchroCoreTest
{
    [TestClass]
    public class TokenConverterTest
    {
        [TestMethod]
        public void TestToString()
        {
            var objVal = new JObject(){ {"foo", new JValue("bar")}, {"baz", new JValue("fraz")} };
            var arrayVal = new JArray(){ new JValue("foo"), new JValue("bar") };
            var stringVal = new JValue("foo");
            var intVal = new JValue(13);
            var floatVal = new JValue(13.69);
            var boolVal = new JValue(true);
        
            Assert.AreEqual("", TokenConverter.ToString(objVal));
            Assert.AreEqual("2", TokenConverter.ToString(arrayVal));
            Assert.AreEqual("foo", TokenConverter.ToString(stringVal));
            Assert.AreEqual("13", TokenConverter.ToString(intVal));
            Assert.AreEqual("13.69", TokenConverter.ToString(floatVal));
            Assert.AreEqual("true", TokenConverter.ToString(boolVal));
        }

        [TestMethod]
        public void TestToBoolean()
        {
            var objVal = new JObject(){ {"foo", new JValue("bar")}, {"baz", new JValue("fraz")} };
            var objValEmpty = new JObject();
            var arrayVal = new JArray(){ new JValue("foo"), new JValue("bar") };
            var arrayValEmpty = new JArray();
            var stringVal = new JValue("foo");
            var stringValEmpty = new JValue("");
            var intVal = new JValue(13);
            var intValZero = new JValue(0);
            var floatVal = new JValue(13.69);
            var floatValZero = new JValue(0.0);
            var boolValTrue = new JValue(true);
            var boolValFalse = new JValue(false);
        
            Assert.AreEqual(true, TokenConverter.ToBoolean(objVal));
            Assert.AreEqual(true, TokenConverter.ToBoolean(objValEmpty));
            Assert.AreEqual(true, TokenConverter.ToBoolean(arrayVal));
            Assert.AreEqual(false, TokenConverter.ToBoolean(arrayValEmpty));
            Assert.AreEqual(true, TokenConverter.ToBoolean(stringVal));
            Assert.AreEqual(false, TokenConverter.ToBoolean(stringValEmpty));
            Assert.AreEqual(true, TokenConverter.ToBoolean(intVal));
            Assert.AreEqual(false, TokenConverter.ToBoolean(intValZero));
            Assert.AreEqual(true, TokenConverter.ToBoolean(floatVal));
            Assert.AreEqual(false, TokenConverter.ToBoolean(floatValZero));
            Assert.AreEqual(true, TokenConverter.ToBoolean(boolValTrue));
            Assert.AreEqual(false, TokenConverter.ToBoolean(boolValFalse));
        }

        [TestMethod]
        public void TestToDouble()
        {
            var arrayVal = new JArray(){ new JValue("foo"), new JValue("bar") };
            var arrayValEmpty = new JArray();
            var stringVal = new JValue("12.34");
            var stringNotNum = new JValue("threeve");
            var intVal = new JValue(13);
            var floatVal = new JValue(13.69);
    
            Assert.AreEqual(2, TokenConverter.ToDouble(arrayVal));
            Assert.AreEqual(0, TokenConverter.ToDouble(arrayValEmpty));
            Assert.AreEqual(12.34, TokenConverter.ToDouble(stringVal));
            Assert.AreEqual(null, TokenConverter.ToDouble(stringNotNum));
            Assert.AreEqual(13, TokenConverter.ToDouble(intVal));
            Assert.AreEqual(13.69, TokenConverter.ToDouble(floatVal));
        }
    }
}
