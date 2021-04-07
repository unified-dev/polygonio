using System;
using System.Buffers;
using System.Linq;
using System.Net.WebSockets;
using System.IO;

namespace PolygonIo.WebSocket
{
    class WebSocketReceiveResultProcessor
    {
        byte[] resultArrayWithTrailing;
        int resultArraySize;
        bool isResultArrayCloned;
        MemoryStream ms;

        public void Reset()
        {
            ms?.Dispose();
            resultArrayWithTrailing = null;
            resultArraySize = 0;
            isResultArrayCloned = false;
            ms = null;
        }

        public bool Receive(WebSocketReceiveResult result, ArraySegment<byte> buffer, out byte[] frame)
        {
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
            {
                frame = result.MessageType == WebSocketMessageType.Close ? null : GetFrame();
                return true;
            }
            
            if (isResultArrayCloned == false)
            {
                // we got more chunks incoming, need to clone first chunk
                resultArrayWithTrailing = resultArrayWithTrailing?.ToArray();
                isResultArrayCloned = true;
            }

            frame = null;
            return false;
        }

        byte[] GetFrame()
        {
            if (ms != null)
            {
                ms.Seek(0, SeekOrigin.Begin);
                return ms.ToArray();
            }
            else
            {
                Array.Resize(ref resultArrayWithTrailing, resultArraySize);
                return resultArrayWithTrailing;
            }
        }
    }
}