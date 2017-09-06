# CoAP.Net [![Build status](https://ci.appveyor.com/api/projects/status/ku2x7p5eo2yf4lls?svg=true)](https://ci.appveyor.com/project/NZSmartie/coap-net-iu0to) [![Coverage Status](https://coveralls.io/repos/github/NZSmartie/CoAP.Net/badge.svg?branch=master)](https://coveralls.io/github/NZSmartie/CoAP.Net?branch=master) [![NuGet](https://img.shields.io/nuget/v/NZSmartie.CoAPNet.svg)](https://www.nuget.org/packages/NZSmartie.CoAPNet/)


## About

This library encodes and decodes CoAP protocol packets that is transport agnostic. 
IT also provides a CoapClient and CoapServer for communicating over CoAP

Since CoAP is designed for unreliable transport layers. (6LoWPAN, UDP, etc...) it made sense to not worry about the transport implementaions and allow the applicatrion to provide their own.

If you're after a UDP transport example, see [CoAPNet.Udp](CoAPNet.Udp/) ([![NuGet](https://img.shields.io/nuget/v/NZSmartie.CoAPNet.Udp.svg)](https://www.nuget.org/packages/NZSmartie.CoAPNet.Udp/))

## Changelog

All relevant changes are logged in [Changelog.md](Changelog.md)

## Status

 - `CoapClient` - Simple client for communicating over the CoAP protocol
   - [X] Send messages
     - [ ] Resolve host names using mDNS or DNS
     - [X] Proper multicast handling
     - [X] Retransmit confirmable (CON) messages, throwing an exception on failure
     - [X] Await message response for confirmable (CON) messages.
     - [ ] Await message response for non-confirmable (NON) messages
   - [X] Receive messages 
     - [X] Rejects messages when 
       - [X] Malform messages with appropiate error code.
       - [X] Exceptions are thrown during processing
   - [X] Correctly parse CoAP packets

 - `CoapServer` - Simple server for binding to local transports and processing requests
 
 - `CoapHandler` - Template request handler to be used by CoapServer
   - `CoapResourceHandler` - Example handler that specificaly serves `CoapResource`s

### Todo

 - Create unit tests to cover as much of RFC7252 as possible.
 - An application layer friendly API (This is a very low level library at the time of writing)

## Examples

### Client Example

```C#
using System;
using System.Text;
using System.Threading.Tasks;
using CoAPNet;
using CoAPNet.Udp;

namespace CoAPDevices
{
    class Program
    {
        static void Main(string[] args)
        {

            Task.Run(async () =>
            {
                // Create a new client using a UDP endpoint (defaults to 0.0.0.0 with any available port number)
                var client = new CoapClient(new CoapUdpEndPoint());

                var messageId = await client.GetAsync("coap://127.0.0.1/hello");

                var response = await client.GetResponseAsync(messageId);

                Console.WriteLine("got a response");
                Console.WriteLine(Encoding.UTF8.GetString(response.Payload));
            }).GetAwaiter().GetResult();

            Console.WriteLine("Press <Enter> to exit");
            Console.ReadLine();

        }
    }
}
```

### Server example

```C#
using System;
using System.Net;
using System.Text;
using System.Threading;
using CoAPNet;
using CoAPNet.Options;
using CoAPNet.Udp;

namespace CoAPDevices
{
    class Program
    {
        static void Main(string[] args)
        {
            var failed = false;

            var myHandler = new CoapResourceHandler();

            myHandler.Resources.Add(new HelloResource("/hello"));

            var myServer = new CoapServer(new CoapUdpTransportFactory());

            try
            {
                myServer.BindTo(new CoapUdpEndPoint(IPAddress.Loopback, Coap.Port));
                myServer.BindTo(new CoapUdpEndPoint(IPAddress.IPv6Loopback, Coap.Port));

                myServer.StartAsync(myHandler, CancellationToken.None).GetAwaiter().GetResult();

                Console.WriteLine("Server Started!");

                Console.WriteLine("Press <Enter> to exit");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                failed = true;
                Console.WriteLine($"{ex.GetType().Name} occured: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                Console.WriteLine("Shutting Down Server");
                myServer.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            }

            if (failed)
                Console.ReadLine();
        }
    }

    public class HelloResource : CoapResource
    {
        public HelloResource(string uri) : base(uri)
        {
            Metadata.InterfaceDescription.Add("read");

            Metadata.ResourceTypes.Add("message");
            Metadata.Title = "Hello World";
        }

        public override CoapMessage Get(CoapMessage request)
        {
            return new CoapMessage
            {
                Code = CoapMessageCode.Content,
                Options = {new ContentFormat(ContentFormatType.TextPlain)},
                Payload = Encoding.UTF8.GetBytes("Hello World!")
            };
        }
    }
}
```