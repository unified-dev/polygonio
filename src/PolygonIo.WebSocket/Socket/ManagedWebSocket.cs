using System;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PolygonIo.WebSocket.Socket
{
    public class ManagedWebSocket
    {
        private const int ReceiveChunkSize = 2048;
        private const int SendChunkSize = 2048;

        Task lifetimeTask;
        bool isConnected;
        bool isRunning;
        TimeSpan ReconnectTime { get; set; } = TimeSpan.FromSeconds(20);
        TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(20);

        private ClientWebSocket webSocket;
        private readonly Uri uri;
        private readonly ITargetBlock<byte[]> target;
        private CancellationTokenSource cancellationTokenSource;

        public Func<Task> OnConnectedAsync { get; set; } = null;
        public Func<Task> OnDisconnectedAsync { get; set; } = null;

        public ManagedWebSocket(string uri, ITargetBlock<byte[]> target)
        {
            this.uri = new Uri(uri);
            this.target = target;
        }

        public async Task SendAsync(string message)
        {
            if (isConnected == false)
            {
                throw new Exception("Connection is not open.");
            }

            var messageBuffer = Encoding.UTF8.GetBytes(message);
            var messagesCount = (int)Math.Ceiling((double)messageBuffer.Length / SendChunkSize);

            for (var i = 0; i < messagesCount; i++)
            {
                var offset = (SendChunkSize * i);
                var count = SendChunkSize;
                var lastMessage = ((i + 1) == messagesCount);

                if ((count * (i + 1)) > messageBuffer.Length)
                {
                    count = messageBuffer.Length - offset;
                }

                await webSocket.SendAsync(new ArraySegment<byte>(messageBuffer, offset, count), WebSocketMessageType.Text, lastMessage, this.cancellationTokenSource.Token);
            }
        }

        public void Start()
        {
            if (isRunning)
                return;
            else
                isRunning = true;

            if (this.cancellationTokenSource != null)
                this.cancellationTokenSource.Dispose();
            this.cancellationTokenSource = new CancellationTokenSource();

            this.lifetimeTask = Task.Run(async () => await LifetimeTask());
        }

        public async Task Stop()
        {
            this.cancellationTokenSource.Cancel();
            await this.lifetimeTask;
        }

        private async Task LifetimeTask()
        {
            while (cancellationTokenSource.Token.IsCancellationRequested == false)
            {
                try
                {
                    this.webSocket = new ClientWebSocket();
                    webSocket.Options.KeepAliveInterval = KeepAliveInterval;
                    await this.webSocket.ConnectAsync(this.uri, cancellationTokenSource.Token);
                    this.isConnected = true;
                    await OnConnectedAsync();
                    await ReadAsync(target);                    
                }
                catch (WebSocketException ex)
                {
                    await OnDisconnectedAsync();
                }
                finally
                {
                    webSocket.Dispose();
                }
                await Task.Delay(this.ReconnectTime, cancellationTokenSource.Token);
            }
        }

        private async Task ReadAsync(ITargetBlock<byte[]> target)
        {
            var buffer = new ArraySegment<byte>(new byte[ReceiveChunkSize]);

            do
            {
                WebSocketReceiveResult result;
                byte[] resultArrayWithTrailing = null;
                var resultArraySize = 0;
                var isResultArrayCloned = false;
                MemoryStream ms = null;

                while (true)
                {
                    result = await webSocket.ReceiveAsync(buffer, cancellationTokenSource.Token);
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
                        // received more chunks, lets merge them via memory stream
                        if (ms == null)
                        {
                            // create memory stream and insert first chunk
                            ms = new MemoryStream();
                            ms.Write(resultArrayWithTrailing, 0, resultArraySize);
                        }

                        // insert current chunk
                        ms.Write(currentChunk, buffer.Offset, currentChunkSize);
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
                    await OnDisconnectedAsync();
                }
                else
                {
                    if (ms != null)
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        await target.SendAsync(ms.ToArray());
                        ms.Dispose();
                    }
                    else
                    {
                        Array.Resize(ref resultArrayWithTrailing, resultArraySize);
                        await target.SendAsync(resultArrayWithTrailing);
                    }
                }
            }
            while (webSocket.State == WebSocketState.Open && !this.cancellationTokenSource.IsCancellationRequested);
        }
    }
}