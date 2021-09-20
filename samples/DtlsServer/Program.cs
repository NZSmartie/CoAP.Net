using System;
using System.Net;
using System.Text;
using System.Threading;
using CoAPNet;
using CoAPNet.Options;
using CoAPNet.Server;
using System.Threading.Tasks;
using CoAPNet.Dtls.Server;
using Org.BouncyCastle.Crypto.Tls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

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

            var loggerFactory = LoggerFactory.Create(x => x.AddConsole());

            // Create a new Dtls transport factory.
            var myTransportFactory = new CoapDtlsServerTransportFactory(loggerFactory, new ExampleDtlsServerFactory());

            // Create a new CoapServer using DTLS as it's base transport
            var myServer = new CoapServer(myTransportFactory, loggerFactory.CreateLogger<CoapServer>());

            try
            {
                // Listen to all ip address and subscribe to multicast requests.
                await myServer.BindTo(new CoapDtlsServerEndPoint(IPAddress.IPv6Any, Coap.PortDTLS));

                // Start our server.
                await myServer.StartAsync(myHandler, CancellationToken.None);
                Console.WriteLine("Server Started!");

                // Wait indefinitely until the application quits.
                await taskCompletionSource.Task;
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
            finally
            {
                Console.WriteLine("Shutting Down Server");
                await myServer.StopAsync(CancellationToken.None);
            }
        }
    }

    public class ExamplePskDtlsServer : PskTlsServer
    {
        private readonly ExamplePskIdentityManager pskIdentityManager;

        public ExamplePskDtlsServer(ExamplePskIdentityManager pskIdentityManager)
            : base(pskIdentityManager)
        {
            this.pskIdentityManager = pskIdentityManager;
        }

        public override int GetHandshakeTimeoutMillis()
        {
            return 30000;
        }

        public string GetIdentity()
        {
            return this.pskIdentityManager.GetIdentity();
        }

        protected override ProtocolVersion MinimumVersion => ProtocolVersion.DTLSv10;
        protected override ProtocolVersion MaximumVersion => ProtocolVersion.DTLSv12;
    }

    public class ExamplePskIdentityManager : TlsPskIdentityManager
    {
        private string identity;

        public byte[] GetHint()
        {
            // you can return information about the server here so the client can select the correct key
            return new byte[0];
        }

        public byte[] GetPsk(byte[] identity)
        {
            var identityString = Encoding.UTF8.GetString(identity);
            this.identity = identityString;

            // if we know this user (by their identity), we return their encryption key
            // in production, you would probably use some kind of configuration or database here
            if (identityString == "user")
            {
                return Encoding.UTF8.GetBytes("password");
            }

            // if we don't know this user, we return null and the connection fails
            return null;
        }

        public string GetIdentity()
        {
            return identity;
        }
    }

    public class ExampleDtlsServerFactory : IDtlsServerFactory
    {
        public TlsServer Create()
        {
            // in this case, the identity manager should not be reused, as it stores state about the connection (in this case, the identity)
            return new ExamplePskDtlsServer(new ExamplePskIdentityManager());
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

        public override async Task<CoapMessage> GetAsync(CoapMessage request, ICoapConnectionInformation connectionInformation)
        {
            Console.WriteLine($"Got request: {request}");

            if(connectionInformation is CoapDtlsConnectionInformation dtlsConnectionInformation)
            {
                // this is a dtls connection

                // here, we know the server must be our ExamplePskDtlsServer so we can just cast it and call the GetIdentity method.
                var server = (ExamplePskDtlsServer)dtlsConnectionInformation.TlsServer;
                var identity = server.GetIdentity();
                return new CoapMessage
                {
                    Code = CoapMessageCode.Content,
                    Options = { new ContentFormat(ContentFormatType.TextPlain) },
                    Payload = Encoding.UTF8.GetBytes($"Hello {identity}!")
                };
            }
            else
            {
                // this is not a dtls connection
                // this will never be called in this example, but you could use a resource for both CoAP and CoAPS and decide here.

                return new CoapMessage
                {
                    Code = CoapMessageCode.Content,
                    Options = { new ContentFormat(ContentFormatType.TextPlain) },
                    Payload = Encoding.UTF8.GetBytes($"Hello World!")
                };
            }
        }
    }
}