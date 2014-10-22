using MaaasClientWin.Common;
using MaaasCore;
using MaaasShared;
using MaasClient.Core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

namespace MaaasClientWin
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class MaaasPage : BasicPage
    {
        static Logger logger = Logger.GetLogger("MaaasPage");

        StateManager _stateManager;
        WinPageView _pageView;

        public MaaasPage()
        {
            this.InitializeComponent();
            this.backButton.Click += backButton_Click;
            DisplayInformation.GetForCurrentView().OrientationChanged += MaaasPage_OrientationChanged;
        }



        void MaaasPage_OrientationChanged(DisplayInformation sender, object args)
        {
            //The orientation of the device is now...
            // var orientation = this.normalizeOrientation(DisplayInformation.GetForCurrentView().CurrentOrientation);
            var orientation = this.normalizeOrientation(sender.CurrentOrientation);
            if (orientation == DisplayOrientations.Landscape)
            {
                // Landscape
                logger.Debug("Screen oriented to Landscape");
                Task t = _stateManager.processViewUpdate(MaaasOrientation.Landscape);
            }
            else
            {
                // Portait
                logger.Debug("Screen oriented to Portrait");
                Task t = _stateManager.processViewUpdate(MaaasOrientation.Portrait);
            }
        }

        void backButton_Click(object sender, RoutedEventArgs e)
        {
            _pageView.OnBackCommand();
        }

        protected override async void LoadState(LoadStateEventArgs args)
        {
            string endpoint = args.NavigationParameter as string;

            logger.Info("Launching app at endpoint: {0}", endpoint);

            WinAppManager appManager = new WinAppManager();
            await appManager.loadState();

            MaaasApp app = appManager.GetApp(endpoint);

            WinDeviceMetrics deviceMetrics = new WinDeviceMetrics();

            Transport transport = new TransportHttp(endpoint);
            //Transport transport = new TransportWs(endpoint);

            Action backToMenu = null;
            if (appManager.AppSeed == null)
            {
                // If we are't nailed to a predefined app, then we'll allow the app to navigate back to
                // this page from its top level page.
                //
                backToMenu = new Action(delegate()
                {
                    this.Frame.GoBack();
                });
            }

            _stateManager = new StateManager(appManager, app, transport, deviceMetrics);
            _pageView = new WinPageView(_stateManager, _stateManager.ViewModel, this, this.mainScroll, backToMenu);

            _pageView.setPageTitle = title => this.pageTitle.Text = title;
            _pageView.setBackEnabled = isEnabled => this.backButton.IsEnabled = isEnabled;

            _stateManager.SetProcessingHandlers(_pageView.ProcessPageView, _pageView.ProcessMessageBox);

            logger.Debug("Connecting orientation change listener");
            DisplayInformation.GetForCurrentView().OrientationChanged += MaaasPage_OrientationChanged;

            await _stateManager.startApplication();
        }

        protected override void SaveState(SaveStateEventArgs args)
        {
            logger.Debug("Disconnecting orientation change listener");
            DisplayInformation.GetForCurrentView().OrientationChanged -= MaaasPage_OrientationChanged;
        }
    }
}
