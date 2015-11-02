using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace SynchroCore
{
    public enum JTokenType
    {
        Undefined,
        Object,
        Array,
        Integer,
        Float,
        String,
        Boolean,
        Null
    }

    public abstract class JToken
    {
        protected JToken _parent;
        public JToken Parent
        {
            get { return _parent; }
            set
            {
                if ((_parent != null) && (value != null))
                {
                    // This is sort of a safety check. Parent objects/arrays manage the parent
                    // references of their children, and will null them out when removing them,
                    // so it should never be the case that the parent is getting set to non-null
                    // when it is already non-null (owned by another parent).
                    //
                    throw new ArgumentException("Parent value already set.");
                }

                _parent = value;
            }
        }

        public JToken Root
        {
            get
            {
                JToken parent = Parent;
                if (parent == null)
                    return this;

                while (parent.Parent != null)
                {
                    parent = parent.Parent;
                }

                return parent;
            }
        }

        public String Path
        {
            get
            {
                bool useDotNotation = false;
                string path = "";

                JToken parent = Parent;
                if (parent != null)
                {
                    path += Parent.Path;

                    if (parent is JObject)
                    {
                        JObject parentObject = parent as JObject;
                        var keyValuePair = parentObject.First(x => x.Value == this);
                        if (path.Length > 0)
                        {
                            path += ".";
                        }
                        path += keyValuePair.Key;
                    }
                    else if (parent is JArray)
                    {
                        JArray parentArray = parent as JArray;
                        int pos = parentArray.IndexOf(this);
                        if (useDotNotation)
                        {
                            if (path.Length > 0)
                            {
                                path += ".";
                            }
                            path += pos.ToString();
                        }
                        else
                        {
                            path += "[" + pos.ToString() + "]";
                        }
                    }
                }

                return path;
            }
        }

        private static Regex pathRegex = new Regex(@"\[(\d+)\]");

        public JToken SelectToken(string path, bool errorWhenNoMatch = false)
        {
            try
            {
                var pathElements = pathRegex.Replace(path, ".$1").Split('.');
                JToken currentToken = this;
                foreach (var element in pathElements)
                {
                    if (currentToken is JArray)
                    {
                        currentToken = ((JArray)currentToken)[int.Parse(element)];
                    }
                    else if (currentToken is JObject)
                    {
                        currentToken = ((JObject)currentToken)[element];
                    }
                    else
                    {
                        // If you try to go into anything other than an object or array looking for a 
                        // child element, you are barking up the wrong tree...
                        //
                        throw new ArgumentException("The provided path did not resolve to a token");
                    }
                }

                return currentToken;
            }
            catch (Exception)
            {
                if (errorWhenNoMatch)
                {
                    throw;
                }
            }

            return null;
        }

        // Remove this token from its parent
        //
        public bool Remove()
        {
            bool bRemoved = false;

            if (Parent != null)
            {
                if (Parent is JObject)
                {
                    JObject parentObject = Parent as JObject;
                    var keyValuePair = parentObject.First(x => x.Value == this);
                    bRemoved = parentObject.Remove(keyValuePair.Key);
                }
                else if (Parent is JArray)
                {
                    JArray parentArray = Parent as JArray;
                    bRemoved = parentArray.Remove(this);
                }

                if (bRemoved && (this.Parent != null))
                {
                    // Parent should handle nulling parent when this when item removed...
                    throw new InvalidDataException("Item was removed, but parent was not cleared");
                }
            }

            return bRemoved;
        }

        // Replace this token in its parent
        //
        public bool Replace(JToken token)
        {
            bool bReplaced = false;

            if ((Parent != null) && (token != this))
            {
                // Find ourself in our parent, and replace...
                //
                if (Parent is JObject)
                {
                    JObject parentObject = Parent as JObject;
                    var keyValuePair = parentObject.First(x => x.Value == this);
                    parentObject[keyValuePair.Key] = token;
                    bReplaced = true;
                }
                else if (Parent is JArray)
                {
                    JArray parentArray = Parent as JArray;
                    int pos = parentArray.IndexOf(this);
                    parentArray[pos] = token;
                    bReplaced = true;
                }

                if (bReplaced && (this.Parent != null))
                {
                    // Parent should handle nulling parent when this when item removed...
                    throw new InvalidDataException("Item was replaced, but parent was not cleared");
                }
            }

            return bReplaced;
        }

        // Update a token to a new value, attempting to preserve the object graph to the extent possible
        //
        public static Boolean UpdateTokenValue(ref JToken currentToken, JToken newToken)
        {
            if (currentToken != newToken)
            {
                if ((currentToken is JValue) && (newToken is JValue))
                {
                    // If the current token and the new token are both primitive values, then we just do a 
                    // value assignment...
                    //
                    ((JValue)currentToken).Value = ((JValue)newToken).Value;
                }
                else
                {
                    // Otherwise we have to replace the current token with the new token in the current token's parent...
                    //
                    if (currentToken.Replace(newToken))
                    {
                        currentToken = newToken;
                        return true; // Token change
                    }
                }
            }
            return false; // Value-only change, or no change
        }

        public static bool DeepEquals(JToken token1, JToken token2)
        {
            return ((token1 == token2) || (token1 != null && token2 != null && token1.DeepEquals(token2)));
        }

        public abstract bool DeepEquals(JToken token);

        public abstract JToken DeepClone();

        public abstract JTokenType Type { get; }

        public static explicit operator bool(JToken token)
        {
            JValue value = token as JValue;
            if (value != null)
            {
                return (bool)value;
            }

            throw new ArgumentException(String.Format("Can not convert {0} to Boolean", token));
        }

        public static explicit operator int(JToken token)
        {
            JValue value = token as JValue;
            if (value != null)
            {
                return (int)value;
            }

            throw new ArgumentException(String.Format("Can not convert {0} to Int", value));
        }

        // See comment on JValue uint converter.
        //
        public static explicit operator uint(JToken token)
        {
            JValue value = token as JValue;
            if (value != null)
            {
                return (uint)value;
            }

            throw new ArgumentException(String.Format("Can not convert {0} to UInt", value));
        }

        public static explicit operator double(JToken token)
        {
            JValue value = token as JValue;
            if (value != null)
            {
                return (double)value;
            }

            throw new ArgumentException(String.Format("Can not convert {0} to Float", value));
        }

        public static explicit operator string(JToken token)
        {
            if ((token == null) || (token.Type == JTokenType.Null))
            {
                return null;
            }

            JValue value = token as JValue;
            if (value != null)
            {
                return (string)value;
            }

            throw new ArgumentException(String.Format("Can not convert {0} to String", value));
        }

        public static JToken Parse(string str)
        {
            return JsonParser.ParseValue(new StringReader(str));
        }

        public string ToJson()
        {
            var writer = new StringWriter();
            JsonWriter.WriteValue(writer, this);
            return writer.ToString();
        }
    }

    public class JObject : JToken, IDictionary<string, JToken>
    {
        IDictionary<string, JToken> _tokens = new Dictionary<string, JToken>();

        public override JTokenType Type { get { return JTokenType.Object; } }

        public JToken GetValue(string name)
        {
            if (name == null)
            {
                return null;
            }

            JToken value = null;
            _tokens.TryGetValue(name, out value);
            return value;
        }

        public override bool DeepEquals(JToken token)
        {
            JObject other = token as JObject;
            if (other == null)
            {
                return false;
            }
            else if (other == this)
            {
                return true;
            }
            else if ((this.Count == 0) && (other.Count == 0))
            {
                return true;
            }
            else if (this.Count != other.Count)
            {
                return false;
            }

            // If we get here, we have two different, non-empty dictionaries with the same number of keys
            //
            foreach (KeyValuePair<string, JToken> keyAndProperty in _tokens)
            {
                JToken otherValue;
                if (!other.TryGetValue(keyAndProperty.Key, out otherValue))
                {
                    return false;
                }
                else if ((keyAndProperty.Value == null) && (otherValue != null))
                {
                    return false;
                }
                else if (!keyAndProperty.Value.DeepEquals(otherValue))
                {
                    return false;
                }
            }

            return true;
        }

        public override JToken DeepClone()
        {
            JObject clone = new JObject();
            foreach (var key in _tokens.Keys)
            {
                clone[key] = _tokens[key].DeepClone();
            }
            return clone;
        }

        public static explicit operator string(JObject obj)
        {
            if (obj == null)
            {
                return null;
            }

            return ((JToken)obj).ToJson();
        }

        public override string ToString()
        {
            return (string)this;
        }

        #region IDictionary<string,JToken> Members

        protected void SetItem(string key, JToken value, bool add)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            JToken existingValue;
            if (_tokens.TryGetValue(key, out existingValue))
            {
                if (add)
                {
                    throw new ArgumentException("An item with the same key has already been added.");
                }

                if (Equals(value, existingValue))
                {
                    return;
                }

                // Item being replaced
                // 
                existingValue.Parent = null;
            }

            if (value.Parent != null)
            {
                value.Remove();
            }
            value.Parent = this;
            _tokens[key] = value;
        }

        protected bool ClearItem(string key)
        {
            bool bCleared = false;
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            JToken existingValue;
            if (_tokens.TryGetValue(key, out existingValue))
            {
                existingValue.Parent = null;
                bCleared = _tokens.Remove(key);
            }
            else
            {
                throw new ArgumentException("No item exists with specified key.");
            }

            return bCleared;
        }

        public void Add(string key, JToken value)
        {
            SetItem(key, value, true);
        }

        public bool ContainsKey(string key)
        {
            return _tokens.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get { return _tokens.Keys; }
        }

        public bool Remove(string key)
        {
            return ClearItem(key);
        }

        public bool TryGetValue(string key, out JToken value)
        {
            return _tokens.TryGetValue(key, out value);
        }

        public ICollection<JToken> Values
        {
            get { return _tokens.Values; }
        }

        public JToken this[string key]
        {
            get
            {
                return _tokens.ContainsKey(key) ? _tokens[key] : null;
            }
            set
            {
                SetItem(key, value, false);
            }
        }

        public void Add(KeyValuePair<string, JToken> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            foreach (string key in _tokens.Keys)
            {
                ClearItem(key);
            }
        }

        public bool Contains(KeyValuePair<string, JToken> item)
        {
            return _tokens.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, JToken>[] array, int arrayIndex)
        {
            _tokens.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _tokens.Count; }
        }

        public bool IsReadOnly
        {
            get { return _tokens.IsReadOnly; }
        }

        public bool Remove(KeyValuePair<string, JToken> item)
        {
            return Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<string, JToken>> GetEnumerator()
        {
            return _tokens.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _tokens.GetEnumerator();
        }

        #endregion
    }

    public class JArray : JToken, IList<JToken>
    {
        IList<JToken> _tokens = new List<JToken>();

        public override JTokenType Type { get { return JTokenType.Array; } }

        public override bool DeepEquals(JToken token)
        {
            JArray other = token as JArray;
            if (other == null)
            {
                return false;
            }
            else if (other == this)
            {
                return true;
            }
            else if ((this.Count == 0) && (other.Count == 0))
            {
                return true;
            }
            else if (this.Count != other.Count)
            {
                return false;
            }

            for (int i = 0; i < this.Count; i++)
            {
                if (!this[i].DeepEquals(other[i]))
                {
                    return false;
                }
            }

            return true;
        }

        override public JToken DeepClone()
        {
            JArray clone = new JArray();
            foreach (var element in _tokens)
            {
                clone.Add(element.DeepClone());
            }
            return clone;
        }

        #region IList<JToken> Members

        protected void SetItem(int index, JToken value)
        {
            if (index == -1)
            {
                // Item being added
                //
                if (value.Parent != null)
                {
                    value.Remove();
                }
                value.Parent = this;
                _tokens.Add(value);
            }
            else if ((index >= 0) && (index < _tokens.Count))
            {
                // Item being replaced
                // 
                JToken existingValue = _tokens.ElementAt(index);

                if (Equals(value, existingValue))
                {
                    return;
                }

                existingValue.Parent = null;

                if (value.Parent != null)
                {
                    value.Remove();
                }
                value.Parent = this;
                _tokens[index] = value;
            }
            else
            {
                throw new ArgumentException("Index out of range.");
            }
        }

        protected void ClearItem(int index)
        {
            if ((index < 0) || (index >= _tokens.Count))
            {
                throw new ArgumentException("Index out of range.");
            }

            JToken existingValue = _tokens.ElementAt(index);
            existingValue.Parent = null;
            _tokens.RemoveAt(index);
        }

        public int IndexOf(JToken item)
        {
            return _tokens.IndexOf(item);
        }

        public void Insert(int index, JToken item)
        {
            SetItem(index, item);
        }

        public void RemoveAt(int index)
        {
            ClearItem(index);
        }

        public JToken this[int index]
        {
            get
            {
                return _tokens[index];
            }
            set
            {
                SetItem(index, value);
            }
        }

        public void Add(JToken item)
        {
            SetItem(-1, item);
        }

        public void Clear()
        {
            for (int i = _tokens.Count - 1; i >= 0; i--)
            {
                ClearItem(i);
            }
        }

        public bool Contains(JToken item)
        {
            return _tokens.Contains(item);
        }

        public void CopyTo(JToken[] array, int arrayIndex)
        {
            _tokens.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _tokens.Count; }
        }

        public bool IsReadOnly
        {
            get { return _tokens.IsReadOnly; }
        }

        public bool Remove(JToken item)
        {
            int index = _tokens.IndexOf(item);
            if (index >= 0)
            {
                ClearItem(index);
                return true;
            }
            return false;
        }

        public IEnumerator<JToken> GetEnumerator()
        {
            return _tokens.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _tokens.GetEnumerator();
        }

        #endregion
    }

    public class JValue : JToken
    {
        public object Value;

        public JValue(object value)
        {
            if (value is JValue)
            {
                // Copy constructor
                Value = ((JValue)value).Value;
            }
            else if ((value == null) || (value is ValueType) || (value is String))
            {
                Value = value;
            }
            else
            {
                throw new ArgumentException("Value must be null, ValueType, or string, was type: " + value.GetType());
            }
        }

        public override JTokenType Type
        {
            get
            {
                if (Value == null)
                {
                    return JTokenType.Null;
                }
                else if (Value is bool)
                {
                    return JTokenType.Boolean;
                }
                else if (Value is int || Value is long || Value is short || Value is sbyte ||
                         Value is ulong || Value is uint || Value is ushort || Value is byte)
                {
                    return JTokenType.Integer;
                }
                else if (Value is double || Value is float || Value is decimal)
                {
                    return JTokenType.Float;
                }
                else if (Value is string)
                {
                    return JTokenType.String;
                }

                return JTokenType.Undefined;
            }
        }

        public override bool DeepEquals(JToken token)
        {
            JValue other = token as JValue;
            if (other == this)
            {
                return true;
            }
            else if (other == null)
            {
                return false;
            }
            else if ((this.Value == null) || (other.Value == null))
            {
                return this.Value == other.Value;
            }
            return this.Value.Equals(other.Value);
        }

        override public JToken DeepClone()
        {
            return new JValue(this);
        }

        public static explicit operator bool(JValue value)
        {
            if (value.Type == JTokenType.Boolean)
            {
                return (bool)value.Value;
            }

            throw new ArgumentException(String.Format("Can not convert {0} to Boolean", value));
        }

        public static explicit operator int(JValue value)
        {
            if (value.Type == JTokenType.Integer)
            {
                return Convert.ToInt32(value.Value);
            }

            throw new ArgumentException(String.Format("Can not convert {0} to Int", value));
        }

        // The general idea is to provide explicit converters for the base JSON types.  The exception
        // is "uint".  When a (uint) caset is applied to a JToken/JValue, it does not use the explicit
        // int converter, but instead uses the explicit double converter (presumably because it thinks
        // it would be less lossy to go from a double to a uint than from an int to a uint, which only
        // half makes sense).  Anyway, we want to make sure uint casts work on integer values, so we 
        // implement this to make that happen.
        //
        public static explicit operator uint(JValue value)
        {
            if (value.Type == JTokenType.Integer)
            {
                return Convert.ToUInt32(value.Value);
            }

            throw new ArgumentException(String.Format("Can not convert {0} to UInt", value));
        }

        public static explicit operator double(JValue value)
        {
            if ((value.Type == JTokenType.Integer) || (value.Type == JTokenType.Float))
            {
                return Convert.ToDouble(value.Value);
            }

            throw new ArgumentException(String.Format("Can not convert {0} to Float", value));
        }

        public static explicit operator string(JValue value)
        {
            if ((value == null) || (value.Type == JTokenType.Null))
            {
                return null;
            }
            else if (value.Type == JTokenType.String)
            {
                return (string)value.Value;
            }
            return Convert.ToString(value.Value);
        }

        public override string ToString()
        {
            return (string)this;
        }
    }
}
