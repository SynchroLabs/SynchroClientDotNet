using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MaaasCore;
using Newtonsoft.Json.Linq;

namespace MaaasClientAndroid.Controls
{
    class WrapperHolder : Java.Lang.Object 
    {
        public readonly AndroidControlWrapper Value;

        public WrapperHolder(AndroidControlWrapper value)
        {
                this.Value = value;
        }
    }

    class AndroidControlWrapper : ControlWrapper
    {
        protected View _control;
        public View Control { get { return _control; } }

        public AndroidControlWrapper(StateManager stateManager, ViewModel viewModel, BindingContext bindingContext, View control) :
            base(stateManager, viewModel, bindingContext)
        {
            _control = control;
        }

        public AndroidControlWrapper(ControlWrapper parent, BindingContext bindingContext, View control = null) :
            base(parent, bindingContext)
        {
            _control = control;
        }

        protected void applyFrameworkElementDefaults(View element)
        {
            // !!! This could be a little more thourough ;)
        }

        protected void processCommonFrameworkElementProperies(JObject controlSpec)
        {
            // !!! This could be a little more thourough ;)
        }

        public static AndroidControlWrapper getControlWrapper(View control)
        {
            return ((WrapperHolder)control.Tag).Value;
        }

        public static AndroidControlWrapper WrapControl(StateManager stateManager, ViewModel viewModel, BindingContext bindingContext, View control)
        {
            return new AndroidControlWrapper(stateManager, viewModel, bindingContext, control);
        }

        public static AndroidControlWrapper CreateControl(ControlWrapper parent, BindingContext bindingContext, JObject controlSpec)
        {
            AndroidControlWrapper controlWrapper = null;

            switch ((string)controlSpec["type"])
            {
                case "text":
                    controlWrapper = new AndroidTextBlockWrapper(parent, bindingContext, controlSpec);
                    break;
                case "edit":
                    controlWrapper = new AndroidTextBoxWrapper(parent, bindingContext, controlSpec);
                    break;
                case "button":
                    controlWrapper = new AndroidButtonWrapper(parent, bindingContext, controlSpec);
                    break;
                case "stackpanel":
                    controlWrapper = new AndroidStackPanelWrapper(parent, bindingContext, controlSpec);
                    break;
            }

            if (controlWrapper != null)
            {
                controlWrapper.processCommonFrameworkElementProperies(controlSpec);
                parent.ChildControls.Add(controlWrapper);
                controlWrapper.Control.Tag = new WrapperHolder(controlWrapper);
            }

            return controlWrapper;
        }

        public void createControls(JArray controlList, Action<JObject, AndroidControlWrapper> OnCreateControl = null)
        {
            base.createControls(this.BindingContext, controlList, (controlContext, controlSpec) =>
            {
                AndroidControlWrapper controlWrapper = CreateControl(this, controlContext, controlSpec);
                if (OnCreateControl != null)
                {
                    OnCreateControl(controlSpec, controlWrapper);
                }
            });
        }
    }
}