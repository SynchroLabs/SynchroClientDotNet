﻿using MaaasClientWin.Common;
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

namespace MaaasClientWin
{
    public sealed partial class AppDetailPage : BasicPage
    {
        private MaaasAppManager appManager = new WinAppManager();

        private MaaasApp _app;
        private MaaasApp App
        {
            get
            {
                return _app;
            }
            set
            {
                _app = value;
                this.DefaultViewModel["App"] = _app;
            }
        }

        public AppDetailPage()
        {
            this.InitializeComponent();
        }

        protected override async void LoadState(LoadStateEventArgs args)
        {
            this.BtnFind.Click += BtnFind_Click;
            this.BtnSave.Click += BtnSave_Click;
            this.BtnLaunch.Click += BtnLaunch_Click;
            this.BtnDelete.Click += BtnDelete_Click;

            await appManager.loadState();

            string endpoint = args.NavigationParameter as string;
            if (endpoint != null)
            {
                // App details mode...
                this.App = appManager.GetApp(endpoint);
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
                this.App = new MaaasApp(endpoint, appDefinition);
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
            appManager.Apps.Add(this.App);
            await appManager.saveState();
            this.Frame.GoBack();
        }

        void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MaaasPage), this.App.Endpoint);
        }

        async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var confirmMessage = new MessageDialog("Are you sure you want to remove this Synchro application from your list", "Synchro Application Delete");
            confirmMessage.Commands.Add(new Windows.UI.Popups.UICommand("Yes", async (command) =>
            {
                MaaasApp app = appManager.GetApp(this.App.Endpoint);
                appManager.Apps.Remove(app);
                await appManager.saveState();
                this.Frame.GoBack();
            }));
            confirmMessage.Commands.Add(new Windows.UI.Popups.UICommand("No"));
            await confirmMessage.ShowAsync();
        }
    }
}