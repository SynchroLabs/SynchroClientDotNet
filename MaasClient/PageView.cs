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
// Each item has a binding context, which is determined before the item is created/processed by inspecting the "binding" attribute
// to look for a "foreach" or "with" binding context specifier.
//
//    foreach: foos (for each element in array "foos", create an instance of this element and set the binding context for
//                   that element to the array element).
//
//    with: foo.bar (select the element foo.bar as the current binding context for the visual element, before apply any other binding).
//
// The "binding" attribute can have tokens in it, but they are only evalutated when it is initially processed (binding
// is only processed once).
//

namespace MaasClient
{
    class PageView
    {
        public String Path { get; set; }
        public Action<string> setPageTitle { get; set; }
        public Action<bool> setBackEnabled { get; set; }
        public Panel Content { get; set; }

        StateManager stateManager;
        ViewModel viewModel;

        string onBackCommand = null;

        public PageView(StateManager stateManager, ViewModel viewModel)
        {
            this.stateManager = stateManager;
            this.viewModel = viewModel;
        }

        public void OnBackCommand(object sender, RoutedEventArgs e)
        {
            Util.debug("Back button click with command: " + onBackCommand);
            this.stateManager.processCommand(onBackCommand);
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

        Boolean processElementBoundValue(FrameworkElement element, string attributeName, JToken bindingContext, string value, GetViewValue getValue, SetViewValue setValue)
        {
            if (value != null)
            {
                ElementMetaData metaData = getMetaData(element);

                ValueBinding binding = this.viewModel.CreateValueBinding(bindingContext, value, getValue, setValue);
                metaData.SetValueBinding(attributeName, binding);
                return true;
            }

            return false;
        }

        void processElementProperty(FrameworkElement element, JToken bindingContext, string value, SetViewValue setValue, string defaultValue = null)
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


        public delegate object ConvertValue(object value);

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

        public void processMarginProperty(FrameworkElement element, JToken bindingContext, JToken margin)
        {
            Thickness thickness = new Thickness();

            if (margin is JValue)
            {
                processElementProperty(element, bindingContext, (string)margin, value => 
                {
                    double marginThickness = Converter.ToDouble(value);
                    thickness.Left = marginThickness;
                    thickness.Top = marginThickness;
                    thickness.Right = marginThickness;
                    thickness.Bottom = marginThickness;
                    element.Margin = thickness; 
                }, "0");
            }
            else if (margin is JObject)
            {
                JObject marginObject = margin as JObject;
                processElementProperty(element, bindingContext, (string)marginObject.Property("left"), value =>
                {
                    thickness.Left = Converter.ToDouble(value);
                    element.Margin = thickness;
                }, "0");
                processElementProperty(element, bindingContext, (string)marginObject.Property("top"), value =>
                {
                    thickness.Top = Converter.ToDouble(value);
                    element.Margin = thickness;
                }, "0");
                processElementProperty(element, bindingContext, (string)marginObject.Property("right"), value =>
                {
                    thickness.Right = Converter.ToDouble(value);
                    element.Margin = thickness;
                }, "0");
                processElementProperty(element, bindingContext, (string)marginObject.Property("bottom"), value =>
                {
                    thickness.Bottom = Converter.ToDouble(value);
                    element.Margin = thickness;
                }, "0");
            }
        }

        static Thickness defaultThickness = new Thickness(0, 0, 10, 10);

        public void applyFrameworkElementDefaults(FrameworkElement element)
        {
            element.Margin = defaultThickness;
            element.HorizontalAlignment = HorizontalAlignment.Left;
        }

        public void processCommonFrameworkElementProperies(FrameworkElement element, JToken bindingContext, JObject controlSpec)
        {
            // !!! TODO: MinHeight/Width, MaxHeight/Width, Tooltip
            //
            //           VerticalAlignment [ Top, Center, Bottom, Stretch ]
            //           HorizontalAlignment [ Left, Center, Right, Stretch ] 
            //

            Util.debug("Processing framework element properties");
            processElementProperty(element, bindingContext, (string)controlSpec["name"], value => element.Name = Converter.ToString(value));
            processElementProperty(element, bindingContext, (string)controlSpec["height"], value => element.Height = Converter.ToDouble(value));
            processElementProperty(element, bindingContext, (string)controlSpec["width"], value => element.Width = Converter.ToDouble(value));
            processElementProperty(element, bindingContext, (string)controlSpec["opacity"], value => element.Opacity = Converter.ToDouble(value));
            processElementProperty(element, bindingContext, (string)controlSpec["visibility"], value => element.Visibility = Converter.ToBoolean(value) ? Visibility.Visible : Visibility.Collapsed);
            processMarginProperty(element, bindingContext, controlSpec["margin"]);

            // These elements are very common among derived classes, so we'll do some runtime reflection...
            processElementPropertyIfPresent(element, bindingContext, (string)controlSpec["fontsize"], "FontSize", value => Converter.ToDouble(value));
            processElementPropertyIfPresent(element, bindingContext, (string)controlSpec["fontweight"], "FontWeight", value => Converter.ToFontWeight(value));
            processElementPropertyIfPresent(element, bindingContext, (string)controlSpec["enabled"], "IsEnabled", value => Converter.ToBoolean(value));
            processElementPropertyIfPresent(element, bindingContext, (string)controlSpec["background"], "Background", value => Converter.ToBrush(value));
            processElementPropertyIfPresent(element, bindingContext, (string)controlSpec["foreground"], "Foreground", value => Converter.ToBrush(value));
        }

        public JToken getListboxContents(ListBox listbox)
        {
            Util.debug("Getting listbox contents");

            string tokenStr = "[";
            foreach (string item in listbox.Items)
            {
                Util.debug("Found listbox item: " + item);
                tokenStr += "\"" + item + "\",";
            }
            tokenStr += "]";
            return JToken.Parse(tokenStr);
        }

        public void setListboxContents(ListBox listbox, JToken contents)
        {
            Util.debug("Setting listbox contents");

            // Keep track of currently selected item/items so we can restore after repopulating list
            string[] selected = listbox.SelectedItems.OfType<string>().ToArray();

            listbox.Items.Clear();
            if ((contents != null) && (contents.Type == JTokenType.Array))
            {
                // !!! Default itemValue is "$data"
                foreach (JToken arrayElementBindingContext in (JArray)contents)
                {
                    // !!! If $data (default), then we get the value of the binding context iteration items.
                    //     Otherwise, if there is a non-default itemData binding, we apply that.
                    string value = (string)arrayElementBindingContext;
                    Util.debug("adding listbox item: " + value);
                    listbox.Items.Add(value);
                }

                foreach (string selection in selected)
                {
                    Util.debug("Previous selection: " + selection);
                    int n = listbox.Items.IndexOf(selection);
                    if (n >= 0)
                    {
                        Util.debug("Found previous selection in list, selecting it!");
                        if (listbox.SelectionMode == SelectionMode.Single)
                        {
                            // If we're single select, we have to select the item via a valid single
                            // select method, like below.  If you try to do it the multi select way by
                            // modifying SelectedItems in single select, you get a "catastrphic" freakout.
                            //
                            listbox.SelectedIndex = n;
                            break;
                        }
                        else
                        {
                            listbox.SelectedItems.Add(listbox.Items[n]);
                        }
                    }
                }
            }
        }

        public JToken getListboxSelection(ListBox listbox)
        {
            if (listbox.SelectionMode == SelectionMode.Multiple)
            {
                string tokenStr = "[";
                foreach (string item in listbox.SelectedItems)
                {
                    Util.debug("Found listbox item: " + item);
                    tokenStr += "\"" + item + "\",";
                }
                tokenStr += "]";

                return JToken.Parse(tokenStr);
            }
            else
            {
                return new JValue(listbox.SelectedItem);
            }
        }

        public void setListboxSelection(ListBox listbox, JToken selection)
        {
            if (listbox.SelectionMode == SelectionMode.Multiple)
            {
                if (selection is JArray)
                {
                    listbox.SelectedItems.Clear();
                    JArray array = selection as JArray;
                    foreach (JToken item in array.Values())
                    {
                        listbox.SelectedItems.Add(Converter.ToString(item));
                    }
                }
                else
                {
                    listbox.SelectedItem = Converter.ToString(selection);
                }
            }
            else
            {
                listbox.SelectedItem = Converter.ToString(selection);
            }
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
                    applyFrameworkElementDefaults(textBlock);
                    setBindingContext(textBlock, bindingContext);
                    processElementProperty(textBlock, bindingContext, (string)controlSpec["value"], value => textBlock.Text = Converter.ToString(value));
                    control = textBlock;
                }
                break;

                case "edit":
                {
                    Util.debug("Found edit element with value of: " + controlSpec["value"]);
                    TextBox textBox = new TextBox();
                    applyFrameworkElementDefaults(textBox);
                    setBindingContext(textBox, bindingContext);
                    JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
                    if (!processElementBoundValue(textBox, "value", bindingContext, (string)bindingSpec["value"], () => { return textBox.Text; }, value => textBox.Text = Converter.ToString(value)))
                    {
                        processElementProperty(textBox, bindingContext, (string)controlSpec["value"], value => textBox.Text = Converter.ToString(value));
                    }
                    textBox.TextChanged += textBox_TextChanged;
                    control = textBox;
                }
                break;

                case "password":
                {
                    Util.debug("Found password element with value of: " + controlSpec["value"]);
                    PasswordBox passwordBox = new PasswordBox();
                    applyFrameworkElementDefaults(passwordBox);
                    setBindingContext(passwordBox, bindingContext);
                    JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
                    if (!processElementBoundValue(passwordBox, "value", bindingContext, (string)bindingSpec["value"], () => { return passwordBox.Password; }, value => passwordBox.Password = Converter.ToString(value)))
                    {
                        processElementProperty(passwordBox, bindingContext, (string)controlSpec["value"], value => passwordBox.Password = Converter.ToString(value));
                    }
                    passwordBox.PasswordChanged += passwordBox_PasswordChanged;
                    control = passwordBox;
                }
                break;

                case "button":
                {
                    Util.debug("Found button element with caption of: " + controlSpec["caption"]);
                    Button button = new Button();
                    applyFrameworkElementDefaults(button);
                    setBindingContext(button, bindingContext);
                    JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "onClick");
                    ElementMetaData metaData = getMetaData(button);
                    metaData.Command = (string)bindingSpec["onClick"];
                    processElementProperty(button, bindingContext, (string)controlSpec["caption"], value => button.Content = Converter.ToString(value));
                    button.Click += button_Click;
                    control = button;
                }
                break;

