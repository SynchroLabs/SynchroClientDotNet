// List page
//
exports.View =
{
    Title: "List example",
    Elements: 
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

        { type: "button", caption: "Return to menu!", binding: "exit" },
    ]
}

exports.InitializeBoundItems = function(state, session)
{
    var boundItems =
    {
        itemToAdd: "",
        items: [ "white", "black", "yellow" ],
        selectedItem: "black",
    }
    return boundItems;
}

exports.Commands = 
{
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
    },
}
