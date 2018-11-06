using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoAPNet;
using CoAPNet.Udp;

namespace CoAPDevices
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Create a new client using a UDP endpoint (defaults to 0.0.0.0 with any available port number)
            var client = new CoapClient(new CoapUdpEndPoint());
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
                message.SetUri("coap://localhost/hello");

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
    }
}