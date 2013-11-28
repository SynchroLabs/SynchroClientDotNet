// Counter page
//
exports.View =
{
<<<<<<< HEAD
    title: "Click Counter",
    onBack: "exit",
    elements: 
    [
        { type: "text", value: "Count: {count}", foreground: "{font.color}", fontweight: "{font.weight}", fontsize: 24 },
        { type: "button", caption: "Increment Count!", binding: "increment" },
        { type: "button", caption: "Decrement Count!", binding: "decrement", enabled: "{count}" },
        { type: "button", caption: "Reset Count!", binding: "reset" },
    ]
}

exports.InitializeViewModelState = function(context, session)
{
    var vmState =
    {
        count: 0,
        font: { color: "Green", weight: "Normal" },
    }
    return vmState;
}

exports.OnChange = function(context, session, vmState, source, changes)
{
    if (vmState.count < 10)
    {
        vmState.font = { color: "Green", weight: "Normal" };
    }
    else
    {
        vmState.font = { color: "Red", weight: "Bold" };
=======
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
>>>>>>> bbe1c8a0fc244cc69ecf4479da62d583974517c5
    }
}

exports.Commands = 
{
<<<<<<< HEAD
    increment: function(context, session, vmState)
    {
        vmState.count += 1;
    },
    decrement: function(context, session, vmState)
    {
        vmState.count -= 1;
    },
    reset: function(context, session, vmState)
    {
        vmState.count = 0;
    },
    exit: function(context)
    {
        return navigateToView(context, "menu");
=======
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
>>>>>>> bbe1c8a0fc244cc69ecf4479da62d583974517c5
    },
}
