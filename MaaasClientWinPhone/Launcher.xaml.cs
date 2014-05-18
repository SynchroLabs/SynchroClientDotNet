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
        public static MaaasAppManager MaaasAppManager { get; set; }

        ObservableCollection<MaaasApp> maaasApps = new ObservableCollection<MaaasApp>();

        public Launcher()
        {
            InitializeComponent();

            foreach (MaaasApp app in MaaasAppManager.Apps)
            {
                maaasApps.Add(app);
            }

            this.appListControl.ItemsSource = maaasApps;

            this.appListControl.SelectionChanged += appListControl_SelectionChanged;
        }

        void appListControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MaaasPage.MaaasApp = this.appListControl.SelectedItem as MaaasApp;
            App.RootFrame.Navigate(new Uri("/MaaasPage.xaml", UriKind.Relative));
        }
    }
}