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
        public Action<bool> setBackEnabled { get; set; }

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

        public void OnBackCommand()
        {
            Util.debug("Back button click with command: " + onBackCommand);
            _stateManager.processCommand(onBackCommand);
        }

        public void ProcessPageView(JObject pageView)
        {
            ClearContent();

            this.onBackCommand = (string)pageView["onBack"];
            this.setBackEnabled(this.onBackCommand != null);

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
                    new JProperty("type", "stackpanel"),
                    new JProperty("orientation", "vertical"),
                    new JProperty("contents", elements)
                );

                rootControlWrapper = CreateRootContainerControl(controlSpec);
            }

            SetContent(rootControlWrapper);
        }
    }
}
