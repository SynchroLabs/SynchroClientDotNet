// List page
//
exports.View =
{
    Title: "List example",
    Elements: 
    [
        { type: "text", value: "Your items", fontsize: 24 },
        { type: "listbox", width: 250, binding: "items" },

        { type: "text", value: "New item", fontsize: 24 },
        { type: "edit", binding: "colors[1].color" },
        { type: "button", caption: "Add", binding: "addItem" },

        { type: "button", caption: "Return to menu!", binding: "exit" },

        { type: "text", value: "{$parent.$parent.$parent.caption}: {color}", fontsize: 24, binding: { foreach: "colors" } },
        { type: "edit", fontsize: 24, binding: { foreach: "colors", value: "color"} },
    ]
}

exports.InitializeBoundItems = function(state, session)
{
    var boundItems =
    {
        caption: "The Color",
        itemToAdd: "",
        items: [ "white", "black", "yellow" ],
        colors:
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
