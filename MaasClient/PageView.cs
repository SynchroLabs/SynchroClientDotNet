using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// Composite binding can be in any attribute.  Substitutes bound items, indicated by braces, into pattern strings.
//    ! = negation of boolean value
//    ? = One time binding (default is one way binding)
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



namespace MaasClient
{
    class PageView
    {
        public String Path { get; set; }
        public Action<string> setPageTitle { get; set; }
        public Panel Content { get; set; }

        StateManager stateManager;

        JObject boundItems;


        public PageView(StateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        void debug(string str)
        {
            System.Diagnostics.Debug.WriteLine(str);
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

        Boolean processElementBoundValue(FrameworkElement element, string value, GetValue getValue, SetValue setValue)
        {
            if (value != null)
            {
                ElementMetaData metaData = getMetaData(element);

                ValueBinding binding = new ValueBinding();
                binding.BoundValue = value;
                binding.GetValue = getValue;
                binding.SetValue = setValue;
                metaData.ValueBinding = binding;
                return true;
            }

            return false;
        }

        void processElementProperty(FrameworkElement element, string value, SetValue setValue, string defaultValue = null)
        {
            if (value == null)
            {
                if (defaultValue != null)
                {
                    setValue(defaultValue);
                }
                return;
            }
            else if (value.Contains("{"))
            {
                // If value contains a binding, create a Binding and add it to metadata
                ElementMetaData metaData = getMetaData(element);

                PropertyBinding binding = new PropertyBinding();
                binding.Content = value;
                binding.SetValue = setValue;
                metaData.PropertyBindings.Add(binding);
            }
            else
            {
                // Otherwise, just set the property value
                setValue(value);
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
                    debug("Found text element with value of: " + controlSpec["value"]);
                    TextBlock textBlock = new TextBlock();
                    setBindingContext(textBlock, bindingContext);
                    processElementProperty(textBlock, (string)controlSpec["value"], value => textBlock.Text = value);
                    processElementProperty(textBlock, (string)controlSpec["fontsize"], value => textBlock.FontSize = Convert.ToDouble(value));
                    control = textBlock;
                }
                break;

                case "edit":
                {
                    debug("Found text element with value of: " + controlSpec["value"]);
                    TextBox textBox = new TextBox();
                    setBindingContext(textBox, bindingContext);
                    if (!processElementBoundValue(textBox, (string)controlSpec["boundValue"], () => { return textBox.Text; }, value => textBox.Text = value))
                    {
                        processElementProperty(textBox, (string)controlSpec["value"], value => textBox.Text = value);
                    }
                    processElementProperty(textBox, (string)controlSpec["fontsize"], value => textBox.FontSize = Convert.ToDouble(value));
                    control = textBox;
                }
                break;

                case "button":
                {
                    debug("Found button element with caption of: " + controlSpec["caption"]);
                    Button button = new Button();
                    setBindingContext(button, bindingContext);
                    processElementProperty(button, (string)controlSpec["caption"], value => button.Content = value);
                    ElementMetaData metaData = getMetaData(button);
                    metaData.Command = (string)controlSpec["command"];
                    button.Click += button_Click;
                    control = button;
                }
                break;

                case "image":
                {
                    // !!! Should support dynamic value (binding) for source
                    //
                    debug("Found image element with caption of: " + controlSpec["caption"]);
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
                    debug("Found listbox element");
                    ListBox listbox = new ListBox();
                    setBindingContext(listbox, bindingContext);
                    listbox.Width = 200;
                    listbox.Items.Add("Foo");
                    control = listbox;
                }
                break;

            }
            return control;
        }

        Regex bindingTokensRE = new Regex(@"[$]([^$]*)[.]");

        public JToken getBindingContext(JToken bindingContext, string bindingPath)
        {
            // Process path elements:
            //  $root
            //  $parent (and $parents[n]?)
            //  $data
            //  $index (inside foreach)
            //  $parentContext
            //
            JToken bindingContextBase = bindingContext;
            string relativeBindingPath = bindingTokensRE.Replace(bindingPath, delegate(Match m)
            {
                string pathElement = m.Groups[1].ToString();
                debug("Found binding path element: " + pathElement);
                if (pathElement == "root")
                {
                    bindingContextBase = this.boundItems;
                }
                else if (pathElement == "parent")
                {
                    bindingContextBase = bindingContextBase.Parent;
                }
                return ""; // Removing the path elements as they are processed
            });

            return bindingContextBase.SelectToken(relativeBindingPath);
        }

        public void createControls(JToken bindingContext, JArray controlList, Action<FrameworkElement> onAddControl)
        {
            foreach (JObject element in controlList)
            {
                JToken controlBindingContext = bindingContext;
                Boolean controlCreated = false;

                if ((element["binding"] != null) && (element["binding"].Type == JTokenType.Object))
                {
                    debug("Found binding object");
                    JObject binding = (JObject)element["binding"];
                    if (binding["foreach"] != null)
                    {
                        // !!! We need to save the element (spec) in case this context array grows/changes later
                        string bindingPath = (string)binding["foreach"];
                        debug("Found 'foreach' binding with path: " + bindingPath);
                        JToken arrayBindingContext = getBindingContext(bindingContext, bindingPath);
                        if ((arrayBindingContext != null) && (arrayBindingContext.Type == JTokenType.Array))
                        {
                            foreach (JObject arrayElementBindingContext in (JArray)arrayBindingContext)
                            {
                                onAddControl(createControl(arrayElementBindingContext, element));
                            }
                        }
                        controlCreated = true;
                    }
                    else if (binding["with"] != null)
                    {
                        string bindingPath = (string)binding["with"];
                        debug("Found 'with' binding with path: " + bindingPath);
                        controlBindingContext = getBindingContext(bindingContext, bindingPath);
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

            createControls(this.boundItems, (JArray)pageView["Elements"], control => panel.Children.Add(control));
        }

        void button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            ElementMetaData metaData = button.Tag as ElementMetaData;
            if ((metaData != null) && (metaData.Command != null))
            {
                debug("Button click with command: " + metaData.Command);
                this.stateManager.processCommand(metaData.Command);
            }
            else
            {
                debug("Button click with no action");
            }
        }

        public void processBoundItems2(JObject obj)
        {
            this.boundItems = obj;

            IList<string> keys = obj.Properties().Select(p => p.Name).ToList();
            foreach(string key in keys)
            {
                debug("Found key: " + key + " with value: " + obj[key] + " of type: " + obj[key].GetType());
            }
        }

        public void newViewItems(JObject boundItems)
        {
            this.boundItems = boundItems;
        }

        public void updatedViewItems(JObject boundItems)
        {
            // !!! This should be incremental updates to existing boundItems, involving deep value copying.
            //     - right now this just does top level properties (which will hammer over objects/lists at 
            //       lower levels that may be in use as binding contexts in the view).
            //
            IList<string> keys = boundItems.Properties().Select(p => p.Name).ToList();
            foreach (string key in keys)
            {
                debug("Found key: " + key + " with value: " + boundItems[key] + " of type: " + boundItems[key].GetType());
                this.boundItems[key] = boundItems[key];
            }
        }

        Regex braceContentsRE = new Regex(@"{([^}]*)}");

        string processTokenValue(JToken bindingContext, string tokenValue)
        {
            return braceContentsRE.Replace(tokenValue, delegate(Match m)
            {
                debug("Found binding: " + m.Groups[1]);
                // !!! Use getBindingContext logic here (to process binding path tokens)
                return (string)bindingContext.SelectToken(m.Groups[1].ToString());
            });
        }

        public void updateView()
        {
            Panel panel = this.Content;

            foreach (FrameworkElement control in panel.Children)
            {
                ElementMetaData metaData = control.Tag as ElementMetaData;
                if (metaData != null)
                {
                    if ((metaData.ValueBinding != null) && (metaData.BindingContext[metaData.ValueBinding.BoundValue] != null))
                    {
                        metaData.ValueBinding.SetValue((string)metaData.BindingContext[metaData.ValueBinding.BoundValue]);
                    }

                    foreach(PropertyBinding binding in metaData.PropertyBindings)
                    {
                        binding.SetValue(processTokenValue(metaData.BindingContext, binding.Content));
                    }
                }
            }
        }

        private void MessageDialogCommandHandler(IUICommand command)
        {
            debug("MessageBox Command invoked: " + command.Label);
            if (command.Id != null)
            {
                debug("MessageBox command: " + (string)command.Id);
                this.stateManager.processCommand((string)command.Id); 
            }
        }

        public async void processMessageBox(JObject messageBox)
        {
            string message = this.processTokenValue(this.boundItems, (string)messageBox["message"]);

            var messageDialog = new MessageDialog(message);

            if (messageBox["title"] != null)
            {
                messageDialog.Title = this.processTokenValue(this.boundItems, (string)messageBox["title"]);
            }

            if (messageBox["options"] != null)
            {
                JArray options = (JArray)messageBox["options"];
                foreach (JObject option in options)
                {
                    if ((string)option["command"] != null)
                    {
                        messageDialog.Commands.Add(new UICommand(
                            this.processTokenValue(this.boundItems, (string)option["label"]),
                            new UICommandInvokedHandler(this.MessageDialogCommandHandler),
                            this.processTokenValue(this.boundItems, (string)option["command"]))
                            );
                    }
                    else
                    {
                        messageDialog.Commands.Add(new UICommand(
                            this.processTokenValue(this.boundItems, (string)option["label"]),
                            new UICommandInvokedHandler(this.MessageDialogCommandHandler))
                            );
                    }
                }
            }

            await messageDialog.ShowAsync();
        }

        // !!! This method assumes string values - needs to work with all types
        //
        public void collectBoundItemValues(Action<string, string> setValue)
        {
            // !!! We should just be getting the changed values (and marking them as unchanged now)
            //
            Panel panel = this.Content;

            foreach (FrameworkElement control in panel.Children)
            {
                ElementMetaData metaData = control.Tag as ElementMetaData;
                if (metaData != null)
                {
                    if (metaData.ValueBinding != null)
                    {
                        String value = metaData.ValueBinding.GetValue();
                        debug("Collected bound item value for: " + metaData.ValueBinding.BoundValue + " of: " + value);
                        setValue(metaData.ValueBinding.BoundValue, value);
                    }
                }
            }
        }
    }
}