                case "toggle":
                {
                    Util.debug("Found toggle element with caption of: " + controlSpec["caption"]);
                    ToggleSwitch toggleSwitch = new ToggleSwitch();
                    applyFrameworkElementDefaults(toggleSwitch);
                    setBindingContext(toggleSwitch, bindingContext);
                    JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
                    if (!processElementBoundValue(toggleSwitch, "value", bindingContext, (string)bindingSpec["value"], () => { return toggleSwitch.IsOn; }, value => toggleSwitch.IsOn = Converter.ToBoolean(value)))
                    {
                        processElementProperty(toggleSwitch, bindingContext, (string)controlSpec["value"], value => toggleSwitch.IsOn = Converter.ToBoolean(value));
                    }
                    if (bindingSpec["onToggle"] != null)
                    {
                        ElementMetaData metaData = getMetaData(toggleSwitch);
                        metaData.Command = (string)bindingSpec["onToggle"];
                    }
                    processElementProperty(toggleSwitch, bindingContext, (string)controlSpec["header"], value => toggleSwitch.Header = Converter.ToString(value));
                    processElementProperty(toggleSwitch, bindingContext, (string)controlSpec["onLabel"], value => toggleSwitch.OnContent = Converter.ToString(value));
                    processElementProperty(toggleSwitch, bindingContext, (string)controlSpec["offLabel"], value => toggleSwitch.OffContent = Converter.ToString(value));
                    toggleSwitch.Toggled += toggleSwitch_Toggled;
                    control = toggleSwitch;
                }
                break;

