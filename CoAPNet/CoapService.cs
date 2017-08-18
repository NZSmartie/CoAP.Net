using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoAPNet.Options;
using CoAPNet.Utils;

namespace CoAPNet
{
    public class CoapService : IDisposable
    {
        protected CoapClient Client { get; }

        public Uri BaseUri => Client.Endpoint.BaseUri;

        public StringComparison UriPathComparison { get; set; } = StringComparison.OrdinalIgnoreCase;

        public IList<CoapResource> Resources { get; } = new List<CoapResource>();

        public CoapService(ICoapEndpoint endpoint)
        {
            Client = new CoapClient(endpoint ?? throw new ArgumentNullException(nameof(endpoint)));
            Initialise();
        }

        public CoapService(CoapClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Initialise();
        }

        protected void Initialise()
        {
            Resources.Add(new CoapResource(new CoapResourceMetadata("/.well-known/core")
            {
                SuggestedContentTypes = {ContentFormatType.ApplicationLinkFormat}
            }));

            Client.Listen();
            Client.OnMessageReceived += Client_OnMessageReceived;
        }

        private async Task Client_OnMessageReceived(object sender, CoapMessageReceivedEventArgs e)
        {
            await ClientOnMessageReceivedAsync(e.Message, e.Endpoint);
        }

        protected async Task ClientOnMessageReceivedAsync(CoapMessage message, ICoapEndpoint endpoint)
        {
            //TODO: check if message is multicast, ignore Confirmable requests and delay response

            if (!message.Code.IsRequest())
                // Todo: send CoapMessageCode.Reset or ignore them, i dunno
                throw new NotImplementedException("Need to respond with a RESET or something");

            var resource = Resources.FirstOrDefault(r =>
                Uri.Compare(
                    new Uri(BaseUri, r.Metadata.UriReference),
                    message.GetUri(),
                    UriComponents.Path,
                    UriFormat.SafeUnescaped,
                    UriPathComparison) == 0);

            if (resource == null)
            {
                await Client.SendAsync(CoapMessageUtility.CreateMessage(CoapMessageCode.NotFound, $"Resouce {message.GetUri()} was not found"), endpoint);
                return;
            }

            CoapMessage result = null;
            try
            {
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (message.Code)
                {
                    case CoapMessageCode.Get:
                        result = resource.Get();
                        break;
                    case CoapMessageCode.Post:
                        result = resource.Post();
                        break;
                    case CoapMessageCode.Put:
                        result = resource.Put();
                        break;
                    case CoapMessageCode.Delete:
                        result = resource.Delete();
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
            catch (NotImplementedException ex)
            {
                result = CoapMessageUtility.CreateMessage(CoapMessageCode.NotImplemented, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                result = CoapMessageUtility.FromException(ex);
                throw;
            }
            finally
            {
                await Client.SendAsync(result, endpoint);
            }
        }

        public void Dispose()
        {
            Client.OnMessageReceived -= Client_OnMessageReceived;
            Client.Dispose();
        }
    }
}