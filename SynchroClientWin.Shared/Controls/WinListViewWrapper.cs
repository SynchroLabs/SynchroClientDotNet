using SynchroCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MaaasClientWin.Controls
{
    class WinListViewWrapper : WinControlWrapper
    {
        static Logger logger = Logger.GetLogger("WinListViewWrapper");

        bool _selectionChangingProgramatically = false;
        JToken _localSelection;

        static string[] Commands = new string[] { CommandName.OnItemClick.Attribute, CommandName.OnSelectionChange.Attribute };
        
        private JToken getContents(JObject controlSpec, String attribute)
        {
            var contents = controlSpec[attribute];
            if (contents.Type == JTokenType.Array)
            {
                var contentsArray = (JArray)contents;
                contents = (contentsArray.Count > 0) ? contentsArray[0] : null;
            }
            return contents;
        }

        public WinListViewWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext, controlSpec)
        {
            logger.Debug("Creating listview element");
            ListView listView = new ListView();
            listView.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            this._control = listView;

            applyFrameworkElementDefaults(listView);

            ListSelectionMode mode = ToListSelectionMode(processElementProperty(controlSpec, "select", null));
            switch (mode)
            {
                case ListSelectionMode.None:
                    listView.SelectionMode = ListViewSelectionMode.None;
                    break;
                case ListSelectionMode.Single:
                    listView.SelectionMode = ListViewSelectionMode.Single;
                    break;
                case ListSelectionMode.Multiple:
                    listView.SelectionMode = ListViewSelectionMode.Multiple;
                    break;
            }

            if (controlSpec["header"] != null)
            {
                createControls(new JArray(){ getContents(controlSpec, "header") }, (childControlSpec, childControlWrapper) =>
                {
                    listView.Header = childControlWrapper.Control;
                });
            }

            if (controlSpec["footer"] != null)
            {
                // On Windows there is what appears to be a bug when a list view with a footer "grows".  The new items
                // show up instantly (without animation), except for the items behind where the footer used to be, which
                // animate in.  This is pretty ugly.  Removing the item container transition solves this.  It also means
                // that the list doesn't animate in when initially set, which is kind of a bummer.  It would be nice if
                // we could somehow schedule turning off the transitions until after the initial list fill.
                //
                listView.ItemContainerTransitions = new Windows.UI.Xaml.Media.Animation.TransitionCollection();

                createControls(new JArray(){ getContents(controlSpec, "footer") }, (childControlSpec, childControlWrapper) =>
                {
                    listView.Footer = childControlWrapper.Control;
                });
            }

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "items", Commands);
            ProcessCommands(bindingSpec, Commands);

            if (bindingSpec["items"] != null)
            {
                // To make ListView compatible with ListBox confi
                var itemContent = (string)bindingSpec["itemContent"] ?? "{$data}"; // Only used in case where itemTemplate not provided
                var itemTemplate = 
                    (JObject)getContents(controlSpec, "itemTemplate") ?? 
                    new JObject() { { "control", new JValue("text") }, { "value", new JValue(itemContent) }, { "margin", new JValue(DEFAULT_MARGIN) } };

                processElementBoundValue(
                    "items",
                    (string)bindingSpec["items"],
                    () => getListViewContents(listView),
                    value => this.setListViewContents(listView, itemTemplate, GetValueBinding("items").BindingContext));
            }

            if (bindingSpec["selection"] != null)
            {
                string selectionItem = (string)bindingSpec["selectionItem"] ?? "$data";

                processElementBoundValue(
                    "selection",
                    (string)bindingSpec["selection"],
                    () => getListViewSelection(listView, selectionItem),
                    value => this.setListViewSelection(listView, selectionItem, (JToken)value));
            }

            if (listView.SelectionMode != ListViewSelectionMode.None)
            {
                listView.SelectionChanged += listView_SelectionChanged;
            }
            else
            {
                listView.IsItemClickEnabled = true;
                listView.ItemClick += listView_ItemClick;
            }
        }

        public JToken getListViewContents(ListView listbox)
        {
            logger.Debug("Get listview contents - NOOP");
            throw new NotImplementedException();
        }

        public void setListViewContents(ListView listview, JObject itemTemplate, BindingContext bindingContext)
        {
            logger.Debug("Setting listview contents");

            List<BindingContext> itemContexts = bindingContext.SelectEach("$data");

            if (listview.Items.Count < itemContexts.Count)
            {
                // New items are added (to the end of the list)
                //
                for (int index = listview.Items.Count; index < itemContexts.Count; index++)
                {
                    WinControlWrapper controlWrapper = CreateControl(this, itemContexts[index], itemTemplate);
                    listview.Items.Add(controlWrapper.Control);
                }
            }
            else if (listview.Items.Count > itemContexts.Count)
            {
                // Items need to be removed (from the end of the list)
                //
                for (int index = listview.Items.Count; index > itemContexts.Count; index--)
                {
                    FrameworkElement control = (FrameworkElement)listview.Items[index - 1];
                    ControlWrapper wrapper = this.getChildControlWrapper(control);

                    // Unregister any bindings for this element or any descendants
                    //
                    wrapper.Unregister();

                    listview.Items.RemoveAt(index - 1);
                }
            }

            ValueBinding selectionBinding = GetValueBinding("selection");
            if (selectionBinding != null)
            {
                // If there is a "selection" value binding, then we update the selection state from that after filling the list.
                //
                selectionBinding.UpdateViewFromViewModel();
            }
            else if (_localSelection != null)
            {
                // If there is not a "selection" value binding, then we use local selection state to restore the selection when
                // re-filling the list.
                //
                this.setListViewSelection(listview, "$data", _localSelection);
            }
        }

        public JToken getListViewSelection(ListView listview, string selectionItem)
        {
            if (listview.SelectionMode == ListViewSelectionMode.Multiple)
            {
                JArray array = new JArray();
                foreach (FrameworkElement control in listview.SelectedItems)
                {
                    array.Add(this.getChildControlWrapper(control).BindingContext.Select(selectionItem).GetValue().DeepClone());
                }
                return array;
            }
            else if (listview.SelectionMode == ListViewSelectionMode.Single)
            {
                FrameworkElement control = (FrameworkElement)listview.SelectedItem;
                if (control != null)
                {
                    // We need to clone the item so we don't destroy the original link to the item in the list (since the
                    // item we're getting in SelectedItem is the list item and we're putting it into the selection binding).
                    //     
                    return this.getChildControlWrapper(control).BindingContext.Select(selectionItem).GetValue().DeepClone();
                }
                return new JValue(false); // This is a "null" selection
            }

            return null;
        }

        public void setListViewSelection(ListView listview, string selectionItem, JToken selection)
        {
            _selectionChangingProgramatically = true;

            if (listview.SelectionMode == ListViewSelectionMode.Multiple)
            {
                listview.SelectedItems.Clear();

                foreach (FrameworkElement control in listview.Items)
                {
                    if (selection is JArray)
                    {
                        JArray array = selection as JArray;
                        foreach (JToken item in array)
                        {
                            if (JToken.DeepEquals(item, this.getChildControlWrapper(control).BindingContext.Select(selectionItem).GetValue()))
                            {
                                listview.SelectedItems.Add(control);
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (JToken.DeepEquals(selection, this.getChildControlWrapper(control).BindingContext.Select(selectionItem).GetValue()))
                        {
                            listview.SelectedItems.Add(control);
                        }
                    }
                }
            }
            else if (listview.SelectionMode == ListViewSelectionMode.Single)
            {
                listview.SelectedItem = null;

                foreach (FrameworkElement control in listview.Items)
                {
                    if (JToken.DeepEquals(selection, this.getChildControlWrapper(control).BindingContext.Select(selectionItem).GetValue()))
                    {
                        listview.SelectedItem = control;
                        break;
                    }
                }
            }

            _selectionChangingProgramatically = false;
        }

        async void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            logger.Debug("Listbox selection changed");
            ListView listview = (ListView)sender;

            ValueBinding selectionBinding = GetValueBinding("selection");
            if (selectionBinding != null)
            {
                updateValueBindingForAttribute("selection");
            }
            else if (!_selectionChangingProgramatically)
            {
                _localSelection = this.getListViewSelection(listview, "$data");
            }

            if (!_selectionChangingProgramatically)
            {
                logger.Debug("Listview selection changed by user!");
                CommandInstance command = GetCommand(CommandName.OnSelectionChange);
                if (command != null)
                {
                    logger.Debug("ListView selection changed with command: {0}", command);

                    if (listview.SelectionMode == ListViewSelectionMode.Single)
                    {
                        // For selection mode "Single", the command handler resolves its tokens relative to the item selected.
                        //
                        // There should always be a first "added" item, which represents the current selection (in single select).
                        //
                        if ((e.AddedItems != null) && (e.AddedItems.Count > 0))
                        {
                            ControlWrapper wrapper = this.getChildControlWrapper((FrameworkElement)e.AddedItems[0]);
                            if (wrapper != null)
                            {
                                await StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(wrapper.BindingContext));
                            }
                        }
                    }
                    else if (listview.SelectionMode == ListViewSelectionMode.Multiple)
                    {
                        // For selection mode "Multiple", the command hander resovles its tokens relative to the listview, not any list item(s).
                        //
                        await StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(this.BindingContext));
                    }
                }
            }
        }

        async void listView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // This will get called when the selection mode is "None" and an item is clicked (no selection change events will
            // fire in this case).
            //
            CommandInstance command = GetCommand(CommandName.OnItemClick);
            if (command != null)
            {
                logger.Debug("ListView item click with command: {0}", command);

                // The item click command handler resolves its tokens relative to the item clicked (not the list view).
                //
                ControlWrapper wrapper = this.getChildControlWrapper((FrameworkElement)e.ClickedItem);
                if (wrapper != null)
                {
                    await StateManager.sendCommandRequestAsync(command.Command, command.GetResolvedParameters(wrapper.BindingContext));
                }
            }
        }
    }
}
