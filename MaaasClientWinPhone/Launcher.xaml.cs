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
using System.Collections.ObjectModel;

namespace MaaasClientWinPhone
{
    public partial class Launcher : PhoneApplicationPage
    {
        public Launcher()
        {
            InitializeComponent();
        }

        public static void NavigateTo(string endpoint = null)
        {
            App.RootFrame.Navigate(new Uri("/Launcher.xaml", UriKind.Relative));
        }
        async protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            MaaasAppManager appManager = new WinPhoneAppManager();
            await appManager.loadState();

            ObservableCollection<MaaasApp> maaasApps = new ObservableCollection<MaaasApp>();
            foreach (MaaasApp app in appManager.Apps)
            {
                maaasApps.Add(app);
            }

            this.appListControl.ItemsSource = maaasApps;

            this.appListControl.SelectionChanged += appListControl_SelectionChanged;
        }

        void appListControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MaaasApp app = this.appListControl.SelectedItem as MaaasApp;
            AppDetailPage.NavigateTo(app.Endpoint);
        }

        private void OnAppAdd(object sender, EventArgs e)
        {
            AppDetailPage.NavigateTo();
        }
    }
}