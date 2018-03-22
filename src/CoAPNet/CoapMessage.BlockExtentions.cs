using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CoAPNet;
using CoAPNet.Options;

namespace CoAPNet
{
    public static class CoapMessageBlockExtensions
    {
        /// <summary>
        /// Checks if a <see cref="CoapMessage"/> is part ofa block-wise transfer by checking for the presence of either <see cref="CoAPNet.Options.Block1"/> or <see cref="CoAPNet.Options.Block2"/>.
        /// </summary>
        /// <param name="message"></param>
        /// <returns><c>true</c> when <paramref name="message"/> is part of a Block-Wise transfer</returns>
        public static bool IsBlockWise(this CoapMessage message)
        {
            return message.Options.Any(o => o.OptionNumber == CoapRegisteredOptionNumber.Block1 || o.OptionNumber == CoapRegisteredOptionNumber.Block2);
        }

        /// <summary>
        /// Attempts to read the entire body of the block-wise message. Using the <paramref name="originalRequest"/> to request blocks.
        /// </summary>
        /// <param name="message">A message containing a <see cref="Block2"/> option.</param>
        /// <param name="client"></param>
        /// <param name="originalRequest">The orignal request which the block-wise response was for.</param>
        /// <returns>The completed body for the block-wise messages.</returns>
        public static byte[] GetCompletedBlockWisePayload(this CoapMessage message, CoapClient client, CoapMessage originalRequest)
        {
            var block2 = message.Options.Get<Options.Block2>()
                         ?? throw new ArgumentException($"{nameof(CoapMessage)} does not contain a {nameof(Options.Block2)} option", nameof(message));

            if (originalRequest == null)
                throw new ArgumentNullException("Please provide original requesting message", nameof(originalRequest));

            if (block2.BlockNumber != 0)
                throw new CoapBlockException($"Can not get completed payload starting with block {block2.BlockNumber}. Please start from 0");

            var memoryStream = new MemoryStream();

            using (var reader = new CoapBlockStreamReader(client, message, originalRequest))
                reader.CopyTo(memoryStream);

            return memoryStream.ToArray();

        }
    }
}