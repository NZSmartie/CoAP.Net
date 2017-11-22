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

            // Create a new client using a UDP endpoint (defaults to 0.0.0.0 with any available port number)
            var client = new CoapClient(new CoapUdpEndPoint());

            var message = new CoapMessage
            {
                //IsMulticast = true,
                Code = CoapMessageCode.Get,
                Type = CoapMessageType.NonConfirmable,
            };

            message.SetUri("coap://localhost/hello");

            client.SendAsync(message).Wait();

            var response = client.ReceiveAsync().Result;

            Console.WriteLine("got a response");
            Console.WriteLine(Encoding.UTF8.GetString(response.Message.Payload));

            Console.WriteLine("Press <Enter> to exit");
            Console.ReadLine();

        }
    }
}