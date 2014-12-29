using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using MaaasClientWin.Common;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using SynchroCore;

namespace MaaasClientWin
{
    public sealed partial class LauncherPage : BasicPage
    {
        static Logger logger = Logger.GetLogger("LauncherPage");

        public LauncherPage()
        {
            this.InitializeComponent();
        }

        protected override async void LoadState(LoadStateEventArgs args)
        { 
            MaaasAppManager appManager = new WinAppManager();
            await appManager.loadState();
            this.DefaultViewModel["Title"] = "Synchro Applications";
            this.DefaultViewModel["Items"] = appManager.Apps;

            this.AddMaaasAppButton.Click += AddMaaasAppButton_Click;
        }

        private void itemGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void itemGridView_ItemClick(object sender, ItemClickEventArgs e)
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
