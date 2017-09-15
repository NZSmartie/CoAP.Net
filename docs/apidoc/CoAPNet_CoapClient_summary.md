---
uid: CoAPNet.CoapClient
summary: *content
---

Connecting and interacting with remote CoAP endpoints can be achieved through creating a new instance of @CoAPNet.CoapClient and @CoAPNet.ICoapEndpoint.

> [!NOTE]
> As it's required to implement @CoAPNet.ICoapEndpoint, An application can use @CoAPNet.Udp.CoapUdpEndPoint as a base example for getting started for your application's transport needs.

### Example

A new @CoAPNet.CoapClient is created using a @CoAPNet.Udp.CoapUdpEndPoint for transport over a UDP interface. Creating a new GET request for `/hello` to the remote endpoint `coap://127.0.0.1`. The result is written to the console before finishing.
    
```C#
using CoAPNet;
using CoAPNet.Udp;
  
// ...
  
public async Task RequestHello()
{
    // Create a new client using a UDP endpoint (defaults to 0.0.0.0 with any available port)
    var client = new CoapClient(new CoapUdpEndPoint());
  
    // New requests are assigned a numeric message Id by CoapClient
    var messageId = await client.GetAsync("coap://127.0.0.1/hello");
  
    // Wait for a response to our specific message Id
    var response = await client.GetResponseAsync(messageId);
  
    Console.WriteLine("got a response");
    Console.WriteLine(Encoding.UTF8.GetString(response.Payload));
}
```