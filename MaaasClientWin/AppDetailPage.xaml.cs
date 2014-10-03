using MaaasClientWin.Common;
using MaaasCore;
using MaaasShared;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
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

    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class AppDetailPage : Page
    {
        private NavigationHelper navigationHelper;

        private MaaasAppManager appManager = new WinAppManager();

        private AppPageViewModel _vm = new AppPageViewModel();
        public AppPageViewModel AppPageViewModel { get { return _vm; } }

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public AppDetailPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
        }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            this.BtnFind.Click += BtnFind_Click;
            this.BtnSave.Click += BtnSave_Click;
            this.BtnLaunch.Click += BtnLaunch_Click;
            this.BtnDelete.Click += BtnDelete_Click;

            await appManager.loadState();

            string endpoint = e.NavigationParameter as string;
            if (endpoint != null)
            {
                // App details mode...
                this.AppPageViewModel.MaaasApp = appManager.GetApp(endpoint);
                this.SearchGrid.Visibility = Visibility.Collapsed;
                this.DetailsGrid.Visibility = Visibility.Visible;
                this.ActionsGrid.Visibility = Visibility.Visible;
                this.BtnSave.Visibility = Visibility.Collapsed;
                this.BtnLaunch.Visibility = Visibility.Visible;
                this.BtnDelete.Visibility = Visibility.Visible;
            }
            else
            {
                // "Find" mode...
                this.SearchGrid.Visibility = Visibility.Visible;
                this.DetailsGrid.Visibility = Visibility.Collapsed;
                this.ActionsGrid.Visibility = Visibility.Collapsed;
            }
        }

        async void BtnFind_Click(object sender, RoutedEventArgs e)
        {
            string endpoint = this.AppFindEndpoint.Text;

            var managedApp = appManager.GetApp(endpoint);
            if (managedApp != null)
            {
                var errMessage = new MessageDialog("You already have a Synchro application with the supplied endpoint in your list", "Synchro Application Search");
                await errMessage.ShowAsync();
                return;
            }

            Transport transport = new TransportHttp(endpoint);

            JObject appDefinition = await transport.getAppDefinition();
            if (appDefinition == null)
            {
                var errMessage = new MessageDialog("No Synchro application found at the supplied endpoint", "Synchro Application Search");
                await errMessage.ShowAsync();
            }
            else
            {
                this.AppPageViewModel.MaaasApp = new MaaasApp(endpoint, appDefinition);
                this.SearchGrid.Visibility = Visibility.Collapsed;
                this.DetailsGrid.Visibility = Visibility.Visible;
                this.ActionsGrid.Visibility = Visibility.Visible;
                this.BtnSave.Visibility = Visibility.Visible;
                this.BtnLaunch.Visibility = Visibility.Collapsed;
                this.BtnDelete.Visibility = Visibility.Collapsed;
            }
        }

        async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            appManager.Apps.Add(this.AppPageViewModel.MaaasApp);
            await appManager.saveState();
            this.Frame.Navigate(typeof(LauncherPage));
        }

        void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MaaasPage), this.AppPageViewModel.MaaasApp.Endpoint);
        }

        async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var confirmMessage = new MessageDialog("Are you sure you want to remove this Synchro application from your list", "Synchro Application Delete");
            confirmMessage.Commands.Add(new Windows.UI.Popups.UICommand("Yes", async (command) =>
            {
                MaaasApp app = appManager.GetApp(this.AppPageViewModel.MaaasApp.Endpoint);
                appManager.Apps.Remove(app);
                await appManager.saveState();
                this.Frame.Navigate(typeof(LauncherPage));
            }));
            confirmMessage.Commands.Add(new Windows.UI.Popups.UICommand("No"));
            await confirmMessage.ShowAsync();
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion
    }
}