﻿// Hello page
//
exports.View =
{
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
            { type: "edit", fontsize: 24, binding: "lastName" },
        ] },

        { type: "text", value: "Hello {firstName} {lastName}", fontsize: 24 },
    ]
}

exports.InitializeViewModelState = function(context, session)
{
    var vmState =
    {
        firstName: "Planet",
        lastName: "Earth",
    }
    return vmState;
}

exports.Commands =
{
    exit: function(context)
    {
        return navigateToView(context, "menu");
    },
}