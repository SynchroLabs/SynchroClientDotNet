using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// Composite binding can be in any attribute.  Substitutes bound items, indicated by braces, into pattern strings.
//    ! = negation of boolean value
//    ^ = One time binding (default is one way binding)
//
// binding = { value: "foo } is equivalent to the common shorthand: binding = "foo" (default attribute is "value")
//
//    foreach: foos (for each element in array "foos", create an instance of this element and set the binding context for
//                   that element to the array element).
//
//    with: foo.bar (select the element foo.bar as the current binding context for the visual element, before apply any other binding).
//
//    event handlers - event: command - for example - onClick: incrementCounter 
//

// Each item has a binding context, which is determined before the item is created/processed by inspecting the "binding" attribute
// to look for a "foreach" or "with" binding context specifier.
//
// The "binding" attribute can have tokens in it, but they are only evalutated when it is initially processed (binding
// is only processed once).
//
// Each element type can have a default binding context, so that binding can be expressed with a simple value.  For example,
// for an edit control, the default binding would be to "value", whereas for a button control the default binding would be
// to "onClick".
//



namespace MaasClient
{
    class PageView
    {
        public String Path { get; set; }
        public Action<string> setPageTitle { get; set; }
        public Panel Content { get; set; }

        StateManager stateManager;

        ViewModel viewModel = new ViewModel();

        public PageView(StateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        ElementMetaData getMetaData(FrameworkElement element)
        {
            ElementMetaData metaData = element.Tag as ElementMetaData;
            if (metaData == null)
            {
                metaData = new ElementMetaData();
                element.Tag = metaData;
            }

            return metaData;
        }

        void setBindingContext(FrameworkElement element, JToken bindingContext)
        {
            ElementMetaData metaData = getMetaData(element);
            metaData.BindingContext = bindingContext;
        }

        Boolean processElementBoundValue(FrameworkElement element, JToken bindingContext, string value, GetValue getValue, SetValue setValue)
        {
            if (value != null)
            {
                ElementMetaData metaData = getMetaData(element);

                ValueBinding binding = this.viewModel.CreateValueBinding(bindingContext, value, getValue, setValue);
                metaData.ValueBinding = binding;
                return true;
            }

            return false;
        }

        void processElementProperty(FrameworkElement element, JToken bindingContext, string value, SetValue setValue, string defaultValue = null)
        {
            if (value == null)
            {
                if (defaultValue != null)
                {
                    setValue(defaultValue);
                }
                return;
            }
            else if (BindingHelper.ContainsBindingTokens(value))
            {
                // If value contains a binding, create a Binding and add it to metadata
                ElementMetaData metaData = getMetaData(element);
                PropertyBinding binding = this.viewModel.CreatePropertyBinding(bindingContext, value, setValue);
                metaData.PropertyBindings.Add(binding);
            }
            else
            {
                // Otherwise, just set the property value
                setValue(value);
            }
        }

        // !!! Currently unused
        public Boolean tokenToBoolean(JToken token)
        {
            Boolean result = false;

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
                }
            }
            return result;
        }

        public delegate object ConvertValue(String value);

        // This method allows us to do some runtime reflection to see if a property exists on an element, and if so, to bind to it.  This
        // is needed because there are common properties of FrameworkElement instances that are repeated in different class trees.  For
        // example, "IsEnabled" exists as a property on most instances of FrameworkElement objects, though it is not defined in a single
        // common base class.
        //
        public void processElementPropertyIfPresent(FrameworkElement element, JToken bindingContext, string attributeValue, string propertyName, ConvertValue convertValue = null)
        {
            if (attributeValue != null)
            {
                if (convertValue == null)
                {
                    convertValue = value => value;
                }
                var property = element.GetType().GetRuntimeProperty(propertyName);
                if (property != null)
                {
                    processElementProperty(element, bindingContext, attributeValue, value => property.SetValue(element, convertValue(value), null));
                }
            }
        }

