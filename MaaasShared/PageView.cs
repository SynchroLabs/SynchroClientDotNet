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
        static Logger logger = Logger.GetLogger("PageView");

        public Action<string> setPageTitle { get; set; }
        public Action<bool> setBackEnabled { get; set; } // Optional - set if you care about back enablement

        protected StateManager _stateManager;
        protected ViewModel _viewModel;
        protected Action _doBackToMenu;

        protected ControlWrapper _rootControlWrapper;

        protected string onBackCommand = null;

        public PageView(StateManager stateManager, ViewModel viewModel, Action doBackToMenu)
        {
            _stateManager = stateManager;
            _viewModel = viewModel;
            _doBackToMenu = doBackToMenu;
        }

        public abstract ControlWrapper CreateRootContainerControl(JObject controlSpec);
        public abstract void ClearContent();
        public abstract void SetContent(ControlWrapper content);

        public abstract void ProcessMessageBox(JObject messageBox, CommandHandler onCommand);

        public bool HasBackCommand 
        { 
            get 
            {
                if (this._stateManager.IsBackSupported())
                {
                    // Page-specified back command...
                    //
                    return true;
                }
                else if ((_doBackToMenu != null) && _stateManager.IsOnMainPath())
                {
                    // No page-specified back command, launched from menu, and is main (top-level) page...
                    //
                    return true;
                }

                return false; 
            } 
        }

        public async Task<bool> OnBackCommand()
        {
            if (_stateManager.IsBackSupported())
            {
                logger.Debug("Back navigation");
                await _stateManager.sendBackRequestAsync();
                return true;
            }
            else if ((_doBackToMenu != null) && _stateManager.IsOnMainPath())
            {
                logger.Debug("Back navigation - returning to menu");
                _doBackToMenu();
                return true;
            }
            else
            {
                logger.Warn("OnBackCommand called with no back command, ignoring");
                return false; // Not handled
            }
        }

        public void ProcessPageView(JObject pageView)
        {
            if (_rootControlWrapper != null)
            {
                _rootControlWrapper.Unregister();
                ClearContent();
                _rootControlWrapper = null;
            }

            if (this.setBackEnabled != null)
            {
                this.setBackEnabled(this.HasBackCommand);
            }

            string pageTitle = (string)pageView["title"];
            if (pageTitle != null)
            {
                setPageTitle(pageTitle);
            }

            JArray elements = (JArray)pageView["elements"];
            if (elements.Count == 1)
            {
                // The only element is the container of all page elements, so make it the root element, and populate it...
                //
                _rootControlWrapper = CreateRootContainerControl((JObject)elements[0]);
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

                _rootControlWrapper = CreateRootContainerControl(controlSpec);
            }

            SetContent(_rootControlWrapper);
        }
    }
}
