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
                    default:
                        result = (string)token; //.ToString();
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

        public static Double ToDouble(JToken value, double defaultValue = 0)
        {
            Double result = defaultValue;

            if (value != null)
            {
                if (value is JValue)
                {
                    JValue jValue = value as JValue;
                    if (jValue.Type == JTokenType.String)
                    {
                        result = Convert.ToDouble((string)jValue);
                    }
                    else
                    {
                        result = (double)jValue;
                    }
                }
            }

            return result;
        }
    }
}
