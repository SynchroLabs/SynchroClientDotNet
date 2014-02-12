using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaaasCore
{
    public abstract class PageView
    {
        public Action<string> setPageTitle { get; set; }
        public Action<bool> setBackEnabled { get; set; } // Optional - set if you care about back enablement

        protected StateManager _stateManager;
        protected ViewModel _viewModel;

        protected string onBackCommand = null;

        public PageView(StateManager stateManager, ViewModel viewModel)
        {
            _stateManager = stateManager;
            _viewModel = viewModel;
        }

        public abstract ControlWrapper CreateRootContainerControl(JObject controlSpec);
        public abstract void ClearContent();
        public abstract void SetContent(ControlWrapper content);

        public abstract void ProcessMessageBox(JObject messageBox);

        public bool HasBackCommand { get { return onBackCommand != null; } }

        public bool OnBackCommand()
        {
            if (onBackCommand != null)
            {
                Util.debug("Back button click with command: " + onBackCommand);
                _stateManager.processCommand(onBackCommand);
                return true;
            }
            else
            {
                Util.debug("OnBackCommand called with no back command, ignoring");
                return false; // Not handled
            }
        }

        public void ProcessPageView(JObject pageView)
        {
            ClearContent();

            this.onBackCommand = (string)pageView["onBack"];
            if (this.setBackEnabled != null)
            {
                this.setBackEnabled(this.onBackCommand != null);
            }

            string pageTitle = (string)pageView["title"];
            if (pageTitle != null)
            {
                setPageTitle(pageTitle);
            }

            ControlWrapper rootControlWrapper = null;

            JArray elements = (JArray)pageView["elements"];
            if (elements.Count == 1)
            {
                // The only element is the container of all page elements, so make it the root element, and populate it...
                //
                rootControlWrapper = CreateRootContainerControl((JObject)elements[0]);
            }
            else if (elements.Count > 1)
            {
                // There is a collection of page elements, create a default container (vertical stackpanel), make it the root, and populate it...
                //
                JObject controlSpec = new JObject(
                    new JProperty("control", "stackpanel"),
                    new JProperty("orientation", "vertical"),
                    new JProperty("contents", elements)
                );

                rootControlWrapper = CreateRootContainerControl(controlSpec);
            }

            SetContent(rootControlWrapper);
        }
    }
}
