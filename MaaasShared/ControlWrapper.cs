using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaaasCore
{
    public class ControlWrapper
    {
        StateManager _stateManager;
        ViewModel _viewModel;
        BindingContext _bindingContext;

        Dictionary<string, CommandInstance> _commands = new Dictionary<string, CommandInstance>();
        Dictionary<string, ValueBinding> _valueBindings = new Dictionary<string, ValueBinding>();
        List<PropertyBinding> _propertyBindings = new List<PropertyBinding>();
        List<ControlWrapper> _childControls = new List<ControlWrapper>();

        public ControlWrapper(StateManager stateManager, ViewModel viewModel, BindingContext bindingContext)
        {
            _stateManager = stateManager;
            _viewModel = viewModel;
            _bindingContext = bindingContext;
        }

        public ControlWrapper(ControlWrapper parent, BindingContext bindingContext)
        {
            _stateManager = parent.StateManager;
            _viewModel = parent.ViewModel;
            _bindingContext = bindingContext;
        }

        public StateManager StateManager { get { return _stateManager; } }
        public ViewModel ViewModel { get { return _viewModel; } }

        public BindingContext BindingContext { get { return _bindingContext; } }
        public List<ControlWrapper> ChildControls { get { return _childControls; } }

        protected void SetCommand(string attribute, CommandInstance command)
        {
            _commands[attribute] = command;
        }

        public CommandInstance GetCommand(string attribute)
        {
            if (_commands.ContainsKey(attribute))
            {
                return _commands[attribute];
            }
            return null;
        }

        protected void SetValueBinding(string attribute, ValueBinding valueBinding)
        {
            _valueBindings[attribute] = valueBinding;
        }

        public ValueBinding GetValueBinding(string attribute)
        {
            if (_valueBindings.ContainsKey(attribute))
            {
                return _valueBindings[attribute];
            }
            return null;
        }

        //
        // Value conversion helpers
        //

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
            if (value == null)
            {
                return "";
            }
            else if (value is JToken)
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
                        case JTokenType.Object:
                            result = true;
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

        // Conversion functions to go from Maaas units or typographic points to device units
        //

        public double ToDeviceUnits(object value)
        {
            return StateManager.DeviceMetrics.MaaasUnitsToDeviceUnits(ToDouble(value));
        }

        public double ToDeviceUnitsFromTypographicPoints(object value)
        {
            return StateManager.DeviceMetrics.TypographicPointsToMaaasUnits(ToDouble(value));
        }

        // Silverlight colors
        //
        // http://msdn.microsoft.com/en-us/library/system.windows.media.colors(v=vs.110).aspx
        //
        public enum ColorNames : uint
        {
            AliceBlue = 0xFFF0F8FF,
            AntiqueWhite = 0xFFFAEBD7,
            Aqua = 0xFF00FFFF,
            Aquamarine = 0xFF7FFFD4,
            Azure = 0xFFF0FFFF,
            Beige = 0xFFF5F5DC,
            Bisque = 0xFFFFE4C4,
            Black = 0xFF000000,
            BlanchedAlmond = 0xFFFFEBCD,
            Blue = 0xFF0000FF,
            BlueViolet = 0xFF8A2BE2,
            Brown = 0xFFA52A2A,
            BurlyWood = 0xFFDEB887,
            CadetBlue = 0xFF5F9EA0,
            Chartreuse = 0xFF7FFF00,
            Chocolate = 0xFFD2691E,
            Coral = 0xFFFF7F50,
            CornflowerBlue = 0xFF6495ED,
            Cornsilk = 0xFFFFF8DC,
            Crimson = 0xFFDC143C,
            Cyan = 0xFF00FFFF,
            DarkBlue = 0xFF00008B,
            DarkCyan = 0xFF008B8B,
            DarkGoldenrod = 0xFFB8860B,
            DarkGray = 0xFFA9A9A9,
            DarkGreen = 0xFF006400,
            DarkKhaki = 0xFFBDB76B,
            DarkMagenta = 0xFF8B008B,
            DarkOliveGreen = 0xFF556B2F,
            DarkOrange = 0xFFFF8C00,
            DarkOrchid = 0xFF9932CC,
            DarkRed = 0xFF8B0000,
            DarkSalmon = 0xFFE9967A,
            DarkSeaGreen = 0xFF8FBC8F,
            DarkSlateBlue = 0xFF483D8B,
            DarkSlateGray = 0xFF2F4F4F,
            DarkTurquoise = 0xFF00CED1,
            DarkViolet = 0xFF9400D3,
            DeepPink = 0xFFFF1493,
            DeepSkyBlue = 0xFF00BFFF,
            DimGray = 0xFF696969,
            DodgerBlue = 0xFF1E90FF,
            Firebrick = 0xFFB22222,
            FloralWhite = 0xFFFFFAF0,
            ForestGreen = 0xFF228B22,
            Fuchsia = 0xFFFF00FF,
            Gainsboro = 0xFFDCDCDC,
            GhostWhite = 0xFFF8F8FF,
            Gold = 0xFFFFD700,
            Goldenrod = 0xFFDAA520,
            Gray = 0xFF808080,
            Green = 0xFF008000,
            GreenYellow = 0xFFADFF2F,
            Honeydew = 0xFFF0FFF0,
            HotPink = 0xFFFF69B4,
            IndianRed = 0xFFCD5C5C,
            Indigo = 0xFF4B0082,
            Ivory = 0xFFFFFFF0,
            Khaki = 0xFFF0E68C,
            Lavender = 0xFFE6E6FA,
            LavenderBlush = 0xFFFFF0F5,
            LawnGreen = 0xFF7CFC00,
            LemonChiffon = 0xFFFFFACD,
            LightBlue = 0xFFADD8E6,
            LightCoral = 0xFFF08080,
            LightCyan = 0xFFE0FFFF,
            LightGoldenrodYellow = 0xFFFAFAD2,
            LightGray = 0xFFD3D3D3,
            LightGreen = 0xFF90EE90,
            LightPink = 0xFFFFB6C1,
            LightSalmon = 0xFFFFA07A,
            LightSeaGreen = 0xFF20B2AA,
            LightSkyBlue = 0xFF87CEFA,
            LightSlateGray = 0xFF778899,
            LightSteelBlue = 0xFFB0C4DE,
            LightYellow = 0xFFFFFFE0,
            Lime = 0xFF00FF00,
            LimeGreen = 0xFF32CD32,
            Linen = 0xFFFAF0E6,
            Magenta = 0xFFFF00FF,
            Maroon = 0xFF800000,
            MediumAquamarine = 0xFF66CDAA,
            MediumBlue = 0xFF0000CD,
            MediumOrchid = 0xFFBA55D3,
            MediumPurple = 0xFF9370DB,
            MediumSeaGreen = 0xFF3CB371,
            MediumSlateBlue = 0xFF7B68EE,
            MediumSpringGreen = 0xFF00FA9A,
            MediumTurquoise = 0xFF48D1CC,
            MediumVioletRed = 0xFFC71585,
            MidnightBlue = 0xFF191970,
            MintCream = 0xFFF5FFFA,
            MistyRose = 0xFFFFE4E1,
            Moccasin = 0xFFFFE4B5,
            NavajoWhite = 0xFFFFDEAD,
            Navy = 0xFF000080,
            OldLace = 0xFFFDF5E6,
            Olive = 0xFF808000,
            OliveDrab = 0xFF6B8E23,
            Orange = 0xFFFFA500,
            OrangeRed = 0xFFFF4500,
            Orchid = 0xFFDA70D6,
            PaleGoldenrod = 0xFFEEE8AA,
            PaleGreen = 0xFF98FB98,
            PaleTurquoise = 0xFFAFEEEE,
            PaleVioletRed = 0xFFDB7093,
            PapayaWhip = 0xFFFFEFD5,
            PeachPuff = 0xFFFFDAB9,
            Peru = 0xFFCD853F,
            Pink = 0xFFFFC0CB,
            Plum = 0xFFDDA0DD,
            PowderBlue = 0xFFB0E0E6,
            Purple = 0xFF800080,
            Red = 0xFFFF0000,
            RosyBrown = 0xFFBC8F8F,
            RoyalBlue = 0xFF4169E1,
            SaddleBrown = 0xFF8B4513,
            Salmon = 0xFFFA8072,
            SandyBrown = 0xFFF4A460,
            SeaGreen = 0xFF2E8B57,
            SeaShell = 0xFFFFF5EE,
            Sienna = 0xFFA0522D,
            Silver = 0xFFC0C0C0,
            SkyBlue = 0xFF87CEEB,
            SlateBlue = 0xFF6A5ACD,
            SlateGray = 0xFF708090,
            Snow = 0xFFFFFAFA,
            SpringGreen = 0xFF00FF7F,
            SteelBlue = 0xFF4682B4,
            Tan = 0xFFD2B48C,
            Teal = 0xFF008080,
            Thistle = 0xFFD8BFD8,
            Tomato = 0xFFFF6347,
            Transparent = 0x00FFFFFF,
            Turquoise = 0xFF40E0D0,
            Violet = 0xFFEE82EE,
            Wheat = 0xFFF5DEB3,
            White = 0xFFFFFFFF,
            WhiteSmoke = 0xFFF5F5F5,
            Yellow = 0xFFFFFF00,
            YellowGreen = 0xFF9ACD32
        }

        public class ColorARGB
        {
            byte _a;
            byte _r;
            byte _g;
            byte _b;

            public ColorARGB(byte a, byte r, byte g, byte b)
            {
                _a = a;
                _r = r;
                _g = g;
                _b = b;
            }

            public ColorARGB(int color)
            {
                var bytes = BitConverter.GetBytes(color);
                _a = bytes[3];
                _r = bytes[2];
                _g = bytes[1];
                _b = bytes[0];
            }

            public byte a { get { return _a; } }
            public byte r { get { return _r; } }
            public byte g { get { return _g; } }
            public byte b { get { return _b; } }
        }

        public static ColorARGB getColor(string colorValue)
        {
            if (colorValue.StartsWith("#"))
            {
                colorValue = colorValue.Replace("#", "");
                if (colorValue.Length == 6)
                {
                    return new ColorARGB(255,
                        byte.Parse(colorValue.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                        byte.Parse(colorValue.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                        byte.Parse(colorValue.Substring(4, 2), System.Globalization.NumberStyles.HexNumber));
                }
            }
            else
            {
                ColorNames color = (ColorNames)Enum.Parse(typeof(ColorNames), colorValue);
                if (Enum.IsDefined(typeof(ColorNames), colorValue))
                {
                    return new ColorARGB((int)color);
                }
                else
                {
                    // !!! Color name not found
                }
            }

            return null;
        }

        // Process a value binding on an element.  If a value is supplied, a value binding to that binding context will be created.
        //
        protected Boolean processElementBoundValue(string attributeName, string value, GetViewValue getValue, SetViewValue setValue)
        {
            if (value != null)
            {
                BindingContext valueBindingContext = this.BindingContext.Select(value);
                ValueBinding binding = ViewModel.CreateAndRegisterValueBinding(valueBindingContext, value, getValue, setValue);
                SetValueBinding(attributeName, binding);
                return true;
            }

            return false;
        }

        // Process an element property, which can contain a plain value, a property binding token string, or no value at all,
        // in which case any optionally supplied defaultValue will be used.  This call *may* result in a property binding to
        // the element property, or it may not.
        //
        // This is "public" because there are cases when a parent element needs to process properties on its children after creation.
        //
        public void processElementProperty(string value, SetViewValue setValue, string defaultValue = null)
        {
            if (value == null)
            {
                if (defaultValue != null)
                {
                    setValue(defaultValue);
                }
                return;
            }
            else if (PropertyValue.ContainsBindingTokens(value))
            {
                // If value contains a binding, create a Binding and add it to metadata
                PropertyBinding binding = ViewModel.CreateAndRegisterPropertyBinding(this.BindingContext, value, setValue);
                _propertyBindings.Add(binding);
            }
            else
            {
                // Otherwise, just set the property value
                setValue(value);
            }
        }

        // This helper is used by control update handlers.
        //
        protected void updateValueBindingForAttribute(string attributeName)
        {
            ValueBinding binding = GetValueBinding(attributeName);
            if (binding != null)
            {
                // Update the local ViewModel from the element/control
                binding.UpdateViewModelFromView();
            }
        }

        // Process and record any commands in a binding spec
        //
        protected void ProcessCommands(JObject bindingSpec, string[] commands)
        {
            foreach (string command in commands)
            {
                JObject commandSpec = bindingSpec[command] as JObject;
                if (commandSpec != null)
                {
                    // A command spec contains an attribute called "command".  All other attributes are considered parameters.
                    //
                    CommandInstance commandInstance = new CommandInstance((string)commandSpec["command"]);
                    foreach (var property in commandSpec)
                    {
                        if (property.Key != "command")
                        {
                            commandInstance.SetParameter(property.Key, property.Value);
                        }
                    }
                    SetCommand(command, commandInstance);
                }
            }
        }

        // When we remove a control, we need to unbind it and its descendants (by unregistering all bindings
        // from the view model).  This is important as often times a control is removed when the underlying
        // bound values go away, such as when an array element is removed, causing a cooresponding (bound) list
        // or list view item to be removed.
        //
        public void Unregister()
        {
            foreach (ValueBinding valueBinding in _valueBindings.Values)
            {
                _viewModel.UnregisterValueBinding(valueBinding);
            }

            foreach (PropertyBinding propertyBinding in _propertyBindings)
            {
                _viewModel.UnregisterPropertyBinding(propertyBinding);
            }

            foreach (ControlWrapper childControl in _childControls)
            {
                childControl.Unregister();
            }
        }

        // This will create controls from a list of control specifications.  It will apply any "foreach" and "with" bindings
        // as part of the process.  It will call the supplied callback to actually create the individual controls.
        //
        public void createControls(BindingContext bindingContext, JArray controlList, Action<BindingContext, JObject> onCreateControl)
        {
            foreach (JObject element in controlList)
            {
                BindingContext controlBindingContext = bindingContext;
                Boolean controlCreated = false;

                if ((element["binding"] != null) && (element["binding"].Type == JTokenType.Object))
                {
                    Util.debug("Found binding object");
                    JObject bindingSpec = (JObject)element["binding"];
                    if (bindingSpec["foreach"] != null)
                    {
                        // First we create a BindingContext for the "foreach" path (a context to the elements to be iterated)
                        string bindingPath = (string)bindingSpec["foreach"];
                        Util.debug("Found 'foreach' binding with path: " + bindingPath);
                        BindingContext forEachBindingContext = bindingContext.Select(bindingPath);

                        // Then we determine the bindingPath to use on each element
                        string withPath = "$data";
                        if (bindingSpec["with"] != null)
                        {
                            // It is possible to use "foreach" and "with" together - in which case "foreach" is applied first
                            // and "with" is applied to each element in the foreach array.  This allows for path navigation
                            // both up to, and then after, the context to be iterated.
                            //
                            withPath = (string)bindingSpec["with"];
                        }

                        // Then we get each element at the foreach binding, apply the element path, and create the controls
                        List<BindingContext> bindingContexts = forEachBindingContext.SelectEach(withPath);
                        foreach (var elementBindingContext in bindingContexts)
                        {
                            Util.debug("foreach - creating control with binding context: " + elementBindingContext.BindingPath);
                            onCreateControl(elementBindingContext, element);
                        }
                        controlCreated = true;
                    }
                    else if (bindingSpec["with"] != null)
                    {
                        string withBindingPath = (string)bindingSpec["with"];
                        Util.debug("Found 'with' binding with path: " + withBindingPath);
                        controlBindingContext = bindingContext.Select(withBindingPath);
                    }
                }

                if (!controlCreated)
                {
                    onCreateControl(controlBindingContext, element);
                }
            }
        }
    }
}
