using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoAPNet.Utils;

namespace CoAPNet
{
    public class CoapResourceHandler : CoapHander
    {
        public readonly IList<CoapResource> Resources = new List<CoapResource>();

        public override CoapResource GetResource(Uri requesstUri)
        {
            return Resources.FirstOrDefault(r =>
                Uri.Compare(
                    new Uri(BaseUri, r.Metadata.UriReference),
                    requesstUri,
                    UriComponents.Path,
                    UriFormat.SafeUnescaped,
                    UriPathComparison) == 0);
        }

        public override Uri BaseUri { get; }

        public CoapResourceHandler()
            : this(new Uri("coap://localhost/"))
        { }

        public CoapResourceHandler(Uri baseUri)
        {
            BaseUri = baseUri;
        }
    }

    public class CoapHander : ICoapHandler
    {
        public virtual CoapResource GetResource(Uri requesstUri)
        {
            throw new NotImplementedException();
        }

        public StringComparison UriPathComparison { get; set; } = StringComparison.Ordinal;

        public virtual Uri BaseUri { get; } = new Uri("coap://localhost/");

        public async Task ProcessRequestAsync(ICoapConnectionInformation connection, byte[] payload)
        {
            var message = new CoapMessage();
            message.Deserialise(payload);

            //TODO: check if message is multicast, ignore Confirmable requests and delay response

            if (!message.Code.IsRequest())
            {
                // TODO: send CoapMessageCode.Reset or ignore them, i dunno
                throw new NotImplementedException("TODO: Send CoapMessageCode.Reset or ignore them");
            }

            var resource = GetResource(new Uri(BaseUri, message.GetUri()));
            CoapMessage result = null;
            try
            {
                if (resource == null)
                {
                    result = CoapMessageUtility.CreateMessage(CoapMessageCode.NotFound,
                        $"Resouce {message.GetUri()} was not found");
                }
                else
                {
                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (message.Code)
                    {
                        case CoapMessageCode.Get:
                            result = resource.Get(message);
                            break;
                        case CoapMessageCode.Post:
                            result = resource.Post(message);
                            break;
                        case CoapMessageCode.Put:
                            result = resource.Put(message);
                            break;
                        case CoapMessageCode.Delete:
                            result = resource.Delete(message);
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }
            catch (NotImplementedException ex)
            {
                result = CoapMessageUtility.CreateMessage(CoapMessageCode.NotImplemented, ex.Message);
                //Debug.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                result = CoapMessageUtility.FromException(ex);
                //Debug.Fail(ex.Message);
            }
            finally
            {
                Debug.Assert(result != null);

                await connection.LocalEndpoint.SendAsync(
                    new CoapPacket
                    {
                        Endpoint = connection.RemoteEndpoint,
                        Payload = result.Serialise()
                    },
                    CancellationToken.None);
            }
        }
    }
}