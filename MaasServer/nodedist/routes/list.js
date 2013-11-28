// List page
//
exports.View =
{
<<<<<<< HEAD
    title: "List example",
    onBack: "exit",
    elements:
=======
    Title: "List example",
    Elements: 
>>>>>>> bbe1c8a0fc244cc69ecf4479da62d583974517c5
    [
        { type: "stackpanel", contents: [
            { type: "text", value: "New item:", fontsize: 24 },
            { type: "edit", binding: "itemToAdd" },
            { type: "button", caption: "Add", binding: "add", enabled: "{itemToAdd}" },
        ] },

        { type: "text", value: "Your items", fontsize: 24 },
        { type: "listbox", width: 250, binding: { items: "items", selection: "selectedItem" } },

        { type: "stackpanel", contents: [
            { type: "button", caption: "Remove", binding: "remove", enabled: "{selectedItem}" },
            { type: "button", caption: "Sort", binding: "sort" },
        ] },
<<<<<<< HEAD
    ]
}

exports.InitializeViewModelState = function(context, session)
{
    var vmState =
=======

        { type: "button", caption: "Return to menu!", binding: "exit" },
    ]
}

exports.InitializeBoundItems = function(state, session)
{
    var boundItems =
>>>>>>> bbe1c8a0fc244cc69ecf4479da62d583974517c5
    {
        itemToAdd: "",
        items: [ "white", "black", "yellow" ],
        selectedItem: "black",
    }
<<<<<<< HEAD
    return vmState;
=======
    return boundItems;
>>>>>>> bbe1c8a0fc244cc69ecf4479da62d583974517c5
}

exports.Commands = 
{
<<<<<<< HEAD
    add: function(context, session, vmState)
    {
        vmState.items.push(vmState.itemToAdd);
        vmState.itemToAdd = "";
    },
    sort: function(context, session, vmState)
    {
        vmState.items.sort();
    },
    remove: function(context, session, vmState)
    {
        vmState.items.remove(vmState.selectedItem);
        vmState.selectedItem = "";
    },
    exit: function(context)
    {
        return navigateToView(context, "menu");
=======
    add: function(state, session, boundItems)
    {
        boundItems.items.push(boundItems.itemToAdd);
        boundItems.itemToAdd = "";
    },
    sort: function (state, session, boundItems)
    {
        boundItems.items.sort();
    },
    remove: function (state, session, boundItems)
    {
        boundItems.items.remove(boundItems.selectedItem);
        boundItems.selectedItem = "";
    },
    exit: function (state)
    {
        return navigateToView(state, "menu");
>>>>>>> bbe1c8a0fc244cc69ecf4479da62d583974517c5
    },
}