        public SolidColorBrush ColorStringToBrush(string color)
        {
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

        public FontWeight GetFontWeight(string weight)
        {
            var property = typeof(FontWeights).GetRuntimeProperty(weight);
            if (property != null)
            {
                return (FontWeight)property.GetValue(null);
            }
            return FontWeights.Normal;
        }

        public void processCommonFrameworkElementProperies(FrameworkElement element, JToken bindingContext, JObject controlSpec)
        {
            // !!! TODO: Margin (top, left, bottom, right), MinHeight/Width, MaxHeight/Width, Tooltip
            //
            Util.debug("Processing framework element properties");
            processElementProperty(element, bindingContext, (string)controlSpec["name"], value => element.Name = value);
            processElementProperty(element, bindingContext, (string)controlSpec["height"], value => element.Height = Convert.ToDouble(value));
            processElementProperty(element, bindingContext, (string)controlSpec["width"], value => element.Width = Convert.ToDouble(value));
            processElementProperty(element, bindingContext, (string)controlSpec["margin"], value => element.Margin = new Thickness(Convert.ToDouble(value)));
            processElementProperty(element, bindingContext, (string)controlSpec["opacity"], value => element.Opacity = Convert.ToDouble(value));
            processElementProperty(element, bindingContext, (string)controlSpec["visibility"], value => element.Visibility = Convert.ToBoolean(value) ? Visibility.Visible : Visibility.Collapsed);

            // These elements are very common among derived classes, so we'll do some runtime reflection...
            processElementPropertyIfPresent(element, bindingContext, (string)controlSpec["fontsize"], "FontSize", value => Convert.ToDouble(value));
            processElementPropertyIfPresent(element, bindingContext, (string)controlSpec["fontweight"], "FontWeight", value => GetFontWeight(value));
            processElementPropertyIfPresent(element, bindingContext, (string)controlSpec["enabled"], "IsEnabled", value => Convert.ToBoolean(value));
            processElementPropertyIfPresent(element, bindingContext, (string)controlSpec["background"], "Background", value => ColorStringToBrush(value));
            processElementPropertyIfPresent(element, bindingContext, (string)controlSpec["foreground"], "Foreground", value => ColorStringToBrush(value));
        }

        public FrameworkElement createControl(JToken bindingContext, JObject controlSpec)
        {
            FrameworkElement control = null;

            switch ((string)controlSpec["type"])
            {
                // For each, process generic properties, then type-specific properties, keeping 
                // track of bindings
                //
                case "text":
                {
                    Util.debug("Found text element with value of: " + controlSpec["value"]);
                    TextBlock textBlock = new TextBlock();
                    setBindingContext(textBlock, bindingContext);
                    processElementProperty(textBlock, bindingContext, (string)controlSpec["value"], value => textBlock.Text = value);
                    control = textBlock;
                }
                break;

                case "edit":
                {
                    Util.debug("Found text element with value of: " + controlSpec["value"]);
                    TextBox textBox = new TextBox();
                    setBindingContext(textBox, bindingContext);
                    JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
                    if (!processElementBoundValue(textBox, bindingContext, (string)bindingSpec["value"], () => { return textBox.Text; }, value => textBox.Text = value))
                    {
                        processElementProperty(textBox, bindingContext, (string)controlSpec["value"], value => textBox.Text = value);
                    }
                    textBox.TextChanged += textBox_TextChanged;
                    control = textBox;
                }
                break;

                case "button":
                {
                    Util.debug("Found button element with caption of: " + controlSpec["caption"]);
                    Button button = new Button();
                    setBindingContext(button, bindingContext);
                    JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "onClick");
                    ElementMetaData metaData = getMetaData(button);
                    metaData.Command = (string)bindingSpec["onClick"];
                    processElementProperty(button, bindingContext, (string)controlSpec["caption"], value => button.Content = value);
                    button.Click += button_Click;
                    control = button;
                }
                break;

                case "image":
                {
                    // !!! Should support dynamic value (binding) for source
                    //
                    Util.debug("Found image element with caption of: " + controlSpec["caption"]);
                    Image image = new Image();
                    setBindingContext(image, bindingContext);

                    // Create source
                    image.Source = new BitmapImage(this.stateManager.buildUri((string)controlSpec["resource"]));
                    image.Height = 128;
                    image.Width = 128;

                    control = image;
                }
                break;

                case "listbox":
                {
                    Util.debug("Found listbox element");
                    ListBox listbox = new ListBox();
                    setBindingContext(listbox, bindingContext);
                    JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "items");
                    if ((bindingSpec != null) && (bindingSpec["items"] != null))
                    {
                        // !!! How is the ongoing binding going to work?
                        Binding binding = BindingHelper.ResolveBinding(this.viewModel.BoundItems, bindingContext, (string)bindingSpec["items"]);
                        JToken arrayBindingContext = binding.BoundToken;
                        if ((arrayBindingContext != null) && (arrayBindingContext.Type == JTokenType.Array))
                        {
                            // !!! Default itemValue is "$data"
                            foreach (JToken arrayElementBindingContext in (JArray)arrayBindingContext)
                            {
                                // !!! If $data (default), then we get the value of the binding context iteration items.
                                //     Otherwise, if there is a non-default itemData binding, we apply that.
                                listbox.Items.Add((string)arrayElementBindingContext);
                            }
                        }
                    }

                    control = listbox;
                }
                break;

                case "stackpanel":
                {
                    Util.debug("Found stackpanel element");
                    StackPanel stackPanel = new StackPanel();
                    setBindingContext(stackPanel, bindingContext);
                    stackPanel.Orientation = Orientation.Horizontal; // !!! Should come from controlSpec (bound?)
                    if (controlSpec["contents"] != null)
                    {
                        createControls(bindingContext, (JArray)controlSpec["contents"], childControl => stackPanel.Children.Add(childControl));
                    }
                    control = stackPanel;
                }
                break;
            }

