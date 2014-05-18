using MaaasCore;
using MaaasShared;
using MaasClient.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace MaaasClientWin
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class MaaasPage : MaaasClientWin.Common.LayoutAwarePage
    {
        StateManager _stateManager;
        WinPageView _pageView;
        MaaasApp _maaasApp;

        public MaaasPage()
        {
            this.InitializeComponent();

            this.Loaded += MaaasPage_Loaded; 
            this.backButton.Click += backButton_Click;
        }

        async void MaaasPage_Loaded(object sender, RoutedEventArgs e)
        {
            // !!! This is just to test that we can do a standalone transport operation to get the AppDefinition
            //
            /*
            Transport tempTransport = new TransportHttp(_maaasApp.Endpoint);
            JObject appDefinition = await tempTransport.getAppDefinition();
            Util.debug("XXXX Got app definition for: " + appDefinition["name"] + " - " + appDefinition["description"]);
            */

            await _stateManager.startApplication();
        }

        void backButton_Click(object sender, RoutedEventArgs e)
        {
            _pageView.OnBackCommand();
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            _maaasApp = navigationParameter as MaaasApp;

            Util.debug("Launching app at endpoint: " + _maaasApp.Endpoint);

            WinDeviceMetrics deviceMetrics = new WinDeviceMetrics();

            Transport transport = new TransportHttp(_maaasApp.Endpoint);
            //Transport transport = new TransportWs(_maaasApp.Endpoint);

            _stateManager = new StateManager(_maaasApp.Endpoint, transport, deviceMetrics);
            _pageView = new WinPageView(_stateManager, _stateManager.ViewModel, this, this.mainScroll);

            _pageView.setPageTitle = title => this.pageTitle.Text = title;
            _pageView.setBackEnabled = isEnabled => this.backButton.IsEnabled = isEnabled;

            _stateManager.SetProcessingHandlers(json => _pageView.ProcessPageView(json), json => _pageView.ProcessMessageBox(json));
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
        }
    }
}
