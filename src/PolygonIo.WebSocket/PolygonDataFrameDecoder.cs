using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using PolygonIo.WebSocket.Deserializers;
using System.Threading.Tasks.Dataflow;

namespace PolygonIo.WebSocket
{
    internal class PolygonDataFrameDecoder
    {
        readonly TransformBlock<ReadOnlySequence<byte>, PolygonDataFrame> decodeBlock;
        private readonly ILogger<PolygonDataFrameDecoder> logger;

        public PolygonDataFrameDecoder(IPolygonDeserializer polygonDeserializer, ILogger<PolygonDataFrameDecoder> logger, Func<PolygonDataFrame, Task> receiveMessages)
        {
            this.logger = logger;
            
            this.decodeBlock = GetDecodeBlock(polygonDeserializer);

            decodeBlock.LinkTo(new ActionBlock<PolygonDataFrame>(async (deserializedData) =>
            {
                if (deserializedData != null)
                    await receiveMessages(deserializedData);
            }));            
        }

        public PolygonDataFrameDecoder(IPolygonDeserializer polygonDeserializer, ILogger<PolygonDataFrameDecoder> logger, ITargetBlock<PolygonDataFrame> externalBlock)
        {
            this.logger = logger;

            this.decodeBlock = GetDecodeBlock(polygonDeserializer);
            decodeBlock.LinkTo(externalBlock);
        }

        TransformBlock<ReadOnlySequence<byte>, PolygonDataFrame> GetDecodeBlock(IPolygonDeserializer polygonDeserializer)
        {
            return new TransformBlock<ReadOnlySequence<byte>, PolygonDataFrame>((data) =>
            {
                try
                {
                    return polygonDeserializer.Deserialize(data);
                }
                catch (Exception e)
                {
                    this.logger.LogError($"Error deserializing '{Encoding.UTF8.GetString(data.ToArray())}' ({e}).");
                    return null;
                }
            });
        }


        public async Task SendAsync(ReadOnlySequence<byte> bytes)
        {
            await this.decodeBlock.SendAsync(bytes);
        }

        public async Task Stop()
        {
            // stop processing frames
            decodeBlock.Complete();
            await decodeBlock.Completion;
        }
    }
}