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
            var cancellationTokenSource = new CancellationTokenSource();

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
                // Run a Send task and Receive task concurrently
                await Task.WhenAny(
                    SendAsync(client, cancellationTokenSource.Token),
                    ReceiveAsync(client, cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                // Canceled tasks are expected, safe to ignore.
                if (ex is TaskCanceledException)
                    return;

                Console.WriteLine($"Exception caught: {ex}");
                Console.WriteLine($"Press <Enter> to exit");
                Console.Read();
            }
        }

        static async Task SendAsync(CoapClient client, CancellationToken token)
        {
            var message = new CoapMessage
            {
                // IsMulticast will instruct the client to send the message to the multicast address on our behalf.
                IsMulticast = true,
                Code = CoapMessageCode.Get,
                Type = CoapMessageType.NonConfirmable,
            };

            // We want to request /.well-known/core from all CoAP servers.
            message.SetUri("/.well-known/core", UriComponents.PathAndQuery);

            do
            {
                Console.WriteLine($"Sending a multicast {message.Code} {message.GetUri().GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped)} request");
                await client.SendAsync(message, token);

                // Delay sending another request by a minute
                await Task.Delay(TimeSpan.FromMinutes(1), token);
            }
            while (!token.IsCancellationRequested); // Loop until canceled.
        }

        static async Task ReceiveAsync(CoapClient client, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Wait indefinitely until any message is received.
                var response = await client.ReceiveAsync(token);

                Console.WriteLine($"Received a response from {response.Endpoint}\n{Encoding.UTF8.GetString(response.Message.Payload)}");
            }
        }
    }
}