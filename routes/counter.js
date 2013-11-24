// Counter page
//
exports.View =
{
    Title: "Click Counter",
    Elements: 
    [
        { type: "text", value: "Count: {count}", foreground: "{color}", fontsize: 24, fontweight: "{weight}" },
        { type: "button", caption: "Increment Count!", binding: "increment" },
        { type: "button", caption: "Decrement Count!", binding: "decrement", enabled: "{count}" },
        { type: "button", caption: "Reset Count!", binding: "reset" },
        { type: "button", caption: "Return to menu!", binding: "exit" },
    ]
}

exports.InitializeBoundItems = function(state, session)
{
    var boundItems =
    {
        count: 0,
        color: "Green",
        weight: "Normal",
    }
    return boundItems;
}

exports.OnChange = function(state, session, boundItems, source, changes)
{
    if (boundItems.count < 10)
    {
        boundItems.color = "Green";
        boundItems.weight = "Normal";
    }
    else
    {
        boundItems.color = "Red";
        boundItems.weight = "Bold";
    }
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
