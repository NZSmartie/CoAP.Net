using System;
using System.Linq;
using System.Text;

namespace CoAPNet
{
    public static class CoapMessageIdentifierExtensions
    {
        public static CoapMessageIdentifier GetIdentifier(this CoapMessage message, ICoapEndpoint endpoint = null)
            => new CoapMessageIdentifier(message, endpoint);
    }

    public struct CoapMessageIdentifier
    {
        public readonly int Id;

        public readonly byte[] Token;

        public readonly CoapMessageType MessageType;

        public readonly ICoapEndpoint Endpoint;

        public CoapMessageIdentifier(CoapMessage message, ICoapEndpoint endpoint = null)
        {
            Id = message.Id;
            MessageType = message.Type;
            Endpoint = endpoint;

            // Even no tokens are treated as a zero-length token representing 0x0
            Token = new byte[message.Token?.Length ?? 0];

            if (message.Token != null)
                Array.Copy(message.Token, Token, message.Token.Length);
        }

        public CoapMessageIdentifier(int id, CoapMessageType messageType, byte[] token = null, ICoapEndpoint endpoint = null)
        {
            Id = id;
            MessageType = messageType;
            Endpoint = endpoint;

            // Even no tokens are treated as a zero-length token representing 0x0
            Token = new byte[token?.Length ?? 0];

            if (token != null)
                Array.Copy(token, Token, token.Length);
        }

        public static bool operator ==(CoapMessageIdentifier A, CoapMessageIdentifier B)
        {
            // Only check endpoints if both are not null
            if (A.Endpoint != null && B.Endpoint != null && A.Endpoint != B.Endpoint)
                return false;

            if (!A.Token.SequenceEqual(B.Token))
                return false;

            // Only check the ID on a piggypacked response. Reponses that arrive after a an Acknowledge have a new ID
            if (((A.MessageType == CoapMessageType.Confirmable && B.MessageType == CoapMessageType.Acknowledgement) ||
                 (B.MessageType == CoapMessageType.Confirmable && A.MessageType == CoapMessageType.Acknowledgement)) && 
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

        public static bool operator !=(CoapMessageIdentifier A, CoapMessageIdentifier B)
        {
            return !(A == B);
        }

        public bool Equals(CoapMessageIdentifier other)
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
                return Token.Aggregate(270, (a, t) => a + t.GetHashCode() * 21890);
            }
        }
    }
}