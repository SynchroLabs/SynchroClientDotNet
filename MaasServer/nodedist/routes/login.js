// Login page
//
exports.View =
{
<<<<<<< HEAD
    title: "Login",
    onBack: "cancel",
    elements:
    [
        { type: "text", value: "Username", fontsize: 24, margin: { bottom: 0 } },
        { type: "edit", binding: "username", width: 200 },
        { type: "text", value: "Password", fontsize: 24, margin: { bottom: 0 } },
        { type: "password", binding: "password", width: 200 },
        { type: "stackpanel", margin: { top: 10 }, contents: [
=======
    Title: "Login",
    Elements: 
    [
        { type: "text", value: "Username", fontsize: 24 },
        { type: "edit", binding: "username", width: 200 },
        { type: "text", value: "Password", fontsize: 24 },
        { type: "password", binding: "password", width: 200 },
        { type: "stackpanel", contents: [
>>>>>>> bbe1c8a0fc244cc69ecf4479da62d583974517c5
            { type: "button", caption: "Login", width: 100, binding: "login" },
            { type: "button", caption: "Cancel", width: 100, binding: "cancel" },
        ] },
        { type: "toggle", binding: "showPassword", header: "Show Password", onLabel: "Showing", offLabel: "Hiding", fontsize: 24 },
        { type: "text", value: "Current entered password: {password}", fontsize: 24, visibility: "{showPassword}" },
    ]
}

<<<<<<< HEAD
exports.InitializeViewModelState = function(context, session)
{
    var vmState =
=======
exports.InitializeBoundItems = function(state, session)
{
    var boundItems =
>>>>>>> bbe1c8a0fc244cc69ecf4479da62d583974517c5
    {
        username: "test",
        password: "",
        showPassword: false
    }
<<<<<<< HEAD
    return vmState;
=======
    return boundItems;
>>>>>>> bbe1c8a0fc244cc69ecf4479da62d583974517c5
}

exports.Commands = 
{
<<<<<<< HEAD
    login: function(context, session, vmState)
    {
        if (vmState.username && (vmState.username == vmState.password))
        {
            session.username = vmState.username;
=======
    login: function(state, session, boundItems)
    {
        if (boundItems.username && (boundItems.username == boundItems.password))
        {
            session.username = boundItems.username;
>>>>>>> bbe1c8a0fc244cc69ecf4479da62d583974517c5
            var messageBox = 
            {
                title: "Winner",
                message: "Congrats {username}, you succeeded!  Now on the Counter app...",
                options:
                [
                    { label: "Ok", command: "success" },
                    { label: "Cancel" },
                ]
            }
<<<<<<< HEAD
            return showMessage(context, messageBox);
        }
        else
        {
            return showMessage(context, { message: "Sorry, you failed!" });
        }
    },
    success: function(context)
    {
        return navigateToView(context, "counter");
    },
    cancel: function(context)
    {
        return navigateToView(context, "menu");
=======
            return showMessage(state, messageBox);
        }
        else
        {
            return showMessage(state, { message: "Sorry, you failed!" });
        }
    },
    success: function(state)
    {
        return navigateToView(state, "counter");
    },
    cancel: function(state, session, boundItems)
    {
        return navigateToView(state, "menu");
>>>>>>> bbe1c8a0fc244cc69ecf4479da62d583974517c5
    },
}