                case "image":
                {
                    Util.debug("Found image element with caption of: " + controlSpec["caption"]);
                    Image image = new Image();
                    applyFrameworkElementDefaults(image);
                    setBindingContext(image, bindingContext);
                    image.Height = 128; // Sizes will be overriden by the generic height/width property handlers, but
                    image.Width = 128;  // we have to set these here (as defaults) in case the sizes aren't specified. 
                    processElementProperty(image, bindingContext, (string)controlSpec["resource"], value => image.Source = new BitmapImage(this.stateManager.buildUri(Converter.ToString(value))));
                    control = image;
                }
                break;

                case "listbox":
                {
                    Util.debug("Found listbox element");
                    ListBox listbox = new ListBox();
                    applyFrameworkElementDefaults(listbox);
                    setBindingContext(listbox, bindingContext);
                    JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "items");
                    if (bindingSpec != null)
                    {
                        if (bindingSpec["items"] != null)
                        {
                            processElementBoundValue(listbox, "items", bindingContext, (string)bindingSpec["items"], () => getListboxContents(listbox), value => this.setListboxContents(listbox, (JToken)value));
                        }
                        if (bindingSpec["selection"] != null)
                        {
                            processElementBoundValue(listbox, "selection", bindingContext, (string)bindingSpec["selection"], () => getListboxSelection(listbox), value => this.setListboxSelection(listbox, (JToken)value));
                        }
                    }
                    // Get selection mode - single (default) or multiple - no dynamic values (we don't need this changing during execution).
                    if ((controlSpec["select"] != null) && ((string)controlSpec["select"] == "multiple"))
                    {
                        listbox.SelectionMode = SelectionMode.Multiple;
                    }
                    listbox.SelectionChanged += listbox_SelectionChanged;
                    control = listbox;
                }
                break;

