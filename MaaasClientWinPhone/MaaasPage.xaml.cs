using System;
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
        public static MaaasApp MaaasApp { get; set; }

        StateManager _stateManager;
        PageView _pageView;

        // Constructor
        public MaaasPage()
        {
            InitializeComponent();
            
            WinPhoneDeviceMetrics deviceMetrics = new WinPhoneDeviceMetrics();

            _stateManager = new StateManager(MaaasApp.Endpoint, new TransportHttp(MaaasApp.Endpoint), deviceMetrics);
            _pageView = new WinPhonePageView(_stateManager, _stateManager.ViewModel, this, this.mainScroll);

            this.BackKeyPress += MainPage_BackKeyPress;
            this.Loaded += MainPage_Loaded;

            _pageView.setPageTitle = title => this.pageTitle.Text = title;

            _stateManager.SetProcessingHandlers(json => _pageView.ProcessPageView(json), json => _pageView.ProcessMessageBox(json));

            // http://msdn.microsoft.com/en-us/library/windowsphone/develop/ff769552(v=vs.105).aspx
            Thickness overhang = (Thickness)Application.Current.Resources["PhoneTouchTargetOverhang"];
            Util.debug("Overhang: " + overhang);

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
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