            if (control != null)
            {
                processCommonFrameworkElementProperies(control, bindingContext, controlSpec);
            }

            return control;
        }

        public void createControls(JToken bindingContext, JArray controlList, Action<FrameworkElement> onAddControl)
        {
            foreach (JObject element in controlList)
            {
                JToken controlBindingContext = bindingContext;
                Boolean controlCreated = false;

                if ((element["binding"] != null) && (element["binding"].Type == JTokenType.Object))
                {
                    Util.debug("Found binding object");
                    JObject bindingSpec = (JObject)element["binding"];
                    if (bindingSpec["foreach"] != null)
                    {
                        // !!! We need to save the element (spec) in case this context array grows/changes later
                        string bindingPath = (string)bindingSpec["foreach"];
                        Util.debug("Found 'foreach' binding with path: " + bindingPath);
                        Binding binding = BindingHelper.ResolveBinding(this.viewModel.BoundItems, bindingContext, bindingPath);
                        JToken arrayBindingContext = binding.BoundToken;
                        if ((arrayBindingContext != null) && (arrayBindingContext.Type == JTokenType.Array))
                        {
                            foreach (JObject arrayElementBindingContext in (JArray)arrayBindingContext)
                            {
                                Util.debug("foreach - creating control with binding context: " + arrayElementBindingContext.Path);
                                onAddControl(createControl(arrayElementBindingContext, element));
                            }
                        }
                        controlCreated = true;
                    }
                    else if (bindingSpec["with"] != null)
                    {
                        string bindingPath = (string)bindingSpec["with"];
                        Util.debug("Found 'with' binding with path: " + bindingPath);
                        Binding binding = BindingHelper.ResolveBinding(this.viewModel.BoundItems, bindingContext, bindingPath);
                        controlBindingContext = binding.BoundToken;
                    }
                }

                if (!controlCreated)
                {
                    onAddControl(createControl(controlBindingContext, element));
                }
            }
        }

        public void processPageView(JObject pageView)
        {
            Panel panel = this.Content;
            panel.Children.Clear();

            this.Path = (string)pageView["Path"];

            string pageTitle = (string)pageView["Title"];
            if (pageTitle != null)
            {
                setPageTitle(pageTitle);
            }

            createControls(this.viewModel.BoundItems, (JArray)pageView["Elements"], control => panel.Children.Add(control));
        }

        void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            ElementMetaData metaData = textBox.Tag as ElementMetaData;
            if ((metaData != null) && (metaData.ValueBinding != null))
            {
                metaData.ValueBinding.UpdateValue();
            }
        }

        void button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            ElementMetaData metaData = button.Tag as ElementMetaData;
            if ((metaData != null) && (metaData.Command != null))
            {
                Util.debug("Button click with command: " + metaData.Command);
                this.stateManager.processCommand(metaData.Command);
            }
            else
            {
                Util.debug("Button click with no action");
            }
        }

        /*
         * Keeping this dead listener code around for now.
         * 
        void jsonObject_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            JObject boundItems = (JObject)sender;
            Util.debug("Property " + e.PropertyName + " changed to value: " + boundItems.GetValue(e.PropertyName));
        }

        void jsonArray_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Util.debug("collectionChanged: " + sender);
        }

        public void addChangeListeners(JObject boundItems)
        {
            boundItems.PropertyChanged += jsonObject_PropertyChanged;

            IList<string> keys = boundItems.Properties().Select(p => p.Name).ToList();
            foreach (string key in keys)
            {
                if (boundItems[key].Type == JTokenType.Array)
                {
                    Util.debug("Found array at key: " + key + " and added listener");
                    JArray boundArray = (JArray)boundItems[key];
                    boundArray.CollectionChanged += jsonArray_CollectionChanged;
                }
            }
        }
         * 
         */

        public void newViewItems(JObject boundItems)
        {
            this.viewModel.InitializeViewModelData(boundItems);
        }

        public void updatedViewItems(JToken boundItems)
        {
            this.viewModel.UpdateViewModelData(boundItems);
        }

        public void updateView()
        {
            this.viewModel.UpdateView();
        }

        public void collectBoundItemValues(Action<string, string> setValue)
        {
            this.viewModel.CollectChangedValues(setValue);
        }

        private void MessageDialogCommandHandler(IUICommand command)
        {
            Util.debug("MessageBox Command invoked: " + command.Label);
            if (command.Id != null)
            {
                Util.debug("MessageBox command: " + (string)command.Id);
                this.stateManager.processCommand((string)command.Id); 
            }
        }

        public async void processMessageBox(JObject messageBox)
        {
            string message = BindingHelper.ExpandBoundTokens(this.viewModel.BoundItems, this.viewModel.BoundItems, (string)messageBox["message"]);

            var messageDialog = new MessageDialog(message);

            if (messageBox["title"] != null)
            {
                messageDialog.Title = BindingHelper.ExpandBoundTokens(this.viewModel.BoundItems, this.viewModel.BoundItems, (string)messageBox["title"]);
            }

            if (messageBox["options"] != null)
            {
                JArray options = (JArray)messageBox["options"];
                foreach (JObject option in options)
                {
                    if ((string)option["command"] != null)
                    {
                        messageDialog.Commands.Add(new UICommand(
                            BindingHelper.ExpandBoundTokens(this.viewModel.BoundItems, this.viewModel.BoundItems, (string)option["label"]),
                            new UICommandInvokedHandler(this.MessageDialogCommandHandler),
                            BindingHelper.ExpandBoundTokens(this.viewModel.BoundItems, this.viewModel.BoundItems, (string)option["command"]))
                            );
                    }
                    else
                    {
                        messageDialog.Commands.Add(new UICommand(
                            BindingHelper.ExpandBoundTokens(this.viewModel.BoundItems, this.viewModel.BoundItems, (string)option["label"]),
                            new UICommandInvokedHandler(this.MessageDialogCommandHandler))
                            );
                    }
                }
            }

            await messageDialog.ShowAsync();
        }
    }
}
