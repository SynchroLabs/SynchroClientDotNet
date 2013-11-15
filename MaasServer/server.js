//var connect = require('connect');
var express = require('express');
var requireDir = require('require-dir');

var app = express();

app.use(express.cookieParser());
// Note: Setting the maxAge value to 60000 (one hour) generates a cookie that .NET does not record (date generation/parsing
// is my guess) - for now we just omit expiration...
app.use(express.session({ secret: 'sdf89f89fd7sdf7sdf', cookie: { maxAge: false, httpOnly: true }}));
app.use(express.query());
app.use(express.json());
app.use("/resources", express.static("./resources"));

var routes = requireDir('./routes');
for (var routePath in routes)
{
	console.log("Found route processor for: " + routePath);
	var route = routes[routePath];

	route.View["Path"] = routePath;

	if (!route.BoundItems)
	{
		route.BoundItems = {};
	}
}

processPath = function (path, request, response)
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
		console.log("Found route module for " + path);

		if (request.body && request.body.BoundItems)
		{
			for (key in request.body.BoundItems)
			{
				console.log("Request body bound item - " + key + ": " + request.body.BoundItems[key]);
				request.session.BoundItems[key] = request.body.BoundItems[key];
			}
		}

		var command = request.query["command"] 
		if (command)
		{
			console.log("Running command: " + command);
			if (!routeModule.Commands[command](state, request.session, request.session.BoundItems))
			{
				// If command returns false (default), it has not created a response, so we
				// create the default response (updating the bound items)...
				console.log("Default command processing, returning bound items");
				response.send({"BoundItems": request.session.BoundItems});
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
			response.send({"BoundItems": request.session.BoundItems, "View" : routeModule.View});
		}
	}
}

showMessage = function(state, messageBox)
{
	state.response.send({"BoundItems": state.request.session.BoundItems, "MessageBox": messageBox});
	return true;
}

navigateToView = function(state, route)
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
		state.response.send({"BoundItems": state.request.session.BoundItems, "View" : routeModule.View});
	}

	return true;
}

app.all('*', function(request, response)
{
	console.log("GET path: " + request.path);
	var path = request.path.substring(1);
	processPath(path, request, response);
});

app.listen(3000);
