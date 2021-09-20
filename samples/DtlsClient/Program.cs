using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoAPNet;
using CoAPNet.Dtls.Client;
using Org.BouncyCastle.Crypto.Tls;

namespace CoAPDevices
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = "localhost";
            var port = Coap.PortDTLS;
            var identity = new BasicTlsPskIdentity("user", Encoding.UTF8.GetBytes("password"));


            // Create a new client using a DTLS endpoint with the remote host and Identity
            var client = new CoapClient(new CoapDtlsClientEndPoint(host, port, new ExamplePskDtlsClient(identity)));
            // Create a cancelation token that cancels after 1 minute
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));

            // Capture the Control + C event
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Exiting");
                cancellationTokenSource.Cancel();

                // Prevent the Main task from being destroyed prematurely.
                e.Cancel = true;
            };

            Console.WriteLine("Press <Ctrl>+C to exit");

            try
            {
                // Create a simple GET request
                var message = new CoapMessage
                {
                    Code = CoapMessageCode.Get,
                    Type = CoapMessageType.Confirmable,
                };

                // Get the /hello resource from localhost.
                message.SetUri($"coaps://{host}:{port}/hello");

                Console.WriteLine($"Sending a {message.Code} {message.GetUri().GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped)} request");
                await client.SendAsync(message, cancellationTokenSource.Token);

                // Wait for the server to respond.
                var response = await client.ReceiveAsync(cancellationTokenSource.Token);

                // Output our response
                Console.WriteLine($"Received a response from {response.Endpoint}\n{Encoding.UTF8.GetString(response.Message.Payload)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception caught: {ex}");
            }
            finally
            {
                Console.WriteLine($"Press <Enter> to exit");
                Console.Read();
            }
        }

        public class ExamplePskDtlsClient : PskTlsClient
        {
            public ExamplePskDtlsClient(TlsPskIdentity pskIdentity)
                : base(pskIdentity)
            {
            }

            public ExamplePskDtlsClient(TlsCipherFactory cipherFactory, TlsPskIdentity pskIdentity)
                : base(cipherFactory, pskIdentity)
            {
            }

            public ExamplePskDtlsClient(TlsCipherFactory cipherFactory, TlsDHVerifier dhVerifier, TlsPskIdentity pskIdentity)
                : base(cipherFactory, dhVerifier, pskIdentity)
            {
            }

            public override ProtocolVersion MinimumVersion => ProtocolVersion.DTLSv10;
            public override ProtocolVersion ClientVersion => ProtocolVersion.DTLSv12;

            public override int GetHandshakeTimeoutMillis()
            {
                return 30000;
            }
        }
    }
}