using MaaasClientWin.Common;
using SynchroCore;
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

            this.SizeChanged += MaaasPage_SizeChanged;
        }

        private void MaaasPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // !!! Update PageMetrics
            logger.Info("Window size changed: {0}", e.NewSize);
        }

        async void MaaasPage_OrientationChanged(DisplayInformation sender, object args)
        {
            //The orientation of the device is now...
            // var orientation = this.normalizeOrientation(DisplayInformation.GetForCurrentView().CurrentOrientation);
            var orientation = this.normalizeOrientation(sender.CurrentOrientation);
            if (orientation == DisplayOrientations.Landscape)
            {
                // Landscape
                logger.Debug("Screen oriented to Landscape");
                await _stateManager.sendViewUpdateAsync(MaaasOrientation.Landscape);
            }
            else
            {
                // Portait
                logger.Debug("Screen oriented to Portrait");
                await _stateManager.sendViewUpdateAsync(MaaasOrientation.Portrait);
            }
        }

        async void backButton_Click(object sender, RoutedEventArgs e)
        {
            await _pageView.GoBack();
        }

        protected override async void LoadState(LoadStateEventArgs args)
        {
            string endpoint = args.NavigationParameter as string;

            logger.Info("Launching app at endpoint: {0}", endpoint);

            WinAppManager appManager = new WinAppManager();
            await appManager.loadState();

            MaaasApp app = appManager.GetApp(endpoint);

            WinDeviceMetrics deviceMetrics = new WinDeviceMetrics();

            Transport transport = new TransportHttp(TransportHttp.UriFromHostString(endpoint));
            //Transport transport = new TransportWs(endpoint);

            bool launchedFromMenu = (appManager.AppSeed == null);

            ProcessAppExit appExit = () =>
            {
                this.Frame.GoBack();
            };

            _stateManager = new StateManager(appManager, app, transport, deviceMetrics);
            _pageView = new WinPageView(_stateManager, _stateManager.ViewModel, this, this.mainScroll, launchedFromMenu);

            _pageView.setPageTitle = title => this.pageTitle.Text = title;
            _pageView.setBackEnabled = isEnabled => this.backButton.IsEnabled = isEnabled;

            _stateManager.SetProcessingHandlers(_pageView.ProcessPageView, appExit, _pageView.ProcessMessageBox, _pageView.ProcessLaunchUrl);

            logger.Debug("Connecting orientation change listener");
            DisplayInformation.GetForCurrentView().OrientationChanged += MaaasPage_OrientationChanged;

            await _stateManager.startApplicationAsync();
        }

        protected override void SaveState(SaveStateEventArgs args)
        {
            logger.Debug("Disconnecting orientation change listener");
            DisplayInformation.GetForCurrentView().OrientationChanged -= MaaasPage_OrientationChanged;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            // !!! Shut down _stateManager
            //
            logger.Info("Navigating away from Synchro app");
            base.OnNavigatedFrom(e);
        }
    }
}
