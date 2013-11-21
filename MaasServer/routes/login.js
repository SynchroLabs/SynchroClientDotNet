// Login page
//
exports.View =
{
    Title: "Login",
    Elements: 
    [
        { type: "text", value: "Username", fontsize: 24 },
        { type: "edit", binding: "username" },
        { type: "text", value: "Password", fontsize: 24 },
        { type: "edit", binding: { value: "password" } },
        { type: "stackpanel", contents: [
            { type: "button", caption: "Login", width: 100, binding: "login" },
            { type: "button", caption: "Cancel", width: 100, binding: "cancel" },
        ] },
        { type: "text", value: "Current user name: {username}", fontsize: 24 },
    ]
}

exports.InitializeBoundItems = function(state, session)
{
    var boundItems =
    {
        username: "test",
        password: "",
    }
    return boundItems;
}

exports.Commands = 
{
    login: function(state, session, boundItems)
    {
        console.log("Login command - username: " + boundItems.username + ", password: " + boundItems.password);
        if (boundItems.username && (boundItems.username == boundItems.password))
        {
            session.username = boundItems.username;
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
    },
}
