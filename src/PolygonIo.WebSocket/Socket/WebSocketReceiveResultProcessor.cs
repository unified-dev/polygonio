using System;
using System.Buffers;
using System.Linq;
using System.Net.WebSockets;
using System.IO;
using System.Collections.Generic;

namespace PolygonIo.WebSocket
{
    class WebSocketReceiveResultProcessor
    {
        Chunk<byte> startChunk = null;
        Chunk<byte> currentChunk = null;

        public bool Receive(WebSocketReceiveResult result, ArraySegment<byte> buffer, out byte[] frame)
        {
            if (result.EndOfMessage && result.MessageType == WebSocketMessageType.Close)
            {
                frame = null;
                return false;
            }

            var slice = buffer.Slice(0, result.Count);

            if (startChunk == null)
                startChunk = currentChunk = new Chunk<byte>(slice);
            else
                currentChunk = currentChunk.Add(slice);
            
            if(result.EndOfMessage && startChunk != null)
            {
                if (startChunk.Next == null)
                    frame = startChunk.Memory.ToArray();
                else
                {
                    var sequence = new ReadOnlySequence<byte>(startChunk, 0, currentChunk, currentChunk.Memory.Length);
                    frame = sequence.ToArray();
                }
                startChunk = null; // reset so we can accept new chunks from scratch
                return true;
            }
            else
            {
                frame = null;
                return false;
            }
        }    
    }
}