# Change Log for CoAP.Net 
 
### v0.7.0

 - [`be5a77ac`](https://github.com/NZSmartie/CoAP.Net/commit/be5a77ac) Use original request in CoapBlockStreamReader
 - [`a543c9fa`](https://github.com/NZSmartie/CoAP.Net/commit/a543c9fa) Correct the byte order in Block1/Block2 as it was backwards
 - [`1455010b`](https://github.com/NZSmartie/CoAP.Net/commit/1455010b) Add BlockSWiseContext to share context between reading and writing blockwise operations
 - [`7bae697b`](https://github.com/NZSmartie/CoAP.Net/commit/7bae697b) Split CoapBlockStream into a Reader and Writer and make tests more modular
 - [`52a52e5d`](https://github.com/NZSmartie/CoAP.Net/commit/52a52e5d) Override Equal method in Block1/Block2
 - [`48ae94c3`](https://github.com/NZSmartie/CoAP.Net/commit/48ae94c3) Split CoapBlockStream into a Reader and Writer and make tests more modular
 - [`6b533e27`](https://github.com/NZSmartie/CoAP.Net/commit/6b533e27) Use Latest C# language (7.1)
 - [`0bcc74b9`](https://github.com/NZSmartie/CoAP.Net/commit/0bcc74b9) Override CoapBlockStream.WriteAsync to reduce buffer size while writing on slow transports
 - [`f46b15b8`](https://github.com/NZSmartie/CoAP.Net/commit/f46b15b8) Add Equality to ContentFormatType
 - [`81031ae8`](https://github.com/NZSmartie/CoAP.Net/commit/81031ae8) Add baseclass for each CoapOption type to support easier type matching Options
 - [`a88e3065`](https://github.com/NZSmartie/CoAP.Net/commit/a88e3065) Fix Null excpetions being thrown when comparing CoapOptions with null values
 - [`fe8f8928`](https://github.com/NZSmartie/CoAP.Net/commit/fe8f8928) Expose SupportBlockSizes in Block1/2 and add documentation
 - [`5a0b28b4`](https://github.com/NZSmartie/CoAP.Net/commit/5a0b28b4) Restore original ToString functionality and add debuggable format
 - [`0da5483a`](https://github.com/NZSmartie/CoAP.Net/commit/0da5483a) Fix CoapMesasgeIdentifier not matching when endpoints are differnt instances.
 - [`47f3a85a`](https://github.com/NZSmartie/CoAP.Net/commit/47f3a85a) Add Value and Name property to ContentFormatType

### v0.6.0

 - [`d7c9d9c4`](https://github.com/NZSmartie/CoAP.Net/commit/d7c9d9c4) Add CancellationToken argument to ICoapTransport Async methods

### v0.5.2

 - [`11841803`](https://github.com/NZSmartie/CoAP.Net/commit/11841803) Perform a stable sort on CoapMessage.Options 

### v0.5.1
 
 - [`802fde24`](https://github.com/NZSmartie/CoAP.Net/commit/802fde24) Bumping netstandard down from 1.4 to 1.3
 - [`aaa6239d`](https://github.com/NZSmartie/CoAP.Net/commit/aaa6239d) Add extension methods for reading BlockWise messages
 - [`7954c95b`](https://github.com/NZSmartie/CoAP.Net/commit/7954c95b) Allow reading CoapBlockStream if data is still buffered.

### v0.5.0
 
 - [`a38ad662`](https://github.com/NZSmartie/CoAP.Net/commit/a38ad662) Only finish flushing when there's nothing that can be immediatly written
 - [`5af3fc57`](https://github.com/NZSmartie/CoAP.Net/commit/5af3fc57) Attempt to read as much as possible before returning
 - [`dedc10aa`](https://github.com/NZSmartie/CoAP.Net/commit/dedc10aa) Fix human-error NullReferenceException in CoapClockStream
 - [`a1aea35b`](https://github.com/NZSmartie/CoAP.Net/commit/a1aea35b) Support reading block-wise from remote endpoint
 - [`fbf58787`](https://github.com/NZSmartie/CoAP.Net/commit/fbf58787) Add Block2/Block2 metadata to CoapMessage.ToString()
 - [`8868be8f`](https://github.com/NZSmartie/CoAP.Net/commit/8868be8f) Housekeeping in Blockstream
 - [`69c5c2e2`](https://github.com/NZSmartie/CoAP.Net/commit/69c5c2e2) Document Block1/Block2.BlockNumber
 - [`a9e27bd5`](https://github.com/NZSmartie/CoAP.Net/commit/a9e27bd5) Add minimal documentation where it's missing
 - [`8e5f96c0`](https://github.com/NZSmartie/CoAP.Net/commit/8e5f96c0) Document CoapBlockStream
 - [`cdfe0944`](https://github.com/NZSmartie/CoAP.Net/commit/cdfe0944) Remove deadlock when disposing CoapBlockStream and introduve Timout
 - [`5ed538d0`](https://github.com/NZSmartie/CoAP.Net/commit/5ed538d0) Flush out the stream if there is any data
 - [`5fcb12fd`](https://github.com/NZSmartie/CoAP.Net/commit/5fcb12fd) Only attempt to restart smaller Block-Wise transfer on first block
 - [`ab0549b6`](https://github.com/NZSmartie/CoAP.Net/commit/ab0549b6) Attempt to send Block-Wise messages in smaller sizes on RequestEntittyTooLarge
 - [`f13b3ec8`](https://github.com/NZSmartie/CoAP.Net/commit/f13b3ec8) Allow advancing ByteQueue and Peeking blocks of data
 - [`7e0bc60e`](https://github.com/NZSmartie/CoAP.Net/commit/7e0bc60e) Hold onto values and decode/encode them only when needed.
 - [`0e7219a9`](https://github.com/NZSmartie/CoAP.Net/commit/0e7219a9) Exclude CoapBlockException from code coverage
 - [`c2b6d446`](https://github.com/NZSmartie/CoAP.Net/commit/c2b6d446) Support reduced block sizes in BlockWise Transfer
 - [`439ca4e0`](https://github.com/NZSmartie/CoAP.Net/commit/439ca4e0) Block on Flush and throw exceptions when writing in CoapBlockStream
 - [`461c4e4e`](https://github.com/NZSmartie/CoAP.Net/commit/461c4e4e) Begin support for RFC 7959 Block-Wise Transfers

### v0.4.0

 - [`fab984bc`](https://github.com/NZSmartie/CoAP.Net/commit/fab984bc) Heh, Debug.Assert gets removed in release builds...
 - [`f6883b66`](https://github.com/NZSmartie/CoAP.Net/commit/f6883b66) Rename static method to prevent hidden overloading
 - [`f3019ff0`](https://github.com/NZSmartie/CoAP.Net/commit/f3019ff0) Improve response matching with CoapMessageIdentifier
 - [`768f50fa`](https://github.com/NZSmartie/CoAP.Net/commit/768f50fa) Better request and response matching in CoapClient
 - [`71cffd71`](https://github.com/NZSmartie/CoAP.Net/commit/71cffd71) Add minimal documentation where it's missing
 - [`74c4d6b0`](https://github.com/NZSmartie/CoAP.Net/commit/74c4d6b0) Add locks around CoapClient.Endpoint to prevent it being disposed unexpectedly
 - [`3a5d05cb`](https://github.com/NZSmartie/CoAP.Net/commit/3a5d05cb) Return a placeholder option to give the application chance at reading them
 - [`f9eb87d2`](https://github.com/NZSmartie/CoAP.Net/commit/f9eb87d2) Include CoapMessageCode in CoapException.FromCoapMessage
 - [`a900f1a7`](https://github.com/NZSmartie/CoAP.Net/commit/a900f1a7) Add Reset to AsyncAutoResetEvent
 - [`1c1c5c8d`](https://github.com/NZSmartie/CoAP.Net/commit/1c1c5c8d) Document CoapMessageCode properties
 - [`30ea8d24`](https://github.com/NZSmartie/CoAP.Net/commit/30ea8d24) Create CoapException from CoapMessage
 - [`30d06ee7`](https://github.com/NZSmartie/CoAP.Net/commit/30d06ee7) Throw a CoapEndpointException instead of InvalidOperationException
 - [`47d31153`](https://github.com/NZSmartie/CoAP.Net/commit/47d31153) Reject Confirmable Empty messages
 - [`c5da5f2d`](https://github.com/NZSmartie/CoAP.Net/commit/c5da5f2d) Ignore Acknowledge with invalid Code Class
 - [`7272c345`](https://github.com/NZSmartie/CoAP.Net/commit/7272c345) Ignore non-empty reset messages in CoapClient
 - [`4b1958da`](https://github.com/NZSmartie/CoAP.Net/commit/4b1958da) Ignore repeated messages within a set TimeSpan on CoapClient
 - [`ccabaab3`](https://github.com/NZSmartie/CoAP.Net/commit/ccabaab3) Forgot to add CoapMessageCodes back in
 - [`0ac5f88a`](https://github.com/NZSmartie/CoAP.Net/commit/0ac5f88a) Better exceptions for FromBytes
 - [`4436a9f3`](https://github.com/NZSmartie/CoAP.Net/commit/4436a9f3) Rename Serialise and Deserialise to ToBytes and FromBytes respectively
 - [`a99134c8`](https://github.com/NZSmartie/CoAP.Net/commit/a99134c8) Make OptionFactory a object

### v0.3.10

 - [`0c6788e1`](https://github.com/NZSmartie/CoAP.Net/commit/0c6788e1) Give a chance to throw message exceptions after deserialising
 - [`2c5ef861`](https://github.com/NZSmartie/CoAP.Net/commit/2c5ef861) Account for SendAsyncInternal dealing with a volatile Endpoint Property
 - [`6ed148a8`](https://github.com/NZSmartie/CoAP.Net/commit/6ed148a8) Use ConcurrentQueue for message queuing in async

### v0.3.9

 - [`4945c5b`](https://github.com/NZSmartie/CoAP.Net/commit/4945c5b) Perform DNS queries on hostnames in CoapUdpEndpoint

### v0.3.8

 - [`c3b44ee`](https://github.com/NZSmartie/CoAP.Net/commit/c3b44ee) Default to multicast address in CoapUdpEndpoint
 - [`2f453f5`](https://github.com/NZSmartie/CoAP.Net/commit/2f453f5) Allow selectively setting Uri componetns in CoapMessage.SetUri
 - [`b8ef59a`](https://github.com/NZSmartie/CoAP.Net/commit/b8ef59a) Sending multicast CoAP messages are checked appropiately
 - [`e131344`](https://github.com/NZSmartie/CoAP.Net/commit/e131344) Replace FromUri with SetUri

### v0.3.7

 - [`a2cdf94`](https://github.com/NZSmartie/CoAP.Net/commit/a2cdf94) Retarget .Net Standard 1.4 and 2.0 and export XML Doc

### v0.3.6

 - [`96b59c6`](https://github.com/NZSmartie/CoAP.Net/commit/96b59c6) CoAPNet.Udp support joinging IPv4 multicast groups
 - [`a16eeb9`](https://github.com/NZSmartie/CoAP.Net/commit/a16eeb9) Improve CoRE Link-Format (de)serialising and deprecate `rev`

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
