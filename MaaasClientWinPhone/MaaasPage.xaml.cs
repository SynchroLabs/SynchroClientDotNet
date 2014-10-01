﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using MaaasClientWinPhone.Resources;
using MaaasCore;
using MaaasShared;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Microsoft.Phone.Info;

namespace MaaasClientWinPhone
{
    public partial class MaaasPage : PhoneApplicationPage
    {
        StateManager _stateManager;
        PageView _pageView;

        // Constructor
        public MaaasPage()
        {
            InitializeComponent();
            
            // http://msdn.microsoft.com/en-us/library/windowsphone/develop/ff769552(v=vs.105).aspx
            Thickness overhang = (Thickness)Application.Current.Resources["PhoneTouchTargetOverhang"];
            Util.debug("Overhang: " + overhang);

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
            
            OrientationChanged += MaaasPage_OrientationChanged;
        }

        public MaaasOrientation CurrentOrientation
        {
            get
            {
                if ((Orientation == Microsoft.Phone.Controls.PageOrientation.Landscape) ||
                    (Orientation == Microsoft.Phone.Controls.PageOrientation.LandscapeLeft) ||
                    (Orientation == Microsoft.Phone.Controls.PageOrientation.LandscapeRight))
                {
                    return MaaasOrientation.Landscape;
                }

                return MaaasOrientation.Portrait;
            }
        }

        void MaaasPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            if ((e.Orientation & PageOrientation.Portrait) == (PageOrientation.Portrait))
            {
                Util.debug("Screen oriented to Portrait");
                _stateManager.processViewUpdate(MaaasOrientation.Portrait);
            }
            else
            {
                Util.debug("Screen oriented to Landscape");
                _stateManager.processViewUpdate(MaaasOrientation.Landscape);
            }
        }

        // Static helper to allow callers to navigate "here" with appropriate parameters...
        //
        public static void NavigateTo(string endpoint)
        {
            App.RootFrame.Navigate(new Uri(string.Format("/MaaasPage.xaml?endpoint={0}", Uri.EscapeUriString(endpoint)), UriKind.Relative));
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string endpoint = NavigationContext.QueryString["endpoint"];   

            WinPhoneDeviceMetrics deviceMetrics = new WinPhoneDeviceMetrics(this);

            WinPhoneAppManager appManager = new WinPhoneAppManager();
            await appManager.loadState();

            MaaasApp app = appManager.GetApp(endpoint);

            _stateManager = new StateManager(appManager, app, new TransportHttp(endpoint), deviceMetrics);
            _pageView = new WinPhonePageView(_stateManager, _stateManager.ViewModel, this, this.mainScroll);

            this.BackKeyPress += MainPage_BackKeyPress;

            _pageView.setPageTitle = title => this.pageTitle.Text = title;

            _stateManager.SetProcessingHandlers(_pageView.ProcessPageView,_pageView.ProcessMessageBox);

            await _stateManager.startApplication();
        }

        void MainPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _pageView.OnBackCommand();
            e.Cancel = true;
        }

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}