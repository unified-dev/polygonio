using System;
using System.Buffers;
using System.Linq;
using System.Net.WebSockets;

namespace PolygonIo.WebSocket
{
    class WebSocketReceiveResultProcessor
    {
        Chunk<byte> startChunk = null;
        Chunk<byte> currentChunk = null;

        public bool Receive(WebSocketReceiveResult result, ArraySegment<byte> buffer, out ReadOnlySequence<byte> frame)
        {
            if (result.EndOfMessage && result.MessageType == WebSocketMessageType.Close)
            {
                frame = default;
                return false;
            }

            var slice = buffer.Slice(0, result.Count).ToArray(); // take a local copy to avoid corruption as buffer is reused by caller

            if (startChunk == null)
                startChunk = currentChunk = new Chunk<byte>(slice);
            else
                currentChunk = currentChunk.Add(slice);
            
            if(result.EndOfMessage && startChunk != null)
            {
                if (startChunk.Next == null)
                    frame = new ReadOnlySequence<byte>(startChunk.Memory);
                else
                    frame = new ReadOnlySequence<byte>(startChunk, 0, currentChunk, currentChunk.Memory.Length);

                startChunk = null; // reset so we can accept new chunks from scratch
                return true;
            }
            else
            {
                frame = default;
                return false;
            }
        }    
    }
}