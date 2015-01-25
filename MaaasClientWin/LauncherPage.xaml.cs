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
using Windows.UI.Popups;

namespace MaaasClientWin
{
    public sealed partial class LauncherPage : BasicPage
    {
        static Logger logger = Logger.GetLogger("LauncherPage");
        MaaasAppManager _appManager = new WinAppManager();

        public LauncherPage()
        {
            this.InitializeComponent();
        }

        protected override async void LoadState(LoadStateEventArgs args)
        { 
            await _appManager.loadState();
            this.DefaultViewModel["Title"] = "Synchro Applications";
            this.DefaultViewModel["Items"] = _appManager.Apps;

            this.AddMaaasAppButton.Click += AddMaaasAppButton_Click;
        }

        private void itemGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void itemGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            MaaasApp maaasApp = (MaaasApp)e.ClickedItem;
            logger.Debug("Item click, endpoint: {0}", maaasApp.Endpoint);
            this.Frame.Navigate(typeof(MaaasPage), maaasApp.Endpoint);
        }

        void AddMaaasAppButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AppDetailPage), null);
        }

        private void GridViewItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);
            flyoutBase.ShowAt(senderElement);
            e.Handled = true;
        }

        private void GridViewItem_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);
            flyoutBase.ShowAt(senderElement);
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
