// Menu page
//
exports.View =
{
    Title: "MAaaS Menu",
    Elements: 
    [
        { type: "image", resource: "resources/tdd.png" },
        { type: "button", caption: "Login Sample", binding: "login" },
        { type: "button", caption: "Click Counter Sample", binding: "counter" },
        { type: "button", caption: "List Sample", binding: { onClick: "list" } },
    ]
}

exports.Commands = 
{
    login: function(state)
    {
        return navigateToView(state, "login");
    },
    counter: function(state)
    {
        return navigateToView(state, "counter");
    },
    list: function(state)
    {
        return navigateToView(state, "list");
    },
}
