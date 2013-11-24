// Sandbox page
//
exports.View =
{
    Title: "Sandbox",
    Elements:
    [
        { type: "text", value: "{$parent.$parent.$parent.caption}: {color}", fontsize: 24, binding: { foreach: "colors" } },
        { type: "edit", fontsize: 24, binding: { foreach: "colors", value: "color" } },

        { type: "button", caption: "Return to menu!", binding: "exit" },
    ]
}

exports.InitializeBoundItems = function (state, session)
{
    var boundItems =
    {
        caption: "The Color",
        colors:
        [
            { color: "red" }, { color: "green" }, { color: "blue" },
        ]
    }
    return boundItems;
}

exports.Commands =
{
    exit: function (state)
    {
        return navigateToView(state, "menu");
    },
}
