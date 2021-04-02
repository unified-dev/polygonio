using System;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PolygonIo.WebSocket.Socket
{
    static class ClientWebSocketExtensions
    {
        static public async Task SendAsChunksAsync(this ClientWebSocket webSocket, string message, int sendChunkSize, CancellationToken cancellationToken)
        {
            await webSocket.SendAsChunksAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, sendChunkSize, cancellationToken);
        }

        static public async Task SendAsChunksAsync(this ClientWebSocket webSocket, byte[] data, WebSocketMessageType type, int sendChunkSize, CancellationToken cancellationToken)
        {
            var messagesCount = (int)Math.Ceiling((double)data.Length / sendChunkSize);

            for (var i = 0; i < messagesCount; i++)
            {
                var offset = (sendChunkSize * i);
                var count = sendChunkSize;
                var lastMessage = ((i + 1) == messagesCount);

                if ((count * (i + 1)) > data.Length)
                {
                    count = data.Length - offset;
                }

                await webSocket
                        .SendAsync(new ArraySegment<byte>(data, offset, count), type, lastMessage, cancellationToken);
            }
        }

        static public async Task ReceiveFramesLoopAsync(this ClientWebSocket webSocket, Func<byte[],Task> target, int receiveChunkSize, CancellationToken cancellationToken)
        {
            var buffer = new ArraySegment<byte>(new byte[receiveChunkSize]);

            do
            {
                WebSocketReceiveResult result;
                byte[] resultArrayWithTrailing = null;
                var resultArraySize = 0;
                var isResultArrayCloned = false;
                MemoryStream ms = null;

                while (true)
                {
                    result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                    var currentChunk = buffer.Array;
                    var currentChunkSize = result.Count;

                    var isFirstChunk = resultArrayWithTrailing == null;

                    if (isFirstChunk)
                    {
                        // first chunk, use buffer as reference, do not allocate anything
                        resultArraySize += currentChunkSize;
                        resultArrayWithTrailing = currentChunk;
                        isResultArrayCloned = false;
                    }
                    else if (currentChunk == null)
                    {
                        // weird chunk, do nothing
                    }
                    else
                    {
                        // received more chunks, merge them via memory stream
                        if (ms == null)
                        {
                            // create memory stream and insert first chunk
                            ms = new MemoryStream();
                            ms.Write(resultArrayWithTrailing, 0, resultArraySize);
                        }

                        ms.Write(currentChunk, buffer.Offset, currentChunkSize); // insert current chunk
                    }

                    if (result.EndOfMessage)
                        break;

                    if (isResultArrayCloned)
                        continue;

                    // we got more chunks incoming, need to clone first chunk
                    resultArrayWithTrailing = resultArrayWithTrailing?.ToArray();
                    isResultArrayCloned = true;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }
                else
                {
                    if (ms != null)
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        await target(ms.ToArray());
                        ms.Dispose();
                    }
                    else
                    {
                        Array.Resize(ref resultArrayWithTrailing, resultArraySize);
                        await target(resultArrayWithTrailing);
                    }
                }
            }
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested);
        }
    }
}