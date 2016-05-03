using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SynchroCore;
using System.Globalization;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace SynchroCoreTest
{
    [TestClass]
    public class JsonParserTest
    {
        void ValidateRoundTrip(string jsonInput, JToken expected)
        {
            var token = JToken.Parse(jsonInput);
            var jsonOutput = token.ToJson();
            Assert.AreEqual(jsonInput, jsonOutput);

            Assert.IsTrue(token.DeepEquals(expected));
        }

        [TestMethod]
        public void TestParseSimple()
        {
            var jsonStr = "{\"foo\":true}";

            var token = (JObject)JToken.Parse(jsonStr);

            Assert.IsTrue((bool)token["foo"]);

            var outStr = token.ToJson();

            Assert.AreEqual(jsonStr, outStr);
        }

        [TestMethod]
        public void TestParseString()
        {
            ValidateRoundTrip("\"abc\"", new JValue("abc"));
        }

        [TestMethod]
		public void TestParseStringEscapes()
		{
			ValidateRoundTrip(@"""\""\\\/\b\f\n\r\t\u20AC""", new JValue("\"\\/\b\f\n\r\t\u20AC"));
		}

		[TestMethod]
		public void TestParseInteger()
		{
			ValidateRoundTrip("0", new JValue(0));
		}

		[TestMethod]
		public void TestParseArray()
		{
			ValidateRoundTrip("[]", new JArray());
			ValidateRoundTrip("[0]", new JArray(){ new JValue(0) });
			ValidateRoundTrip("[\"abc\"]", new JArray(){ new JValue("abc") });
			ValidateRoundTrip("[0,\"abc\"]", new JArray(){ new JValue(0), new JValue("abc") });
            ValidateRoundTrip("[0,\"abc\",[1,\"def\"]]", new JArray() { new JValue(0), new JValue("abc"), new JArray(){ new JValue(1), new JValue("def") } });
		}

		[TestMethod]
		public void TestParseObject()
		{
			ValidateRoundTrip("{}", new JObject());
			ValidateRoundTrip(
				"{\"foo\":0,\"bar\":\"kitty\",\"baz\":[8,\"dog\"]}",
				new JObject()
                {
					{ "foo", new JValue(0) },
					{ "bar", new JValue("kitty") },
					{ "baz", new JArray(){ new JValue(8), new JValue("dog") } }
				}
			);
		}

		[TestMethod]
		public void TestParseObjectWithWhitespace()
		{
			Assert.IsTrue(JToken.DeepEquals(new JObject(), JToken.Parse("{}")));
			Assert.IsTrue(JToken.DeepEquals(
				new JObject()
				{
					{ "foo", new JValue(0) },
					{ "bar", new JValue("kitty") },
					{ "baz", new JArray(){ new JValue(8), new JValue("dog") } }
				},
				JToken.Parse("  {  \"foo\"  :  0  ,  \"bar\"  :  \"kitty\"  ,  \"baz\"  :  [  8  ,  \"dog\"  ]  }  ")));
		}

		[TestMethod]
		public void TestParseBoolean()
		{
			ValidateRoundTrip("true", new JValue(true));
			ValidateRoundTrip("false", new JValue(false));
		}

		[TestMethod]
		public void TestParseNull()
		{
			ValidateRoundTrip("null", new JValue(null));
		}

		[TestMethod]
		// deprecated [ExpectedException(typeof(IOException))]
		public void TestUnterminatedString()
		{
            Assert.ThrowsException<IOException>(() => JToken.Parse("\"abc"));
		}

		[TestMethod]
		public void TestComments()
		{
			var jsonWithComments = @"
// This is a comment
{
	// The foo element is my favorite
	""foo""  :  0,
	""bar""  :  ""kitty"",
	// The baz element, he's OK also
	""baz""  :  [  8  ,  ""dog""  ]
}
";
			Assert.IsTrue(JToken.DeepEquals(
				new JObject()
				{
					{ "foo", new JValue(0) },
					{ "bar", new JValue("kitty") },
					{ "baz", new JArray(){ new JValue(8), new JValue("dog") } }
				},
				JToken.Parse(jsonWithComments)));
		}

		[TestMethod]
		public void TestMultilineComments()
		{
			var jsonWithComments = @"
// This is a comment
{
	// The foo element is my favorite. But comment him out for now.
/*
	""foo""  :  0,
*/
	""bar""  :  ""kitty"",
	// The baz element, he's OK also
	""baz""  :  [  8  ,  ""dog""  ]
}
";
			Assert.IsTrue(JToken.DeepEquals(
				new JObject()
				{
					{ "bar", new JValue("kitty") },
					{ "baz", new JArray(){ new JValue(8), new JValue("dog") } }
				},
				JToken.Parse(jsonWithComments)));
		}

		[TestMethod]
		public void TestParseDouble()
		{
			ValidateRoundTrip("0.001", new JValue(.001));
			ValidateRoundTrip("6.02E+23", new JValue(6.02E+23));
		}

        [TestMethod]
        public void TestParseBigInt()
        {
            ValidateRoundTrip("1461445025212", new JValue(1461445025212));
        }

        [TestMethod]
		public void TestParseDoubleCrazyLocale()
		{
            // !!! No threads in Win app...
            //
			var crazyCulture = new CultureInfo("en-US");
			//var oldCulture = Thread.CurrentThread.CurrentCulture;
            var oldCulture = CultureInfo.DefaultThreadCurrentCulture;

			crazyCulture.NumberFormat.NumberDecimalSeparator = "Z";

			// Thread.CurrentThread.CurrentCulture = crazyCulture;
            CultureInfo.DefaultThreadCurrentCulture = crazyCulture;

			try
			{
				ValidateRoundTrip("0.001", new JValue(.001));
				ValidateRoundTrip("6.02E+23", new JValue(6.02E+23));
			}
			finally
			{
				// Thread.CurrentThread.CurrentCulture = oldCulture;
                CultureInfo.DefaultThreadCurrentCulture = oldCulture;
			}
		}
    }
}
