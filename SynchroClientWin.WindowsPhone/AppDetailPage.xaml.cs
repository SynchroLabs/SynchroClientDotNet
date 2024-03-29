﻿using SynchroCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using MaaasClientWin;
using Windows.UI.Xaml.Navigation;
using MaaasClientWin.Common;
using Windows.UI.Popups;
using Windows.Phone.UI.Input;

namespace MaaasClientWin
{
    public partial class AppDetailPage : BasicPage
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

            this.PanelSearch.Visibility = Visibility.Collapsed;
            this.PanelDetails.Visibility = Visibility.Collapsed;

            this.BtnFind.Click += BtnFind_Click;
            this.BtnSave.Click += BtnSave_Click;
        }

        protected override async void LoadState(LoadStateEventArgs args)
        {
            await appManager.loadState();

            string endpoint = args.NavigationParameter as string;
            if (endpoint != null)
            {
                // App details mode...
                this.App = appManager.GetApp(endpoint);
                this.PanelSearch.Visibility = Visibility.Collapsed;
                this.BtnSave.Visibility = Visibility.Collapsed;
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
                var errMessage = new MessageDialog("You already have a Synchro application with the supplied endpoint in your list", "Synchro Application Search");
                await errMessage.ShowAsync();
                return;
            }

            bool formatException = false;
            try
            {
                Uri endpointUri = TransportHttp.UriFromHostString(endpoint);
                Transport transport = new TransportHttp(endpointUri);

                JObject appDefinition = await transport.getAppDefinition();
                if (appDefinition == null)
                {
                    var errMessage = new MessageDialog("No Synchro application found at the supplied endpoint", "Synchro Application Search");
                    await errMessage.ShowAsync();
                }
                else
                {
                    this.App = new MaaasApp(endpoint, appDefinition);
                    this.PanelSearch.Visibility = Visibility.Collapsed;
                    this.BtnSave.Visibility = Visibility.Visible;
                    this.PanelDetails.Visibility = Visibility.Visible;
                }
            }
            catch (FormatException)
            {
                // Can't await async message dialog in catch block (until C# 6.0).
                //
                formatException = true;
            }

            if (formatException)
            {
                var errMessage = new MessageDialog("Endpoint not formatted correctly", "Synchro Application Search");
                await errMessage.ShowAsync();
            }
        }

        async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            appManager.Apps.Add(this.App);
            await appManager.saveState();
            this.Frame.GoBack();
        }
    }
}