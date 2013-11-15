///<reference path='node\node.d.ts' />
///<reference path='node\express.d.ts' />
var express = require("express");

var app = express.createServer();
app.use(express.cookieParser());
app.use(express.cookieSession({ secret: "xxxxx" }));
app.use(express.query());
app.use(express.json());

app.get('/', function (req, res) {
    res.send('Hello World from Express');
});

app.listen(3000);
console.log('Server running at http://127.0.0.1:3000/');

//# sourceMappingURL=test_server.js.map
