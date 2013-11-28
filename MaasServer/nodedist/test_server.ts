///<reference path='node\node.d.ts' />
///<reference path='node\express.d.ts' />
///<reference path='node\require-dir.d.ts' />

import express = require("express");
import requireDir = require('require-dir');

var objectMonitor = require("./objectMonitor");

var routes = requireDir("routes");
for (var routePath in routes) {
    console.log("Found route processor for: " + routePath);
    var route = routes[routePath];
    route.View["Path"] = routePath;
}

var app = express();
app.use(express.cookieParser());
// Note: Setting the maxAge value to 60000 (one hour) generates a cookie that .NET does not record (date generation/parsing
// is my guess) - for now we just omit expiration...
app.use(express.cookieSession({ secret: 'sdf89f89fd7sdf7sdf', cookie: { maxAge: false, httpOnly: true } }));
app.use(express.query());
app.use(express.json());
app.use("/resources", express.static("./resources"));

app.get('/', function (req, res)
{
    res.send('No view route provided');
});

function getObjectProperty(obj, propertyPath)
{
    propertyPath = propertyPath.replace(/\[(\w+)\]/g, '.$1'); // convert indexes to properties
    var parts = propertyPath.split('.'),
        last = parts.pop(),
        len = parts.length,
        i = 1,
        current = parts[0];

    if (len > 0)
    {
        while ((obj = obj[current]) && i < len)
        {
            current = parts[i];
            i++;
        }
    }

    if (obj)
    {
        return obj[last];
    }
}

function setObjectProperty(obj, propertyPath, value)
{
    propertyPath = propertyPath.replace(/\[(\w+)\]/g, '.$1'); // convert indexes to properties
    var parts = propertyPath.split('.'),
        last = parts.pop(),
        len = parts.length,
        i = 1,
        current = parts[0];

    if (len > 0)
    {
        while ((obj = obj[current]) && i < len)
        {
            current = parts[i];
            i++;
        }
    }
    
    if (obj)
    {
        console.log("Updating bound item for property: " + propertyPath);
        obj[last] = value;
        return obj[last];
    }
}

function processPath(path, request, response)
{
    console.log("Processing path " + path);
    var state =
    {
        path: path,
        request: request,
        response: response,
    }

	var routeModule = routes[path];
    if (routeModule)
    {
        var boundItemsAfterUpdate = null;

        console.log("Found route module for " + path);

        if (request.body && request.body.BoundItems)
        {
            console.log("BoundItems: " + request.session.BoundItems);

            // Record the current state of bound items so we can diff it after apply the changes from the client,
            // and use that diff to see if there were any changes, so that we can then pass them to the OnChange
            // handler (for the "view" mode, indicating changes that were made by/on the view).
            //
            var boundItemsBeforeUpdate = JSON.parse(JSON.stringify(request.session.BoundItems));

            // Now apply the changes from the client...
            for (var key in request.body.BoundItems)
            {
                console.log("Request body bound item change from client - " + key + ": " + request.body.BoundItems[key]);
                setObjectProperty(request.session.BoundItems, key, request.body.BoundItems[key]);
            }

            // Getting this here allows us to track any changes made by server logic (in change notifications or commands)
            //
            boundItemsAfterUpdate = JSON.parse(JSON.stringify(request.session.BoundItems));

            // If we had changes from the view and we have a change listener for this route, call it.
            if (routeModule.OnChange)
            {
                // !!! Pass changelist (consistent with command side changelist)
                routeModule.OnChange(state, request.session, request.session.BoundItems, "view");
            }
        }

        var command = request.query["command"]
		if (command)
        {
            console.log("Running command: " + command);

            // If no bound item updates happened on the client, we need to record the state of the
            // bound items now, before we run any commands, so we can diff it after...
            // (this is really "BoundItems after update from client, if any" or "BoundItems before
            // any server code gets a crack at them).
            //
            if (!boundItemsAfterUpdate)
            {
                boundItemsAfterUpdate = JSON.parse(JSON.stringify(request.session.BoundItems));
            }

            if (!routeModule.Commands[command](state, request.session, request.session.BoundItems))
            {
                // !! This is a problem for non-default returns that route to the same page, such as MessageBox,
                //    which also needs to get the bound item update...
                //
                // If command returns false (default), it has not created a response, so we
                // create the default response (updating the bound items)...
                console.log("Default command processing, returning bound items");

                // If we have a change listener for this route, analyze changes, and call it as appropriate.
                if (routeModule.OnChange)
                {
                    // !!! We might need to call getChangeList here also, to determine if there were any changes,
                    //     and to construct the changelist for the handler.
                    // !!! Pass changelist (consistent with view side changelist)
                    routeModule.OnChange(state, request.session, request.session.BoundItems, "command");
                }

                var boundItemUpdates = objectMonitor.getChangeList(null, boundItemsAfterUpdate, request.session.BoundItems);
                response.send({ "BoundItemUpdates": boundItemUpdates });
            }
        }
        else
        {
            request.session.BoundItems = {};
            if (routeModule.InitializeBoundItems)
            {
                console.log("Initializing bound items");
                request.session.BoundItems = routeModule.InitializeBoundItems(state, request.session);
            }
            response.send({ "BoundItems": request.session.BoundItems, "View": routeModule.View });
        }
    }
}

function fnShowMessage(state, messageBox)
{
    state.response.send({ "BoundItems": state.request.session.BoundItems, "MessageBox": messageBox });
    return true;
}
declare var showMessage: any;
showMessage = fnShowMessage;

function fnNavigateToView(state, route)
{
    var routeModule = routes[route];
    if (routeModule)
    {
        console.log("Found route module for " + route);
        state.request.session.BoundItems = {};
        if (routeModule.InitializeBoundItems)
        {
            console.log("Initializing bound items (on nav)");
            state.request.session.BoundItems = routeModule.InitializeBoundItems(state, state.request.session);
        }
        state.response.send({ "BoundItems": state.request.session.BoundItems, "View": routeModule.View });
    }

    return true;
}
declare var navigateToView: any;
navigateToView = fnNavigateToView;

app.all('*', function (request, response)
{
    console.log("GET path: " + request.path);
    var path = request.path.substring(1);
    processPath(path, request, response);
});

app.listen(3000);
console.log('Server running at http://127.0.0.1:3000/');