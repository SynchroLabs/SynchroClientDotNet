﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using SynchroCore;
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

            this.SizeChanged += MaaasPage_SizeChanged;
        }

        private void MaaasPage_SizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
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

        protected override async void LoadState(LoadStateEventArgs args)
        {
            string endpoint = args.NavigationParameter as string;

            logger.Info("Launching app at endpoint: {0}", endpoint);

            WinAppManager appManager = new WinAppManager();
            await appManager.loadState();

            MaaasApp app = appManager.GetApp(endpoint);

            WinPhoneDeviceMetrics deviceMetrics = new WinPhoneDeviceMetrics();

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
            // Note: No on screen back button to enable/disable via _pageView.setBackEnabled on Windows Phone

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
            logger.Info("Navigating away from Synchro app");
            base.OnNavigatedFrom(e);
        }

        public override async void OnHardwareBackPressed(object sender, BackPressedEventArgs e) 
        {
            logger.Info("Back button pressed");
            e.Handled = true;
            await _pageView.GoBack();
        }
    }
}