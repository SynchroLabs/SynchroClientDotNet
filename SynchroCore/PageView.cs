using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynchroCore
{
    public abstract class PageView
    {
        static Logger logger = Logger.GetLogger("PageView");

        public Action<string> setPageTitle { get; set; }
        public Action<bool> setBackEnabled { get; set; } // Optional - set if you care about back enablement

        protected StateManager _stateManager;
        protected ViewModel _viewModel;
        protected Action _doBackToMenu;

        // This is the top level container of controls for a page.  If the page specifies a single top level
        // element, then this represents that element.  If not, then this is a container control that we 
        // created to wrap those elements (currently a vertical stackpanel).
        //
        // Derived classes have a similarly named _rootControlWrapper which represents the actual topmost
        // visual element, typically a scroll container, that is re-populated as page contents change, and
        // which has a single child, the _rootContainerControlWrapper (which will change as the active page
        // changes).
        //
        protected ControlWrapper _rootContainerControlWrapper;

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
        public abstract void ProcessLaunchUrl(string primaryUrl, string secondaryUrl);

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

        public async Task<bool> GoBack()
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
                if (_rootContainerControlWrapper != null)
                {
                    _rootContainerControlWrapper.Unregister();
                }
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
            if (_rootContainerControlWrapper != null)
            {
                _rootContainerControlWrapper.Unregister();
                ClearContent();
                _rootContainerControlWrapper = null;
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
                _rootContainerControlWrapper = CreateRootContainerControl((JObject)elements[0]);
            }
            else if (elements.Count > 1)
            {
                // There is a collection of page elements, create a default container (vertical stackpanel), make it the root, and populate it...
                //
                JObject controlSpec = new JObject()
                {
                    { "control", new JValue("stackpanel") },
                    { "orientation", new JValue("vertical") },
                    { "contents", elements }
                };

                _rootContainerControlWrapper = CreateRootContainerControl(controlSpec);
            }

            SetContent(_rootContainerControlWrapper);
        }
    }
}
