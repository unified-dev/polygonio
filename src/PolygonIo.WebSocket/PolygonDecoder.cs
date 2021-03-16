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
        readonly ActionBlock<DeserializedData> outputBlock;

        public PolygonDecoder(IPolygonDeserializer polygonDeserializer, ILogger<PolygonDecoder> logger, Func<DeserializedData, Task> receiveMessages)
        {
            this.decodeBlock = new TransformBlock<ReadOnlySequence<byte>, DeserializedData>((data) =>
            {
                try
                {
                    return polygonDeserializer.Deserialize(data);
                }
                catch (Exception e)
                {
                    logger.LogError($"Error deserializing '{Encoding.UTF8.GetString(data.ToArray())}' ({e}).");
                    return null;
                }
            });

            outputBlock = new ActionBlock<DeserializedData>(async (deserializedData) =>
            {
                if (deserializedData != null)
                    await receiveMessages(deserializedData);
            });

            decodeBlock.LinkTo(outputBlock);
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