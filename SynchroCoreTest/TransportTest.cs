﻿using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SynchroCore;
using System.Threading.Tasks;
using System.Threading;

namespace SynchroCoreTest
{
    [TestClass]
    public class TransportTest
    {
        string testEndpoint = "http://localhost:1337/api/samples";

        [TestMethod]
        public async Task TestGetAppDefinition()
        {
            var expected = new JObject()
            {
                { "name", new JValue("synchro-samples") },
                { "version", new JValue("0.0.1") },
                { "description", new JValue("Synchro API Samples") },
                { "main", new JValue("menu") },
                { "author", new JValue("Bob Dickinson <bob@synchro.io> (http://synchro.io/)") },
                { "private", new JValue(true) },
                { "engines", new JObject()
                    {
                        { "synchro", new JValue("*") }
                    }
                }
            };
    
            var transport = new TransportHttp(uri: new Uri(testEndpoint));
        
            var actual = await transport.getAppDefinition();

            Assert.IsTrue(expected.DeepEquals(actual));
        }
        
        [TestMethod]
        public async Task TestGetFirstPage()
        {
            var transport = new TransportHttp(uri: new Uri(testEndpoint));

            AutoResetEvent AsyncCallComplete = new AutoResetEvent(false);

            JObject theResponse = null;
            await transport.sendMessage(
                null,
                requestObject: new JObject()
                {
                    { "Mode", new JValue("Page") },
                    { "Path", new JValue("menu") },
                    { "TransactionId", new JValue(1) }
                },
                responseHandler: (response) =>
                {
                    // A failed assert here will cause the failure handler to get called, which obscures the root cause
                    // of the test failure.  So we just record the response, and do our asserts after the await returns.
                    //
                    theResponse = response;
                    AsyncCallComplete.Set();
                },
                requestFailureHandler: (request, error) =>
                {
                    Assert.Fail("Unexpected error from sendMessage");
                    AsyncCallComplete.Set();
                }
            );

            AsyncCallComplete.WaitOne();

            Assert.AreEqual("menu", (string)theResponse["Path"]);
        }

        [TestMethod]
        public async Task TestNavigateToPageViaCommand()
        {
            var transport = new TransportHttp(uri: new Uri(testEndpoint));

            AutoResetEvent AsyncCallComplete = new AutoResetEvent(false);

            JObject theResponse = null;        
            await transport.sendMessage(
                null,
                requestObject: new JObject()
                {
                    { "Mode", new JValue("Page") },
                    { "Path", new JValue("menu") },
                    { "TransactionId", new JValue(1)},
                    { "DeviceMetrics", new JObject(){{"clientVersion", new JValue("1.1.0")}}}
                },
                responseHandler: (response) =>
                {
                    theResponse = response;
                    AsyncCallComplete.Set();
                },
                requestFailureHandler: (request, error) =>
                {
                    Assert.Fail("Unexpected error from sendMessage");
                    AsyncCallComplete.Set();
                }
            );

            AsyncCallComplete.WaitOne();

            Assert.AreEqual("menu", (string)theResponse["Path"]);

            var sessionId = (string)theResponse["NewSessionId"];
            var instanceId = (int)theResponse["InstanceId"];
            var instanceVersion = (int)theResponse["InstanceVersion"];

            AsyncCallComplete.Reset();

            JObject theResponse2 = null;
            await transport.sendMessage(
                sessionId,
                requestObject: new JObject()
                {
                    { "Mode", new JValue("Command") },
                    { "Path", new JValue("menu") },
                    { "TransactionId", new JValue(2) },
                    { "InstanceId", new JValue(instanceId) },
                    { "InstanceVersion", new JValue(instanceVersion) },
                    { "Command", new JValue("goToView") },
                    { "Parameters", new JObject(){ { "view", new JValue("hello") } } }
                },
                responseHandler: (response) =>
                {
                    theResponse2 = response;
                    AsyncCallComplete.Set();
                },
                requestFailureHandler: (request, error) =>
                {
                    Assert.Fail("Unexpected error from sendMessage");
                    AsyncCallComplete.Set();
                }
            );

            AsyncCallComplete.WaitOne();

            Assert.AreEqual("hello", (string)theResponse2["Path"]);
        }


        [TestMethod]
        public async Task TestHttp404Failure()
        {
            var transport = new TransportHttp(uri: new Uri("http://localhost:1337"));

            AutoResetEvent AsyncCallComplete = new AutoResetEvent(false);

            Exception theError = null;
            await transport.sendMessage(
                null,
                requestObject: new JObject()
                {
                    { "Mode", new JValue("Page") },
                    { "Path", new JValue("menu") },
                    { "TransactionId", new JValue(1) }
                },
                responseHandler: (response) =>
                {
                    Assert.Fail("Unexpected success from sendMessage");
                    AsyncCallComplete.Set();
                },
                requestFailureHandler: (request, error) =>
                {
                    theError = error;
                    AsyncCallComplete.Set();
                }
            );

            AsyncCallComplete.WaitOne();

            Assert.AreEqual(404, theError.Data["statusCode"]);
        }

        [TestMethod]
        public async Task TestNetworkFailure()
        {
            var transport = new TransportHttp(uri: new Uri("http://nohostcanbefoundhere"));

            AutoResetEvent AsyncCallComplete = new AutoResetEvent(false);

            Exception theError = null;
            await transport.sendMessage(
                null,
                requestObject: new JObject()
                {
                    { "Mode", new JValue("Page") },
                    { "Path", new JValue("menu") },
                    { "TransactionId", new JValue(1) }
                },
                responseHandler: (response) =>
                {
                    Assert.Fail("Unexpected success from sendMessage");
                    AsyncCallComplete.Set();
                },
                requestFailureHandler: (request, error) =>
                {
                    theError = error;
                    AsyncCallComplete.Set();
                }
            );
        
            AsyncCallComplete.WaitOne();

            // -1 means you didn't get a response, which I guess is good enough for now...
            //
            Assert.AreEqual(-1, theError.Data["statusCode"]);
        }

        [TestMethod]
        public void TestUriFromHostString()
        {
            Assert.AreEqual(TransportHttp.UriFromHostString("foo/app"), "http://foo/app");
            Assert.AreEqual(TransportHttp.UriFromHostString("http://foo/app"), "http://foo/app");
            Assert.AreEqual(TransportHttp.UriFromHostString("https://foo/app"), "https://foo/app");
        }
    }
}
