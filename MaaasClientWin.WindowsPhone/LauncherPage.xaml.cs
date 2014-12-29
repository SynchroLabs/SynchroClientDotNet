using SynchroCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls;
using MaaasClientWin.Common;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml;

namespace MaaasClientWin
{
    public partial class LauncherPage : BasicPage
    {
        static Logger logger = Logger.GetLogger("LauncherPage");

        public LauncherPage()
        {
            InitializeComponent();
        }

        protected override async void LoadState(LoadStateEventArgs args)
        {
            MaaasAppManager appManager = new WinAppManager();
            await appManager.loadState();
            this.DefaultViewModel["Items"] = appManager.Apps;

            this.AddMaaasAppButton.Click += AddMaaasAppButton_Click;
        }

        private void appListControl_ItemClick(object sender, ItemClickEventArgs e)
        {
            MaaasApp maaasApp = (MaaasApp)e.ClickedItem;
            logger.Debug("Item click, endpoint: {0}", maaasApp.Endpoint);
            this.Frame.Navigate(typeof(AppDetailPage), maaasApp.Endpoint);
        }

        void AddMaaasAppButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AppDetailPage), null);
        }
    }
}