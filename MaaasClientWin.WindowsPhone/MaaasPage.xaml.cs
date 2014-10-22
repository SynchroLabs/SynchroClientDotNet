using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using MaaasCore;
using MaaasShared;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using MaaasClientWin.Common;
using Windows.Phone.UI.Input;
using System.Threading.Tasks;

namespace MaaasClientWin
{
    public partial class MaaasPage : BasicPage
    {
        static Logger logger = Logger.GetLogger("MaaasPage");

        StateManager _stateManager;
        PageView _pageView;

        public MaaasPage()
        {
            this.InitializeComponent();
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

        protected override async void LoadState(LoadStateEventArgs args)
        {
            string endpoint = args.NavigationParameter as string;

            logger.Info("Launching app at endpoint: {0}", endpoint);

            WinAppManager appManager = new WinAppManager();
            await appManager.loadState();

            MaaasApp app = appManager.GetApp(endpoint);

            WinPhoneDeviceMetrics deviceMetrics = new WinPhoneDeviceMetrics();

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
            // Note: No on screen back button to enable/disable via _pageView.setBackEnabled on Windows Phone

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

        public override void OnHardwareBackPressed(object sender, BackPressedEventArgs e) 
        {
            logger.Info("Back button pressed");
            e.Handled = true;
            _pageView.OnBackCommand();
        }
    }
}