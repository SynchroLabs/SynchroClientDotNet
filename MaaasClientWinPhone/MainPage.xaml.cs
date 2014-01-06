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
    public partial class MainPage : PhoneApplicationPage
    {
        static string _host = "192.168.1.109:1337"; // "localhost:1337";
        //static string _host = "maaas.azurewebsites.net";

        StateManager _stateManager;
        PageView _pageView;

        bool _isAppBackEnabled = false;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            
            WinPhoneDeviceMetrics deviceMetrics = new WinPhoneDeviceMetrics();

            _stateManager = new StateManager(_host, new TransportHttp(_host + "/api"), deviceMetrics);
            _pageView = new WinPhonePageView(_stateManager, _stateManager.ViewModel, (Panel)this.mainStack);

            this.BackKeyPress += MainPage_BackKeyPress;
            this.Loaded += MainPage_Loaded;

            _stateManager.Path = "menu";

            _pageView.setPageTitle = title => this.pageTitle.Text = title;
            _pageView.setBackEnabled = isEnabled => _isAppBackEnabled = isEnabled;

            _stateManager.SetProcessingHandlers(json => _pageView.ProcessPageView(json), json => _pageView.ProcessMessageBox(json));

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await _stateManager.loadLayout();
        }

        void MainPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isAppBackEnabled)
            {
                _pageView.OnBackCommand();
                e.Cancel = true;
            }
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