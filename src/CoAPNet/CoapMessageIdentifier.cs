using System;
using System.Linq;
using System.Text;

namespace CoAPNet
{
    public static class CoapMessageIdentifierExtensions
    {
        public static CoapMessageIdentifier GetIdentifier(this CoapMessage message, ICoapEndpoint endpoint = null, bool isRequest = false)
            => new CoapMessageIdentifier(message, endpoint, isRequest);
    }

    public struct CoapMessageIdentifier
    {
        public readonly int Id;

        public readonly byte[] Token;

        public readonly CoapMessageType MessageType;

        public readonly ICoapEndpoint Endpoint;

        /// <summary>
        /// When <c>true</c>, the associated identifer is for a requesting <see cref="CoapMessage"/>.
        /// </summary>
        public readonly bool IsRequest;

        public CoapMessageIdentifier(CoapMessage message, ICoapEndpoint endpoint = null, bool isRequest = false)
        {
            Id = message.Id;
            MessageType = message.Type;
            Endpoint = endpoint;
            IsRequest = isRequest;

            // Even no tokens are treated as a zero-length token representing 0x0
            Token = new byte[message.Token?.Length ?? 0];

            if (message.Token != null)
                Array.Copy(message.Token, Token, message.Token.Length);
        }

        public CoapMessageIdentifier(int id, CoapMessageType messageType, in byte[] token = null, ICoapEndpoint endpoint = null, bool isRequest = false)
        {
            Id = id;
            MessageType = messageType;
            Endpoint = endpoint;
            IsRequest = isRequest;

            // Even no tokens are treated as a zero-length token representing 0x0
            Token = new byte[token?.Length ?? 0];

            if (token != null)
                Array.Copy(token, Token, token.Length);
        }

        public static bool operator ==(in CoapMessageIdentifier A, in CoapMessageIdentifier B)
        {
            // Only check endpoints if both are not null
            if (A.Endpoint != null && B.Endpoint != null && !A.Endpoint.Equals(B.Endpoint))
                return false;

            if (!A.Token.SequenceEqual(B.Token))
                return false;

            if (A.IsRequest && B.IsRequest)
                return A.Id == B.Id;

            // Only check the ID on a piggypacked response. Reponses that arrive after a an Acknowledge have a new ID
            if (((A.IsRequest && B.MessageType == CoapMessageType.Acknowledgement) ||
                 (B.IsRequest && A.MessageType == CoapMessageType.Acknowledgement)) && 
                 A.Id != B.Id)
                return false;

            return true;
        }
        
        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("<MessageID: {0}, Token: 0x{1}, Endpoint: {2}>", 
                Id, 
                Token.Length == 0 
                    ? "00" 
                    : string.Join("", Token.Select(t => t.ToString("X2"))),
                Endpoint == null ? "null" : Endpoint.ToString());
        }

        public static bool operator !=(in CoapMessageIdentifier A, in CoapMessageIdentifier B)
        {
            return !(A == B);
        }

        public bool Equals(in CoapMessageIdentifier other)
        {
            // See operator == (...) above
            return this == other;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            return obj is CoapMessageIdentifier identifier
                   && this == identifier;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var idHash = IsRequest || MessageType == CoapMessageType.Acknowledgement
                             ? Id * 55865
                             : 0;
                var tokenHash = Token.Aggregate(270, (a, t) => a + t.GetHashCode() * 21890);

                return tokenHash ^ idHash;
            }
        }
    }
}