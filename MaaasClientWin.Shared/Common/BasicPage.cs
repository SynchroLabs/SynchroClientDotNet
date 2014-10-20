using System;
using System.Collections.Generic;
using System.Text;
using Windows.Graphics.Display;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

#if WINDOWS_PHONE_APP
using Windows.Phone.UI.Input;
#endif

namespace MaaasClientWin.Common
{
    public class BasicPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public BasicPage()
        {
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.navigationHelper.SaveState += navigationHelper_SaveState;
        }

        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            LoadState(e);
        }

        private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            SaveState(e);
        }

        protected virtual void LoadState(LoadStateEventArgs e) { }
        protected virtual void SaveState(SaveStateEventArgs e) { }

        #region NavigationHelper registration

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        protected DisplayOrientations normalizeOrientation(DisplayOrientations orientation)
        {
            if (orientation == DisplayOrientations.LandscapeFlipped)
            {
                return DisplayOrientations.Landscape;
            }
            else if (orientation == DisplayOrientations.PortraitFlipped)
            {
                return DisplayOrientations.Portrait;
            }

            return orientation;
        }

#if WINDOWS_PHONE_APP
        // http://msdn.microsoft.com/en-US/library/windows/apps/xaml/dn639128.aspx
        //
        public virtual void OnHardwareBackPressed(object sender, BackPressedEventArgs e) { }
#endif
    }
}