using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Runtime.InteropServices;

namespace PolygonIo.WebSocket.Socket
{
    sealed class WebSocketReceiveResultProcessor : IDisposable
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

            var slice = buffer.Slice(0, result.Count);

            if (startChunk == null)
                startChunk = currentChunk = new Chunk<byte>(slice);
            else
                currentChunk = currentChunk.Add(slice);

            if (result.EndOfMessage && startChunk != null)
            {
                frame = startChunk.Next == null ?
                    new ReadOnlySequence<byte>(startChunk.Memory) : new ReadOnlySequence<byte>(startChunk, 0, currentChunk, currentChunk.Memory.Length);

                startChunk = currentChunk = null; // Reset so we can accept new chunks from scratch.
                return true;
            }
            else
            {
                frame = default;
                return false;
            }
        }

        public void Dispose()
        {
            // Release any partial decoded chunks.
            var chunk = startChunk;

            while (chunk != null)
            {
                if (MemoryMarshal.TryGetArray(chunk.Memory, out var segment))
                    ArrayPool<byte>.Shared.Return(segment.Array);

                chunk = (Chunk<byte>)chunk.Next;
            }

            // Suppress finalization.
            GC.SuppressFinalize(this);
        }
    }
}