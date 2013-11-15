// Login page
//
exports.View =
{
    Title: "Login",
    Elements: 
    [
        { type: "text", value: "Username", fontsize: 24 },
        { type: "edit",  boundValue: "username" },
        { type: "text", value: "Password", fontsize: 24 },
        { type: "edit", boundValue: "password" },
        { type: "button", caption: "Login", command: "login" },
        { type: "button", caption: "Cancel", command: "cancel" },
    ]
}

exports.InitializeBoundItems = function(state, session)
{
    var boundItems =
    {
        username: "test",
        password: ""
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
    cancel: function(state)
    {
        return navigateToView(state, "menu");
    },
}