                // !!! When we do the grid/canvas containers, they are going to need to look at each child control and be able to pull off
                //     positioning attributes (top, left for canvas - row, column for grid).
                //
                case "stackpanel":
                {
                    Util.debug("Found stackpanel element");
                    StackPanel stackPanel = new StackPanel();
                    applyFrameworkElementDefaults(stackPanel);
                    setBindingContext(stackPanel, bindingContext);
                    Orientation orientation = Orientation.Horizontal;
                    if ((controlSpec["orientation"] != null) && ((string)controlSpec["orientation"] == "vertical"))
                    {
                        orientation = Orientation.Vertical;
                    }
                    stackPanel.Orientation = orientation;

                    if (controlSpec["contents"] != null)
                    {
                        createControls(bindingContext, (JArray)controlSpec["contents"], childControl => stackPanel.Children.Add(childControl));
                    }
                    control = stackPanel;
                }
                break;

                case "slider":
                {
                    Util.debug("Found slider element");
                    Slider slider = new Slider();
                    applyFrameworkElementDefaults(slider);
                    setBindingContext(slider, bindingContext);
                    JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "value");
                    if (!processElementBoundValue(slider, "value", bindingContext, (string)bindingSpec["value"], () => { return slider.Value; }, value => slider.Value = Converter.ToDouble(value)))
                    {
                        processElementProperty(slider, bindingContext, (string)controlSpec["value"], value => slider.Value = Converter.ToDouble(value));
                    }

                    processElementProperty(slider, bindingContext, (string)controlSpec["minimum"], value => slider.Minimum = Converter.ToDouble(value));
                    processElementProperty(slider, bindingContext, (string)controlSpec["maximum"], value => slider.Maximum = Converter.ToDouble(value));

