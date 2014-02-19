﻿using MaaasCore;
using MaaasShared;
using MaasClient.Core;
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
    public sealed partial class BasicPage : MaaasClientWin.Common.LayoutAwarePage
    {
        static string _host = Util.getMaaasHost();

        StateManager _stateManager;
        WinPageView _pageView;

        public BasicPage()
        {
            this.InitializeComponent();

            WinDeviceMetrics deviceMetrics = new WinDeviceMetrics();

            this.mainScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            this.mainScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

            //Transport transport = new TransportHttp(_host + "/api");
            Transport transport = new TransportWs(_host);

            _stateManager = new StateManager(_host, transport, deviceMetrics);
            _pageView = new WinPageView(_stateManager, _stateManager.ViewModel, this, this.mainScroll);
            _stateManager.Path = "menu";

            this.Loaded += BasicPage_Loaded; 
            this.backButton.Click += backButton_Click;

            _pageView.setPageTitle = title => this.pageTitle.Text = title;
            _pageView.setBackEnabled = isEnabled => this.backButton.IsEnabled = isEnabled;

            _stateManager.SetProcessingHandlers(json => _pageView.ProcessPageView(json), json => _pageView.ProcessMessageBox(json));
        }

        async void BasicPage_Loaded(object sender, RoutedEventArgs e)
        {
            await _stateManager.loadLayout();
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
