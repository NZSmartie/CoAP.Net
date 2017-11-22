using System;
using System.Net;
using System.Text;
using System.Threading;
using CoAPNet;
using CoAPNet.Options;
using CoAPNet.Udp;
using CoAPNet.Server;

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
                myServer.BindTo(new CoapUdpEndPoint(IPAddress.Any, Coap.Port) { JoinMulticast = true });

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
                Options = { new ContentFormat(ContentFormatType.TextPlain) },
                Payload = Encoding.UTF8.GetBytes("Hello World!")
            };
        }
    }
}