using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
﻿
namespace SynchroCore
{
    public class JsonWriter
    {
        private static Dictionary<char, string> charSubstitutions = new Dictionary<char, string>() {
			{ '\\', @"\\" },
			{ '/', @"\/" },
			{ '"', @"\""" },
			{ '\b', @"\b" },
			{ '\f', @"\f" },
			{ '\n', @"\n" },
			{ '\r', @"\r" },
			{ '\t', @"\t" },
		};

        private static void WriteString(TextWriter writer, string _string)
        {
            writer.Write('\"');
            foreach (var _char in _string)
            {
                if (charSubstitutions.ContainsKey(_char))
                {
                    writer.Write(charSubstitutions[_char]);
                }
                else if ((_char < ' ') || (_char > '\u007E'))
                {
                    writer.Write(string.Format("\\u{0:X4}", (int)_char));
                }
                else
                {
                    writer.Write(_char);
                }
            }
            writer.Write('\"');
        }

        private static void WriteNumber(TextWriter writer, int i)
        {
            writer.Write(i);
        }

        private static void WriteNumber(TextWriter writer, double d)
        {
            writer.Write(d.ToString(CultureInfo.InvariantCulture.NumberFormat));
        }

        private static void WriteArray(TextWriter writer, JArray array)
        {
            bool firstElement = true;

            writer.Write('[');
            foreach (var value in array)
            {
                if (!firstElement)
                {
                    writer.Write(',');
                }
                else
                {
                    firstElement = false;
                }

                WriteValue(writer, value);
            }
            writer.Write(']');
        }

        private static void WriteBoolean(TextWriter writer, bool b)
        {
            writer.Write(b ? "true" : "false");
        }

        static void WriteNull(TextWriter writer)
        {
            writer.Write("null");
        }

        private static Dictionary<JTokenType, Action<TextWriter, JToken>> writerActions = new Dictionary<JTokenType, Action<TextWriter, JToken>>() {
			{ JTokenType.Object, (writer, value) => { WriteObject(writer, (JObject) value); } },
			{ JTokenType.Array, (writer, value) => { WriteArray(writer, (JArray)value); } },
			{ JTokenType.String, (writer, value) => { WriteString(writer, (string)value); } },
			{ JTokenType.Integer, (writer, value) => { WriteNumber(writer, (int)value); } },
			{ JTokenType.Float, (writer, value) => { WriteNumber(writer, (double)value); } },
			{ JTokenType.Boolean, (writer, value) => { WriteBoolean(writer, (bool)value); } },
		};

        public static void WriteValue(TextWriter writer, JToken value)
        {
            if ((value == null) || (value.Type == JTokenType.Null))
            {
                WriteNull(writer);
            }
            else if (writerActions.ContainsKey(value.Type))
            {
                writerActions[value.Type](writer, value);
            }
            else
            {
                throw new IOException(string.Format("Unknown object type {0}", value.Type));
            }
        }

        private static void WriteObject(TextWriter writer, JObject _object)
        {
            bool firstKey = true;

            writer.Write('{');
            foreach (var key in _object.Keys)
            {
                var value = _object[key];

                if (!firstKey)
                {
                    writer.Write(',');
                }
                else
                {
                    firstKey = false;
                }

                WriteString(writer, key);

                writer.Write(':');

                WriteValue(writer, value);
            }
            writer.Write('}');
        }
    }
}