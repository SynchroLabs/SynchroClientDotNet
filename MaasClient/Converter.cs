using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml.Media;

namespace MaasClient
{
    // When setting a value into a control using a Binding.SetControl delegate for a control that takes a simple value, the value 
    // provided may be a string, double, boolean, or a JToken/JValue that is convertible into one of these basic types.  The functions
    // in this class should be used to convert the supplied object value into the type required by the control.
    //
    class Converter
    {
        public static Double ToDouble(object value)
        {
            if (value is JValue)
            {
                var jvalue = value as JValue;
                return (double)jvalue;
            }
            return Convert.ToDouble(value);
        }

        public static String ToString(object value)
        {
            if (value is JToken)
            {
                return (string)(JToken)value;
            }
            else
            {
                return (string)value;
            }
        }

        public static Boolean ToBoolean(object value)
        {
            Boolean result = false;

            if (value is JToken)
            {
                var token = value as JToken;
                if (token != null)
                {
                    switch (token.Type)
                    {
                        case JTokenType.Boolean:
                            result = (Boolean)token;
                            break;
                        case JTokenType.String:
                            String str = (String)token;
                            result = str.Length > 0;
                            break;
                        case JTokenType.Float:
                            result = (float)token != 0;
                            break;
                        case JTokenType.Integer:
                            result = (int)token != 0;
                            break;
                        case JTokenType.Array:
                            JArray array = token as JArray;
                            result = array.Count > 0;
                            break;
                    }
                }
            }
            else
            {
                if (value is String)
                {
                    result = ((string)value).Length > 0;
                }
                else
                {
                    result = Convert.ToBoolean(value);
                }
            }
            return result;
        }

        public static SolidColorBrush ToBrush(object value)
        {
            String color = Converter.ToString(value);
            if (color.StartsWith("#"))
            {
                color = color.Replace("#", "");
                if (color.Length == 6)
                {
                    return new SolidColorBrush(ColorHelper.FromArgb(255,
                        byte.Parse(color.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                        byte.Parse(color.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                        byte.Parse(color.Substring(4, 2), System.Globalization.NumberStyles.HexNumber)));
                }
            }
            else
            {
                var property = typeof(Colors).GetRuntimeProperty(color);
                if (property != null)
                {
                    return new SolidColorBrush((Color)property.GetValue(null));
                }
            }

            return null;
        }

        public static FontWeight ToFontWeight(object value)
        {
            String weight = Converter.ToString(value);

            var property = typeof(FontWeights).GetRuntimeProperty(weight);
            if (property != null)
            {
                return (FontWeight)property.GetValue(null);
            }
            return FontWeights.Normal;
        }
    }
}
