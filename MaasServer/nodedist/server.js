///<reference path='..\node\node.d.ts' />
///<reference path='..\node\express.d.ts' />
///<reference path='..\node\require-dir.d.ts' />
var express = require("express");
var requireDir = require('require-dir');

var objectMonitor = require("./objectMonitor");

var routes = requireDir("routes");
for (var routePath in routes) {
    console.log("Found route processor for: " + routePath);
    var route = routes[routePath];
    route.View["path"] = routePath;
}

var app = express();
app.use(express.cookieParser());

// Note: Setting the maxAge value to 60000 (one hour) generates a cookie that .NET does not record (date generation/parsing
// is my guess) - for now we just omit expiration...
app.use(express.cookieSession({ secret: 'sdf89f89fd7sdf7sdf', cookie: { maxAge: false, httpOnly: true } }));
app.use(express.query());
app.use(express.json());
app.use("/resources", express.static("./resources"));

app.get('/', function (req, res) {
    res.send('No view route provided');
});

function getObjectProperty(obj, propertyPath) {
    propertyPath = propertyPath.replace(/\[(\w+)\]/g, '.$1');
    var parts = propertyPath.split('.'), last = parts.pop(), len = parts.length, i = 1, current = parts[0];

    if (len > 0) {
        while ((obj = obj[current]) && i < len) {
            current = parts[i];
            i++;
        }
    }

    if (obj) {
        return obj[last];
    }
}

function setObjectProperty(obj, propertyPath, value) {
    propertyPath = propertyPath.replace(/\[(\w+)\]/g, '.$1');
    var parts = propertyPath.split('.'), last = parts.pop(), len = parts.length, i = 1, current = parts[0];

    if (len > 0) {
        while ((obj = obj[current]) && i < len) {
            current = parts[i];
            i++;
        }
    }

    if (obj) {
        console.log("Updating bound item for property: " + propertyPath);
        obj[last] = value;
        return obj[last];
    }
}

function processPath(path, request, response) {
    console.log("Processing path " + path);
    var context = {
        path: path,
        request: request,
        response: response
    };

    var routeModule = routes[path];
    if (routeModule) {
        var boundItemsAfterUpdate = null;

        console.log("Found route module for " + path);

        if (request.body && request.body.BoundItems) {
            console.log("BoundItems: " + request.session.BoundItems);

            // Record the current state of bound items so we can diff it after apply the changes from the client,
            // and use that diff to see if there were any changes, so that we can then pass them to the OnChange
            // handler (for the "view" mode, indicating changes that were made by/on the view).
            //
            var boundItemsBeforeUpdate = JSON.parse(JSON.stringify(request.session.BoundItems));

            for (var key in request.body.BoundItems) {
                console.log("Request body bound item change from client - " + key + ": " + request.body.BoundItems[key]);
                setObjectProperty(request.session.BoundItems, key, request.body.BoundItems[key]);
            }

            // Getting this here allows us to track any changes made by server logic (in change notifications or commands)
            //
            boundItemsAfterUpdate = JSON.parse(JSON.stringify(request.session.BoundItems));

            if (routeModule.OnChange) {
                // !!! Pass changelist (consistent with command side changelist)
                routeModule.OnChange(context, request.session, request.session.BoundItems, "view");
            }
        }

        var command = request.query["command"];
        if (command) {
            console.log("Running command: " + command);

            if (!boundItemsAfterUpdate) {
                boundItemsAfterUpdate = JSON.parse(JSON.stringify(request.session.BoundItems));
            }

            if (!routeModule.Commands[command](context, request.session, request.session.BoundItems)) {
                // !! This is a problem for non-default returns that route to the same page, such as MessageBox,
                //    which also needs to get the bound item update...
                //
                // If command returns false (default), it has not created a response, so we
                // create the default response (updating the bound items)...
                console.log("Default command processing, returning bound items");

                if (routeModule.OnChange) {
                    // !!! We might need to call getChangeList here also, to determine if there were any changes,
                    //     and to construct the changelist for the handler.
                    // !!! Pass changelist (consistent with view side changelist)
                    routeModule.OnChange(context, request.session, request.session.BoundItems, "command");
                }

                var boundItemUpdates = objectMonitor.getChangeList(null, boundItemsAfterUpdate, request.session.BoundItems);
                response.send({ "BoundItemUpdates": boundItemUpdates });
            }
        } else {
            request.session.BoundItems = {};
            if (routeModule.InitializeViewModelState) {
                console.log("Initializing view model state");
                request.session.BoundItems = routeModule.InitializeViewModelState(context, request.session);
            }
            response.send({ "BoundItems": request.session.BoundItems, "View": routeModule.View });
        }
    }
}

function fnShowMessage(state, messageBox) {
    state.response.send({ "BoundItems": state.request.session.BoundItems, "MessageBox": messageBox });
    return true;
}

showMessage = fnShowMessage;

function fnNavigateToView(context, route) {
    var routeModule = routes[route];
    if (routeModule) {
        console.log("Found route module for " + route);
        context.request.session.BoundItems = {};
        if (routeModule.InitializeViewModelState) {
            console.log("Initializing view model state (on nav)");
            context.request.session.BoundItems = routeModule.InitializeViewModelState(context, context.request.session);
        }
        context.response.send({ "BoundItems": context.request.session.BoundItems, "View": routeModule.View });
    }

    return true;
}

navigateToView = fnNavigateToView;

app.all('*', function (request, response) {
    console.log("GET path: " + request.path);
    var path = request.path.substring(1);
    processPath(path, request, response);
});

app.listen(3000);
console.log('Server running at http://127.0.0.1:3000/');

//# sourceMappingURL=server.js.map
