using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaaasCore
{
    public class JsonParser
    {
        private static void SkipWhitespace(TextReader reader)
        {
            while ((Char.IsWhiteSpace((char)reader.Peek())) || (reader.Peek() == '/'))
            {
                while (Char.IsWhiteSpace((char)reader.Peek()))
                {
                    reader.Read();
                }

                if (reader.Peek() == '/')
                {
                    reader.Read(); // Eat the initial /
                    var nextChar = reader.Read();

                    if (nextChar == '/')
                    {
                        while ((reader.Peek() != '\r') && (reader.Peek() != '\n') && (reader.Peek() != -1))
                        {
                            reader.Read();
                        }
                    }
                    else /* nextChar assumed to be a * */
                    {
                        while (true)
                        {
                            nextChar = reader.Read();

                            if (nextChar == -1)
                            {
                                break;
                            }
                            else if (nextChar == '*')
                            {
                                // If the next character is a '/' eat it, otherwise keep going

                                if (reader.Peek() == '/')
                                {
                                    reader.Read();
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private static string ParseString(TextReader reader)
        {
            int thisChar;
            var returnString = new StringBuilder();

            SkipWhitespace(reader);

            // Skip the opening quotes

            reader.Read();

            // Read until closing quotes

            while ((thisChar = reader.Read()) != '"')
            {
                if (thisChar == -1)
                {
                    throw new IOException("Unexpected end of stream");
                }

                if (thisChar == '\\')
                {
                    thisChar = reader.Read();

                    switch (thisChar)
                    {
                        case 'b':
                            thisChar = '\b';
                            break;
                        case 'f':
                            thisChar = '\f';
                            break;
                        case 'r':
                            thisChar = '\r';
                            break;
                        case 'n':
                            thisChar = '\n';
                            break;
                        case 't':
                            thisChar = '\t';
                            break;
                        case 'u':
                            // Parse four hex digits
                            var hexBuilder = new StringBuilder(4);
                            for (int counter = 0; counter < 4; ++counter)
                            {
                                hexBuilder.Append((char)reader.Read());
                            }
                            thisChar = Convert.ToInt32(hexBuilder.ToString(), 16);
                            break;
                        case '\\':
                        case '"':
                        case '/':
                        default:
                            break;
                    }
                }

                returnString.Append((char)thisChar);
            }

            return returnString.ToString();
        }

        private static object ParseNumber(TextReader reader)
        {
            var numberBuilder = new StringBuilder();

            SkipWhitespace(reader);

            while ("0123456789Ee.-+".IndexOf((char)reader.Peek()) >= 0)
            {
                numberBuilder.Append((char)reader.Read());
            }

            var numberData = numberBuilder.ToString();

            if (numberData.IndexOfAny("eE.".ToCharArray()) >= 0)
            {
                return double.Parse(numberData, CultureInfo.InvariantCulture.NumberFormat);
            }
            else
            {
                return int.Parse(numberData);
            }
        }

        private static JArray ParseArray(TextReader reader)
        {
            var finalArray = new JArray();

            SkipWhitespace(reader);

            // Skip the opening bracket

            reader.Read();

            SkipWhitespace(reader);

            // Read until closing bracket

            while (reader.Peek() != ']')
            {
                // Read a value

                finalArray.Add(ParseValue(reader));

                SkipWhitespace(reader);

                // Skip the comma if any

                if (reader.Peek() == ',')
                {
                    reader.Read();
                }

                SkipWhitespace(reader);
            }

            // Skip the closing bracket

            reader.Read();

            return finalArray;
        }

        private static JObject ParseObject(TextReader reader)
        {
            var finalObject = new JObject();

            SkipWhitespace(reader);

            // Skip the opening brace

            reader.Read();

            SkipWhitespace(reader);

            // Read until closing brace

            while (reader.Peek() != '}')
            {
                string name;
                JToken value;

                // Read a string

                name = ParseString(reader);

                SkipWhitespace(reader);

                // Skip the colon

                reader.Read();

                SkipWhitespace(reader);

                // Read the value

                value = ParseValue(reader);

                SkipWhitespace(reader);

                finalObject[name] = value;

                // Skip the comma if any

                if (reader.Peek() == ',')
                {
                    reader.Read();
                }

                SkipWhitespace(reader);
            }

            // Skip the closing brace

            reader.Read();

            return finalObject;
        }

        private static object ParseTrue(TextReader reader)
        {
            // Skip 't', 'r', 'u', 'e'

            reader.Read();
            reader.Read();
            reader.Read();
            reader.Read();

            return true;
        }

        private static object ParseFalse(TextReader reader)
        {
            // Skip 'f', 'a', 'l', 's', 'e'

            reader.Read();
            reader.Read();
            reader.Read();
            reader.Read();
            reader.Read();

            return false;
        }

        static object ParseNull(TextReader reader)
        {
            // Skip 'n', 'u', 'l', 'l'

            reader.Read();
            reader.Read();
            reader.Read();
            reader.Read();

            return null;
        }

        public static JToken ParseValue(TextReader reader)
        {
            SkipWhitespace(reader);

            int lookahead = reader.Peek();

            if ((lookahead == '-') || ((lookahead >= '0') && (lookahead <= '9')))
            {
                return new JValue(ParseNumber(reader));
            }
            else if (lookahead == '[')
            {
                return ParseArray(reader);
            }
            else if (lookahead == '{')
            {
                return ParseObject(reader);
            }
            else if (lookahead == 't')
            {
                return new JValue(ParseTrue(reader));
            }
            else if (lookahead == 'f')
            {
                return new JValue(ParseFalse(reader));
            }
            else if (lookahead == 'n')
            {
                return new JValue(ParseNull(reader));
            }
            else
            {
                return new JValue(ParseString(reader));
            }
        }
    }
}
