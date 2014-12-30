using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynchroCore
{
    public static class TokenConverter
    {
        public static String ToString(JToken token, String defaultValue = "")
        {
            string result = defaultValue;

            if (token != null)
            {
                switch (token.Type)
                {
                    case JTokenType.Array:
                        JArray array = token as JArray;
                        result = array.Count.ToString();
                        break;
                    case JTokenType.String:
                        result = (string)token;
                        break;
                    case JTokenType.Integer:
                        result = ((int)token).ToString();
                        break;
                    case JTokenType.Float:
                        result = ((double)token).ToString();
                        break;
                    case JTokenType.Boolean:
                        result = ((bool)token) ? "true" : "false";
                        break;
                    default:
                        try 
                        {
                            result = (string)token;
                        }
                        catch (Exception)
                        {
                            // Can't convert to string.  No big deal.
                        }
                        break;
                }
            }

            return result;
        }

        public static Boolean ToBoolean(JToken token, Boolean defaultValue = false)
        {
            Boolean result = defaultValue;

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
                        result = (double)token != 0;
                        break;
                    case JTokenType.Integer:
                        result = (int)token != 0;
                        break;
                    case JTokenType.Array:
                        JArray array = token as JArray;
                        result = array.Count > 0;
                        break;
                    case JTokenType.Object:
                        result = true;
                        break;
                }
            }

            return result;
        }

        public static double? ToDouble(JToken value, double? defaultValue = null)
        {
            double? result = defaultValue;

            if (value != null)
            {
                if (value is JValue)
                {
                    JValue jValue = value as JValue;
                    if (jValue.Type == JTokenType.String)
                    {
                        try
                        {
                            result = Convert.ToDouble((string)jValue);
                        }
                        catch (FormatException)
                        {
                            // Not formatted as a number, no biggie...
                        }
                    }
                    else
                    {
                        result = (double)jValue;
                    }
                }
                else if (value is JArray)
                {
                    return ((JArray)value).Count;
                }
            }

            return result;
        }
    }
}
