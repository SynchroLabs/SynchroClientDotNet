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
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Popups;

namespace MaaasClientWin
{
    public partial class LauncherPage : BasicPage
    {
        static Logger logger = Logger.GetLogger("LauncherPage");
        MaaasAppManager _appManager = new WinAppManager();

        public LauncherPage()
        {
            InitializeComponent();
        }

        protected override async void LoadState(LoadStateEventArgs args)
        {
            await _appManager.loadState();
            this.DefaultViewModel["Items"] = _appManager.Apps;

            this.AddMaaasAppButton.Click += AddMaaasAppButton_Click;
        }

        private void appListControl_ItemClick(object sender, ItemClickEventArgs e)
        {
            MaaasApp maaasApp = (MaaasApp)e.ClickedItem;
            logger.Debug("Item click, endpoint: {0}", maaasApp.Endpoint);
            this.Frame.Navigate(typeof(MaaasPage), maaasApp.Endpoint);
        }

        void AddMaaasAppButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AppDetailPage), null);
        }

        private void appListControl_Holding(object sender, Windows.UI.Xaml.Input.HoldingRoutedEventArgs e)
        {
            logger.Info("Holding");
            FrameworkElement senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);
            flyoutBase.ShowAt(senderElement);
            e.Handled = true;
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("Details clicked");
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            MaaasApp maaasApp = (MaaasApp)item.DataContext;
            this.Frame.Navigate(typeof(AppDetailPage), maaasApp.Endpoint);
        }

        async private void Delete_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("Delete clicked");
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            MaaasApp maaasApp = (MaaasApp)item.DataContext;

            var confirmMessage = new MessageDialog("Are you sure you want to remove this Synchro application from your list", "Synchro Application Delete");
            confirmMessage.Commands.Add(new Windows.UI.Popups.UICommand("Yes", async (command) =>
            {
                _appManager.Apps.Remove(maaasApp);
                await _appManager.saveState();
            }));
            confirmMessage.Commands.Add(new Windows.UI.Popups.UICommand("No"));
            await confirmMessage.ShowAsync();
        }
    }
}