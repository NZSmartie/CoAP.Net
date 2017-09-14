 # Change Log for CoAP.Net 

### v0.3.5

 - [`0c1e1cf`](https://github.com/NZSmartie/CoAP.Net/commit/0c1e1cf) Respond appropiately to unsupported content format option

### v0.3.4

 - [`c6111c0`](https://github.com/NZSmartie/CoAP.Net/commit/c6111c0) Split server classes into CoAPNet.Server to maintain zero dependency on CoAPNet
 - [`c03d7af`](https://github.com/NZSmartie/CoAP.Net/commit/c03d7af) Use logging in server/client related classes
 - [`885d0d2`](https://github.com/NZSmartie/CoAP.Net/commit/885d0d2) Include full uri in CORELink format when Authoraty does not match.

### v0.3.3

 - [`22ecd43`](https://github.com/NZSmartie/CoAP.Net/commit/22ecd43) Include Async handling of requests from CoapHandler and CoapResource

### v0.3.1

 - [`b3069b6`](https://github.com/NZSmartie/CoAP.Net/commit/b3069b6) Added Apache License 2.0
 
### v0.3.0

 - [`18e1f87`](https://github.com/NZSmartie/CoAP.Net/commit/8e1f873) Improve async operations in CoapClient
 - [`1059530`](https://github.com/NZSmartie/CoAP.Net/commit/0595303) Remove CancellationToken from ICoapEndpoint
 - [`45d9715`](https://github.com/NZSmartie/CoAP.Net/commit/45d9715) Correctly decode uint options from bytes 
 
### v0.2.6
   
 - [`98dad09`](https://github.com/NZSmartie/CoAP.Net/commit/98dad09) Add Contructor overloads to CoapUdpEndPoint for simplicity 
 - [`4480168`](https://github.com/NZSmartie/CoAP.Net/commit/4480168) Include IP address in Coap Options.
 - [`4480168`](https://github.com/NZSmartie/CoAP.Net/commit/4480168) Create CoapEndpoint for unknown endpoints

### v0.2.4

 - [`9037770`](https://github.com/NZSmartie/CoAP.Net/commit/9037770) Implemented a UDP example to use with NZSmartie.CoAPNet
 - [`5340553`](https://github.com/NZSmartie/CoAP.Net/commit/5340553) Support building Core Link Format resources from CoapResourceMetadata

### v0.2.3

 - [`8e547da`](https://github.com/NZSmartie/CoAP.Net/commit/8e547da) Implement better exception handling in CoapHandler
 - [`330ade3`](https://github.com/NZSmartie/CoAP.Net/commit/330ade3) Throw CoapOptionException when deserialising  unsupported critical options

### v0.2.2

 - [`98e879f`](https://github.com/NZSmartie/CoAP.Net/commit/98e879f) Pass the requesting CoapMessage into Get/Put/Post/Delete methods in CoapResource

### v0.2.1

 - [`f568528`](https://github.com/NZSmartie/CoAP.Net/commit/f568528) Support message retransmission in CoapClient
 
### v0.2.0

 - [`069e9dc`](https://github.com/NZSmartie/CoAP.Net/commit/069e9dc) Added (still Work in Progress) CoapHandler and CoapService with tests for handling incomming requests.
 
### v0.1.1

  - [`f63717d`](https://github.com/NZSmartie/CoAP.Net/commit/f63717d) Improve concurrency in CoapClient
  - [`4165523`](https://github.com/NZSmartie/CoAP.Net/commit/4165523) Rename CoapResource to CoapResourceMetadata
