# CoAP.Net [![Build status](https://img.shields.io/appveyor/ci/NZSmartie/coap-net-iu0to.svg?logo=appveyor)](https://ci.appveyor.com/project/NZSmartie/coap-net-iu0to) [![Tests Status](https://img.shields.io/appveyor/tests/NZSmartie/coap-net-iu0to.svg?logo=appveyor)](https://ci.appveyor.com/project/NZSmartie/coap-net-iu0to/build/tests) [![Coverage Status](https://coveralls.io/repos/github/NZSmartie/CoAP.Net/badge.svg?branch=master)](https://coveralls.io/github/NZSmartie/CoAP.Net?branch=master) [![NuGet](https://img.shields.io/nuget/v/NZSmartie.CoAPNet.svg)](https://www.nuget.org/packages/NZSmartie.CoAPNet/) [![MyGet Pre Release](https://img.shields.io/myget/coapnet/vpre/NZSmartie.CoapNet.svg?label=myget)](https://www.myget.org/feed/Packages/coapnet) [![license](https://img.shields.io/github/license/NZSmartie/CoAP.Net.svg)](https://github.com/NZSmartie/CoAP.Net/blob/master/LICENSE) 

## About

This library is a transport agnostic implementation of the Constraint Application Protocol (CoAP - RFC 7252) for .Net Standard.

Since CoAP is designed for unreliable transport layers. (6LoWPAN, UDP, etc...) it made sense to not worry about the transport implementaions and allow the applicatrion to provide their own.

If you're after a UDP transport example, see [CoAPNet.Udp](CoAPNet.Udp/)

## Changelog

All relevant changes are logged in [Changelog.md](Changelog.md)

## Status

 - Full support for [RFC 7959 - Block-Wise Transfers in the Constrained Application Protocol (CoAP)](https://tools.ietf.org/html/rfc7959)
 - Support for Sending and Receiving Confirmable (CON) and Non-Confirmable (NON) messagees.
   - Retransmit confirmable messages, throwing an exception on failure
   - Rejects malform messages with appropiate error code.
   - Ignores repeated messages within a set `TimeSpan`
 - Support for Sending and receiving multicast messages. 

 - `CoapServer` - Simple server for binding to local transports and processing requests 
   - `CoapHandler` - Template request handler to be used by CoapServer
     - `CoapResourceHandler` - Example handler that specificaly serves `CoapResource`s

### Todo

 - Create unit tests to cover as much of RFC7252 as possible.
 - Create more examples
 - Support DTLS over UDP
 - Resolve host names using mDNS or DNS
 - Await message response for non-confirmable (NON) messages
 - Support for [RFC 7641 - Observing Resources in the Constrained Application Protocol (CoAP)](https://tools.ietf.org/html/rfc7641)
 - Support for [RFC 7390 - Group Communication for the Constrained Application Protocol (CoAP)](https://tools.ietf.org/html/rfc7390)

## Stats

[![Build History](https://buildstats.info/appveyor/chart/NZSmartie/coap-net-iu0to?branch=master)](https://ci.appveyor.com/project/NZSmartie/coap-net-iu0to/history)


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
