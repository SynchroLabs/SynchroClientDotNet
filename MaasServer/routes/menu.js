// Menu page
//
exports.View =
{
    Title: "MAaaS Menu",
    Elements: 
    [
        { type: "image", resource: "resources/tdd.png" },
        { type: "button", caption: "Hello World", binding: "hello", margin: { top: 10 } },
        { type: "button", caption: "Login Sample", binding: "login", margin: { top: 10 } },
        { type: "button", caption: "Click Counter Sample", binding: "counter", margin: { top: 10 } },
        { type: "button", caption: "List Sample", binding: "list", margin: { top: 10 } },
        { type: "button", caption: "Sandbox", binding: "sandbox", margin: { top: 10 } },
    ]
}

exports.Commands = 
{
    hello: function (state)
    {
        return navigateToView(state, "hello");
    },
    login: function (state)
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
    sandbox: function (state)
    {
        return navigateToView(state, "sandbox");
    },
}
