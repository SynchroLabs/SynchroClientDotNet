// List page
//
exports.View =
{
    Title: "List example",
    Elements: 
    [
        { type: "text", value: "Your items", fontsize: 24 },
        { type: "listbox", boundValue: "items" },

        { type: "text", value: "New item", fontsize: 24 },
        { type: "edit", boundValue: "itemToAdd" },
        { type: "button", caption: "Add", command: "addItem" },

        { type: "button", caption: "Return to menu!", command: "exit" },

        { type: "button", caption: "Binding test", binding: { onClick: "exit" } },

        { type: "text", value: "{color}", fontsize: 24, binding: { foreach: "items" } },

    ]
}

exports.InitializeBoundItems = function(state, session)
{
    var boundItems =
    {
        itemToAdd: "",
        items:
        [
            { color: "red" }, { color: "green" }, { color: "blue" },
        ]
    }
    return boundItems;
}

exports.Commands = 
{
    addItem: function(state, session, boundItems)
    {
        if (boundItems.itemToAdd != "")
        {
            boundItems.items.push(boundItems.itemToAdd);
            boundItems.itemToAdd = "";
        }
    },
    exit: function(state)
    {
        return navigateToView(state, "menu");
    },
}
