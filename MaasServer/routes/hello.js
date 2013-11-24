// Hello page
//
exports.View =
{
    Title: "Hello World",
    Elements:
    [
        { type: "stackpanel", contents: [
            { type: "text", value: "First name:", fontsize: 24 },
            { type: "edit", fontsize: 24, binding: "firstName" },
        ] },
        { type: "stackpanel", contents: [
            { type: "text", value: "Last name:", fontsize: 24 },
            { type: "edit", fontsize: 24, binding: "lastName" },
        ] },

        { type: "text", value: "Hello {firstName} {lastName}", fontsize: 24 },

        { type: "button", caption: "Return to menu!", binding: "exit" },
    ]
}

exports.InitializeBoundItems = function (state, session)
{
    var boundItems =
    {
        firstName: "Planet",
        lastName: "Earth",
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