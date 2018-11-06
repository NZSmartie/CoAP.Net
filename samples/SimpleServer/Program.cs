using System;
using System.Net;
using System.Text;
using System.Threading;
using CoAPNet;
using CoAPNet.Options;
using CoAPNet.Udp;
using CoAPNet.Server;
using System.Threading.Tasks;

namespace CoAPDevices
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Create a task that finishes when Ctrl+C is pressed
            var taskCompletionSource = new TaskCompletionSource<bool>();

            // Capture the Control + C event
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Exiting");
                taskCompletionSource.SetResult(true);

                // Prevent the Main task from being destroyed prematurely.
                e.Cancel = true;
            };

            Console.WriteLine("Press <Ctrl>+C to exit");

            // Create a resource handler.
            var myHandler = new CoapResourceHandler();
            // Register a /hello resource.
            myHandler.Resources.Add(new HelloResource("/hello"));

            // Create a new CoapServer using UDP as it's base transport
            var myServer = new CoapServer(new CoapUdpTransportFactory());

            try
            {
                // Listen to all ip address and subscribe to multicast requests.
                await myServer.BindTo(new CoapUdpEndPoint(Coap.Port) { JoinMulticast = true });

                // Start our server.
                await myServer.StartAsync(myHandler, CancellationToken.None);
                Console.WriteLine("Server Started!");

                // Wait indefinitely until the application quits.
                await taskCompletionSource.Task;
            }
            catch (Exception ex)
            {
                if(!(ex is TaskCanceledException))
                {
                    Console.WriteLine($"Exception caught: {ex}");

                    Console.WriteLine($"Press <Enter> to exit");
                    Console.Read();
                }
            }
            finally
            {
                Console.WriteLine("Shutting Down Server");
                await myServer.StopAsync(CancellationToken.None);
            }
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
            Console.WriteLine($"Got request: {request}");

            return new CoapMessage
            {
                Code = CoapMessageCode.Content,
                Options = { new ContentFormat(ContentFormatType.TextPlain) },
                Payload = Encoding.UTF8.GetBytes("Hello World!")
            };
        }
    }
}