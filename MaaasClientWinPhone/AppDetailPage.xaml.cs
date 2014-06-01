using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using MaaasCore;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MaaasShared;
using Newtonsoft.Json.Linq;

namespace MaaasClientWinPhone
{
    // This is a simple customer class that  
    // implements the IPropertyChange interface. 
    public class AppPageViewModel : INotifyPropertyChanged
    {
        private MaaasApp _app;

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property. 
        // The CallerMemberName attribute that is applied to the optional propertyName 
        // parameter causes the property name of the caller to be substituted as an argument. 
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public AppPageViewModel()
        {
        }

        public MaaasApp MaaasApp
        {
            get
            {
                return this._app;
            }

            set
            {
                if (value != this._app)
                {
                    this._app = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }

    public partial class AppDetailPage : PhoneApplicationPage
    {
        private MaaasAppManager appManager = new WinPhoneAppManager();

        private AppPageViewModel _vm = new AppPageViewModel();
        public AppPageViewModel AppPageViewModel { get { return _vm; } }

        public AppDetailPage()
        {
            InitializeComponent();

            this.PanelSearch.Visibility = Visibility.Collapsed;
            this.PanelDetails.Visibility = Visibility.Collapsed;

            this.BtnFind.Click += BtnFind_Click;
            this.BtnDelete.Click += BtnDelete_Click;
            this.BtnLaunch.Click += BtnLaunch_Click;
            this.BtnSave.Click += BtnSave_Click;
        }

        // Static helper to allow callers to navigate "here" with appropriate parameters...
        //
        public static void NavigateTo(string endpoint = null)
        {
            if (endpoint != null)
            {
                App.RootFrame.Navigate(new Uri(string.Format("/AppDetailPage.xaml?endpoint={0}", Uri.EscapeUriString(endpoint)), UriKind.Relative));
            }
            else
            {
                App.RootFrame.Navigate(new Uri("/AppDetailPage.xaml", UriKind.Relative));
            }
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await appManager.loadState();

            string endpoint = null;
            if (NavigationContext.QueryString.ContainsKey("endpoint"))
            {
                endpoint = NavigationContext.QueryString["endpoint"];
            }
            if (endpoint != null)
            {
                // App details mode...
                this.AppPageViewModel.MaaasApp = appManager.GetApp(endpoint);
                this.PanelSearch.Visibility = Visibility.Collapsed;
                this.BtnSave.Visibility = Visibility.Collapsed;
                this.BtnLaunch.Visibility = Visibility.Visible;
                this.BtnDelete.Visibility = Visibility.Visible;
                this.PanelDetails.Visibility = Visibility.Visible;
            }
            else
            {
                // "Find" mode...
                this.PanelSearch.Visibility = Visibility.Visible;
                this.PanelDetails.Visibility = Visibility.Collapsed;
            }
        }

        async void BtnFind_Click(object sender, RoutedEventArgs e)
        {
            string endpoint = this.AppFindEndpoint.Text;

            var managedApp = appManager.GetApp(endpoint);
            if (managedApp != null)
            {
                MessageBox.Show("You already have a Maaas application with the supplied endpoint in your list", "Maaas Application Search", MessageBoxButton.OK);
                return;
            }

            Transport transport = new TransportHttp(endpoint);

            JObject appDefinition = await transport.getAppDefinition();
            if (appDefinition == null)
            {
                MessageBox.Show("No Maaas application found at the supplied endpoint", "Maaas Application Search", MessageBoxButton.OK);
            }
            else
            {
                this.AppPageViewModel.MaaasApp = new MaaasApp(endpoint, appDefinition);
                this.PanelSearch.Visibility = Visibility.Collapsed;
                this.BtnSave.Visibility = Visibility.Visible;
                this.BtnLaunch.Visibility = Visibility.Collapsed;
                this.BtnDelete.Visibility = Visibility.Collapsed;
                this.PanelDetails.Visibility = Visibility.Visible;
            }
        }

        async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            appManager.Apps.Add(this.AppPageViewModel.MaaasApp);
            await appManager.saveState();
            Launcher.NavigateTo();
        }

        void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            MaaasPage.NavigateTo(this.AppPageViewModel.MaaasApp.Endpoint);
        }

        async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to remove this Maaas application from your list", "Maaas Application Delete", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                MaaasApp app = appManager.GetApp(this.AppPageViewModel.MaaasApp.Endpoint);
                appManager.Apps.Remove(app);
                await appManager.saveState();
                Launcher.NavigateTo();
            }
        }
    }
}