                    slider.ValueChanged += slider_ValueChanged;  
                    control = slider;
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
                        Binding binding = BindingHelper.ResolveBinding(bindingPath, this.viewModel.RootBindingContext, bindingContext);
                        JToken arrayBindingContext = binding.GetValue();
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
                        Binding binding = BindingHelper.ResolveBinding(bindingPath, this.viewModel.RootBindingContext, bindingContext);
                        controlBindingContext = binding.GetValue();
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

            this.Path = (string)pageView["path"];

            this.onBackCommand = (string)pageView["onBack"];
            this.setBackEnabled(this.onBackCommand != null);

            string pageTitle = (string)pageView["title"];
            if (pageTitle != null)
            {
                setPageTitle(pageTitle);
            }

            createControls(this.viewModel.RootBindingContext, (JArray)pageView["elements"], control => panel.Children.Add(control));
        }

        void updateValueBindingForAttribute(FrameworkElement element, string attributeName)
        {
            ElementMetaData metaData = element.Tag as ElementMetaData;
            if (metaData != null)
            {
                ValueBinding binding = metaData.GetValueBinding(attributeName);
                if (binding != null)
                {
                    // Update the local ViewModel from the element/control
                    binding.UpdateViewModelFromView();
                }
            }
        }

        //
        // Control update handlers
        //

        void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            // Edit controls have a bad habit of posting a text changed event, and there are cases where 
            // this event is generated based on programmatic setting of text and comes in asynchronously
            // after that programmatic action, making it difficult to distinguish actual user changes.
            // This shortcut will help a lot of the time, but there are still cases where this will be 
            // signalled incorrectly (such as in the case where a control with focus is the target of
            // an update from the server), so we'll do some downstream delta checking as well, but this
            // check will cut down most of the chatter.
            //
            if (textBox.FocusState != FocusState.Unfocused)
            {
                updateValueBindingForAttribute(textBox, "value");
            }
        }

        void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;

            // See giant comment in textBox_TextChanged (above).
            if (passwordBox.FocusState != FocusState.Unfocused)
            {
                updateValueBindingForAttribute(passwordBox, "value");
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

        void toggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            var toggleSwitch = sender as ToggleSwitch;
            updateValueBindingForAttribute(toggleSwitch, "value");
            ElementMetaData metaData = toggleSwitch.Tag as ElementMetaData;
            if ((metaData != null) && (metaData.Command != null))
            {
                Util.debug("ToggleSwitch toggled with command: " + metaData.Command);
                this.stateManager.processCommand(metaData.Command);
            }
        }

        void listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listbox = sender as ListBox;
            updateValueBindingForAttribute(listbox, "selection");
        }

        private void slider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            var slider = sender as Slider;
            updateValueBindingForAttribute(slider, "value");
        }

        //
        // MessageBox stuff...
        //

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
            string message = BindingHelper.ExpandBoundTokensAsString((string)messageBox["message"], this.viewModel.RootBindingContext);

            var messageDialog = new MessageDialog(message);

            if (messageBox["title"] != null)
            {
                messageDialog.Title = BindingHelper.ExpandBoundTokensAsString((string)messageBox["title"], this.viewModel.RootBindingContext);
            }

            if (messageBox["options"] != null)
            {
                JArray options = (JArray)messageBox["options"];
                foreach (JObject option in options)
                {
                    if ((string)option["command"] != null)
                    {
                        messageDialog.Commands.Add(new UICommand(
                            BindingHelper.ExpandBoundTokensAsString((string)option["label"], this.viewModel.RootBindingContext),
                            new UICommandInvokedHandler(this.MessageDialogCommandHandler),
                            BindingHelper.ExpandBoundTokensAsString((string)option["command"], this.viewModel.RootBindingContext))
                            );
                    }
                    else
                    {
                        messageDialog.Commands.Add(new UICommand(
                            BindingHelper.ExpandBoundTokensAsString((string)option["label"], this.viewModel.RootBindingContext),
                            new UICommandInvokedHandler(this.MessageDialogCommandHandler))
                            );
                    }
                }
            }

            await messageDialog.ShowAsync();
        }
    }
}
