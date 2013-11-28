// Menu page
//
exports.View =
{
<<<<<<< HEAD
    title: "MAaaS Menu",
    elements: 
    [
        { type: "image", resource: "http://fbcdn-profile-a.akamaihd.net/hprofile-ak-ash3/c23.23.285.285/s160x160/943786_10201215910308278_1343091684_n.jpg" },
        { type: "button", caption: "Hello World", binding: "hello" },
        { type: "button", caption: "Login Sample", binding: "login" },
        { type: "button", caption: "Click Counter Sample", binding: "counter" },
        { type: "button", caption: "List Sample", binding: "list" },
        { type: "button", caption: "Sandbox", binding: "sandbox" },
=======
    Title: "MAaaS Menu",
    Elements: 
    [
        { type: "image", resource: "resources/tdd.png" },
        { type: "button", caption: "Hello World", binding: "hello", margin: { top: 10 } },
        { type: "button", caption: "Login Sample", binding: "login", margin: { top: 10 } },
        { type: "button", caption: "Click Counter Sample", binding: "counter", margin: { top: 10 } },
        { type: "button", caption: "List Sample", binding: "list", margin: { top: 10 } },
        { type: "button", caption: "Sandbox", binding: "sandbox", margin: { top: 10 } },
>>>>>>> bbe1c8a0fc244cc69ecf4479da62d583974517c5
    ]
}

exports.Commands = 
{
<<<<<<< HEAD
    hello: function(context)
    {
        return navigateToView(context, "hello");
    },
    login: function(context)
    {
        return navigateToView(context, "login");
    },
    counter: function(context)
    {
        return navigateToView(context, "counter");
    },
    list: function(context)
    {
        return navigateToView(context, "list");
    },
    sandbox: function(context)
    {
        return navigateToView(context, "sandbox");
=======
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
>>>>>>> bbe1c8a0fc244cc69ecf4479da62d583974517c5
    },
}
