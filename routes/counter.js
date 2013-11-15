// Counter page
//
exports.View =
{
    Title: "Click Counter",
    Elements: 
    [
        { type: "text", value: "Count: {count}", fontsize: 24 },
        { type: "button", caption: "Increment Count!", command: "increment" },
        { type: "button", caption: "Decrement Count!", command: "decrement" },
        { type: "button", caption: "Reset Count!", command: "reset" },
        { type: "button", caption: "Return to menu!", command: "exit" },
        { type: "text", value: "Color 2: {colors[1]}", fontsize: 24 },
    ]
}

exports.InitializeBoundItems = function(state, session)
{
    var boundItems =
    {
        count: 0,
        colors:
        [
            "red", "green", "blue"
        ]
    }
    return boundItems;
}

exports.Commands = 
{
    increment: function(state, session, boundItems)
    {
        boundItems.count += 1;
    },
    decrement: function(state, session, boundItems)
    {
        boundItems.count -= 1;
    },
    reset: function(state, session, boundItems)
    {
        boundItems.count = 0;
    },
    exit: function(state)
    {
        return navigateToView(state, "menu");
    },
}
