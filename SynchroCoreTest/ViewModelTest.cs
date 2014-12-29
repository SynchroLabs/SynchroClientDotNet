using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SynchroCore;

namespace SynchroCoreTest
{
    [TestClass]
    public class ViewModelTest
    {
        JObject viewModelObj = new JObject()
        {
            {"serial", new JValue(1)},
            {"title", new JValue("Colors")},
            {"colors", new JArray()
                { 
                    new JObject(){ {"name", new JValue("Red")}, {"color", new JValue("red")}, {"value", new JValue("0xff0000")} },
                    new JObject(){ {"name", new JValue("Green")}, {"color", new JValue("green")}, {"value", new JValue("0x00ff00")} },
                    new JObject(){ {"name", new JValue("Blue")}, {"color", new JValue("blue")}, {"value", new JValue("0x0000ff")} }
                }
            }
        };

        [TestMethod]
        public void TestUpdateView()
        {
            // Create a binding of each type, initialize them from the view model, verify that their values were set properly
            //
            var viewModel = new ViewModel();
        
            viewModel.InitializeViewModelData(viewModelObj);
      
            var serialString = "";
            var propBinding = viewModel.CreateAndRegisterPropertyBinding(viewModel.RootBindingContext, value: "Serial: {serial}", setValue: (valueToken) =>
            {
                serialString = (string)valueToken;
            });

            var serialValue = -1;
            var valBinding = viewModel.CreateAndRegisterValueBinding(viewModel.RootBindingContext.Select("serial"),
                getValue: () =>
                {
                    return new JValue(serialValue);
                },
                setValue: (valueToken) =>
                   {
                    serialValue = (int)valueToken;
                }
            );

            propBinding.UpdateViewFromViewModel();
            valBinding.UpdateViewFromViewModel();
        
            Assert.AreEqual("Serial: 1", serialString);
            Assert.AreEqual(1, serialValue);
        }

        [TestMethod]
        public void TestUpdateViewFromValueBinding()
        {
            var viewModel = new ViewModel();
        
            viewModel.InitializeViewModelData(viewModelObj);
        
            var bindingsInitialized = false;
        
            var serialString = "";
            var propBinding = viewModel.CreateAndRegisterPropertyBinding(viewModel.RootBindingContext, value: "Serial: {serial}", setValue: (valueToken) =>
            {
                serialString = (string)valueToken;
            });

            var titleString = "";
            var propBindingTitle = viewModel.CreateAndRegisterPropertyBinding(viewModel.RootBindingContext, value: "Title: {title}", setValue: (valueToken) =>
            {
                titleString = (string)valueToken;
                if (bindingsInitialized)
                {
                    Assert.Fail("Property binding setter for title should not be called after initialization (since its token wasn't impacted by the value binding change)");
                }
            });

            var serialValue = -1;
            var valBinding = viewModel.CreateAndRegisterValueBinding(viewModel.RootBindingContext.Select("serial"),
                getValue: () =>
                {
                    return new JValue(serialValue);
                },
                setValue: (valueToken) =>
                {
                    serialValue = (int)valueToken;
                    if (bindingsInitialized)
                    {
                        Assert.Fail("Value bining setter should not be called after initialization (its change shouldn't update itself)");
                    }
                }
            );
        
            propBinding.UpdateViewFromViewModel();
            propBindingTitle.UpdateViewFromViewModel();
            valBinding.UpdateViewFromViewModel();

            bindingsInitialized = true;

            Assert.AreEqual("Serial: 1", serialString);
            Assert.AreEqual("Title: Colors", titleString);
            Assert.AreEqual(1, serialValue);
        
            // When the value binding updates the view model, the propBinding (that has a token bound to the same context/path) will automatically
            // update (its setter will be called), but the value binding that triggered the update will not have its setter called.
            //
            serialValue = 2;
            valBinding.UpdateViewModelFromView();

            Assert.AreEqual("Serial: 2", serialString);
        
            // Now let's go collect the changes caused by value binding updates and verify them...
            //
            var changes = viewModel.CollectChangedValues();
            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(2, (int)changes["serial"]);
        
            // Collecting the changes (above) should have cleared the dirty indicators, so there shouldn't be any changes now...
            //
            Assert.AreEqual(0, viewModel.CollectChangedValues().Count);
        }

        [TestMethod]
        public void TestUpdateViewFromViewModelDeltas()
        {
            var viewModel = new ViewModel();
        
            viewModel.InitializeViewModelData(viewModelObj);
        
            var bindingsInitialized = false;
        
            var serialString = "";
            var propBinding = viewModel.CreateAndRegisterPropertyBinding(viewModel.RootBindingContext, value: "Serial: {serial}", setValue: (valueToken) =>
            {
                serialString = (string)valueToken;
            });
        
            var titleString = "";
            var propBindingTitle = viewModel.CreateAndRegisterPropertyBinding(viewModel.RootBindingContext, value: "Title: {title}", setValue: (valueToken) =>
            {
                titleString = (string)valueToken;
                if (bindingsInitialized)
                {
                    Assert.Fail("Property binding setter for title should not be called after initialization (since its token wasn't impacted by the deltas)");
                }
            });
        
            var serialValue = -1;
            var valBinding = viewModel.CreateAndRegisterValueBinding(viewModel.RootBindingContext.Select("serial"),
                getValue: () => 
                {
                    return new JValue(serialValue);
                },
                setValue: (valueToken) =>
                {
                    serialValue = (int)valueToken;
                }
            );
        
            propBinding.UpdateViewFromViewModel();
            propBindingTitle.UpdateViewFromViewModel();
            valBinding.UpdateViewFromViewModel();
        
            bindingsInitialized = true;
        
            Assert.AreEqual("Serial: 1", serialString);
            Assert.AreEqual("Title: Colors", titleString);
            Assert.AreEqual(1, serialValue);
        
            // We're going to apply some deltas to the view model and verify that the correct dependant bindings got updated,
            // and that no non-dependant bindings got updated
            //
            var deltas = new JArray()
            {
                new JObject(){ {"path", new JValue("serial")}, {"change", new JValue("update")}, {"value", new JValue(2)} }
            };
            viewModel.UpdateViewModelData(deltas, updateView: true);

            Assert.AreEqual("Serial: 2", serialString);
            Assert.AreEqual(2, serialValue);
        }
    }
}
