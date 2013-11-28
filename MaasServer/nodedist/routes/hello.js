// Hello page
//
exports.View =
{
<<<<<<< HEAD
    title: "Hello World",
    onBack: "exit",
    elements:
    [
        { type: "stackpanel", contents: [
            { type: "text", value: "First name:", fontsize: 24, margin: { top: 10, right: 10 } },
            { type: "edit", fontsize: 24, binding: "firstName" },
        ] },
        { type: "stackpanel", contents: [
            { type: "text", value: "Last name:", fontsize: 24, margin: { top: 10, right: 10 } },
=======
    Title: "Hello World",
    Elements:
    [
        { type: "stackpanel", contents: [
            { type: "text", value: "First name:", fontsize: 24 },
            { type: "edit", fontsize: 24, binding: "firstName" },
        ] },
        { type: "stackpanel", contents: [
            { type: "text", value: "Last name:", fontsize: 24 },
>>>>>>> bbe1c8a0fc244cc69ecf4479da62d583974517c5
            { type: "edit", fontsize: 24, binding: "lastName" },
        ] },

        { type: "text", value: "Hello {firstName} {lastName}", fontsize: 24 },
<<<<<<< HEAD
    ]
}

exports.InitializeViewModelState = function(context, session)
{
    var vmState =
=======

        { type: "button", caption: "Return to menu!", binding: "exit" },
    ]
}

exports.InitializeBoundItems = function (state, session)
{
    var boundItems =
>>>>>>> bbe1c8a0fc244cc69ecf4479da62d583974517c5
    {
        firstName: "Planet",
        lastName: "Earth",
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
    exit: function(context)
    {
        return navigateToView(context, "menu");
=======
    exit: function (state)
    {
        return navigateToView(state, "menu");
>>>>>>> bbe1c8a0fc244cc69ecf4479da62d583974517c5
    },
}