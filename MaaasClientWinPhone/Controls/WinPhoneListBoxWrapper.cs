using MaaasCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MaaasClientWinPhone.Controls
{
    class WinPhoneListBoxWrapper : WinPhoneControlWrapper
    {
        public WinPhoneListBoxWrapper(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec) :
            base(parent, bindingContext)
        {
            Util.debug("Creating listbox element");
            ListBox listbox = new ListBox();
            this._control = listbox;

            applyFrameworkElementDefaults(listbox);

            JObject bindingSpec = BindingHelper.GetCanonicalBindingSpec(controlSpec, "items");
            if (bindingSpec != null)
            {
                if (bindingSpec["items"] != null)
                {
                    processElementBoundValue("items", (string)bindingSpec["items"], () => getListboxContents(listbox), value => this.setListboxContents(listbox, (JToken)value));
                }
                if (bindingSpec["selection"] != null)
                {
                    processElementBoundValue("selection", (string)bindingSpec["selection"], () => getListboxSelection(listbox), value => this.setListboxSelection(listbox, (JToken)value));
                }
            }
            // Get selection mode - single (default) or multiple - no dynamic values (we don't need this changing during execution).
            if ((controlSpec["select"] != null) && ((string)controlSpec["select"] == "Multiple"))
            {
                listbox.SelectionMode = SelectionMode.Multiple;
            }
            listbox.SelectionChanged += listbox_SelectionChanged;
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
                            // modifying SelectedItems in single select, you get a "catastrophic" freakout.
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
                        listbox.SelectedItems.Add(ToString(item));
                    }
                }
                else
                {
                    listbox.SelectedItem = ToString(selection);
                }
            }
            else
            {
                listbox.SelectedItem = ToString(selection);
            }
        }

        void listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            updateValueBindingForAttribute("selection");
        }
    }
}

