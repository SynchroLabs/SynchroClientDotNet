﻿using MaaasCore;
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

        public MaaasPage()
        {
            this.InitializeComponent();
            this.backButton.Click += backButton_Click;
            DisplayInformation.GetForCurrentView().OrientationChanged += MaaasPage_OrientationChanged;
            // DisplayProperties.OrientationChanged += DisplayProperties_OrientationChanged;
        }

        private DisplayOrientations normalizeOrientation(DisplayOrientations orientation)
        {
            if (orientation == DisplayOrientations.LandscapeFlipped)
            {
                return DisplayOrientations.Landscape;
            }
            else if (orientation == DisplayOrientations.PortraitFlipped)
            {
                return DisplayOrientations.Portrait;
            }

            return orientation;
        }

        void MaaasPage_OrientationChanged(DisplayInformation sender, object args)
        {
            //The orientation of the device is now...
            var orientation = normalizeOrientation(DisplayInformation.GetForCurrentView().CurrentOrientation);
            orientation = normalizeOrientation(sender.CurrentOrientation);
            if (orientation == DisplayOrientations.Landscape)
            {
                // Landscape
                Util.debug("Screen oriented to Landscape");
                _stateManager.processViewUpdate(MaaasOrientation.Landscape);
            }
            else
            {
                // Portait
                Util.debug("Screen oriented to Portrait");
                _stateManager.processViewUpdate(MaaasOrientation.Portrait);
            }
        }

        /*
        void DisplayProperties_OrientationChanged(object sender)
        {
            //The orientation of the device is now...
            var orientation = DisplayInformation.GetForCurrentView().CurrentOrientation;
            if (orientation == DisplayOrientations.Landscape)
            {
                // Landscape
                Util.debug("Screen oriented to Landscape");
                _stateManager.processViewUpdate(MaaasOrientation.Landscape);
            }
            else
            {
                // Portait
                Util.debug("Screen oriented to Portrait");
                _stateManager.processViewUpdate(MaaasOrientation.Portrait);
            }
        }
         */

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
        protected override async void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            string endpoint = navigationParameter as string;

            Util.debug("Launching app at endpoint: " + endpoint);

            WinAppManager appManager = new WinAppManager();
            await appManager.loadState();

            MaaasApp app = appManager.GetApp(endpoint);

            WinDeviceMetrics deviceMetrics = new WinDeviceMetrics();

            Transport transport = new TransportHttp(endpoint);
            //Transport transport = new TransportWs(endpoint);

            _stateManager = new StateManager(appManager, app, transport, deviceMetrics);
            _pageView = new WinPageView(_stateManager, _stateManager.ViewModel, this, this.mainScroll);

            _pageView.setPageTitle = title => this.pageTitle.Text = title;
            _pageView.setBackEnabled = isEnabled => this.backButton.IsEnabled = isEnabled;

            _stateManager.SetProcessingHandlers(_pageView.ProcessPageView, _pageView.ProcessMessageBox);

            await _stateManager.startApplication();
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
