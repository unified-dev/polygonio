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
    internal class PolygonDecoder
    {
        readonly TransformBlock<ReadOnlySequence<byte>, DeserializedData> decodeBlock;
        private readonly ILogger<PolygonDecoder> logger;

        public PolygonDecoder(IPolygonDeserializer polygonDeserializer, ILogger<PolygonDecoder> logger, Func<DeserializedData, Task> receiveMessages)
        {
            this.logger = logger;
            
            this.decodeBlock = GetDecodeBlock(polygonDeserializer);

            decodeBlock.LinkTo(new ActionBlock<DeserializedData>(async (deserializedData) =>
            {
                if (deserializedData != null)
                    await receiveMessages(deserializedData);
            }));            
        }

        public PolygonDecoder(IPolygonDeserializer polygonDeserializer, ILogger<PolygonDecoder> logger, ITargetBlock<DeserializedData> externalBlock)
        {
            this.logger = logger;

            this.decodeBlock = GetDecodeBlock(polygonDeserializer);
            decodeBlock.LinkTo(externalBlock);
        }

        TransformBlock<ReadOnlySequence<byte>, DeserializedData> GetDecodeBlock(IPolygonDeserializer polygonDeserializer)
        {
            return new TransformBlock<ReadOnlySequence<byte>, DeserializedData>((data) =>
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