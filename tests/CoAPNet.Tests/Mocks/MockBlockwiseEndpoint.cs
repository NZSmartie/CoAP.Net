using CoAPNet.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoAPNet.Tests.Mocks
{
    public enum MockBlockwiseEndpointMode
    {
        ReduceBlockSize,
        RequestTooLarge
    }

    public class MockBlockwiseEndpoint : MockEndpoint
    {
        public CoapMessage BaseResponse { get; set; }

        public CoapMessage FinalResponse { get; set; }

        public MockBlockwiseEndpointMode Mode { get; set; }

        public int BlockSize { get; set; } 
            = Options.BlockBase.SupportedBlockSizes.Max();

        public int TotalBytes { get; set; }

        public MockBlockwiseEndpoint(CoapMessage baseResponse, int blockSize, int totalBytes)
        {
            BaseResponse = baseResponse;
            BlockSize = blockSize;
            TotalBytes = totalBytes;

            FinalResponse = baseResponse;
        }

        public override Task MockSendAsync(CoapPacket packet, CancellationToken token)
        {
            var request = CoapMessage.CreateFromBytes(packet.Payload);
            var block = request.Options.Get<Block1>() as BlockBase 
                ?? request.Options.Get<Block2>();

            var blockNumber = block != null
                ? block.BlockNumber
                : 0;
            var from = block != null
                ? (block.BlockNumber * block.BlockSize)
                : 0;
            var to = block!= null 
                ? Math.Min(from + block.BlockSize,TotalBytes)
                : BlockSize;

            CoapMessage response;
            if (block is Block1)
            {
                if (block.BlockSize > BlockSize)
                {
                    response = BaseResponse.Clone();
                    switch (Mode)
                    {
                        case MockBlockwiseEndpointMode.ReduceBlockSize:
                            response.Options.Add(new Options.Block1(0, BlockSize, true));
                            break;
                        case MockBlockwiseEndpointMode.RequestTooLarge:
                            response.Code = CoapMessageCode.RequestEntityTooLarge;
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                else
                {
                    response = block.IsMoreFollowing 
                        ? BaseResponse.Clone() 
                        : FinalResponse.Clone();

                    if (!block.IsMoreFollowing)
                    {
                        response.Options.Add(new Options.Block2(0, BlockSize, TotalBytes > BlockSize ));
                        response.Payload = ByteRange(0, BlockSize);
                    }

                    response.Options.Add(new Options.Block1(block.BlockNumber, block.BlockSize, block.IsMoreFollowing));
                    response.Code = CoapMessageCode.Continue;
                }
            }
            else //if (block is Block2)
            {
                if (block != null && block.BlockSize < BlockSize)
                {
                    BlockSize = block.BlockSize;
                    to = from + BlockSize;
                }

                response = FinalResponse.Clone();

                response.Options.Add(new Block2(blockNumber, BlockSize, to < TotalBytes));
                response.Payload = ByteRange(from, to);
            }

            response.Id = request.Id;
            response.Token = request.Token;
            response.Type = CoapMessageType.Acknowledgement;

            EnqueueReceivePacket(new CoapPacket { Payload = response.ToBytes() });

            return Task.CompletedTask;

        }

        public static byte[] ByteRange(int from, int to)
            => Enumerable.Range(from, to - from).Select(i => Convert.ToByte(i % (byte.MaxValue + 1))).ToArray();
    }
}
