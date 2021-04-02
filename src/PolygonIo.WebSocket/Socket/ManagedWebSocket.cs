using Microsoft.Extensions.Logging;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace PolygonIo.WebSocket.Socket
{
    public class ManagedWebSocket : IDisposable
    {
        readonly StateMachine stateMachine;
        private const int ReceiveChunkSize = 2048;
        private const int SendChunkSize = 2048;

        Task backgroundReaderTask;
        TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(15);

        private ClientWebSocket webSocket;
        private readonly Uri uri;
        private readonly ITargetBlock<byte[]> target;
        private readonly ILogger<ManagedWebSocket> logger;
        private CancellationTokenSource cancellationTokenSource;
        bool isDisposing;

        private readonly Func<Task> notifyOnConnected;
        private readonly Func<Task> notifyOnDisconnected;

        public ManagedWebSocket(string uri, Func<Task> onConnected, Func<Task> onDisconnected, ITargetBlock<byte[]> target, ILogger<ManagedWebSocket> logger)
        {
            this.stateMachine = new StateMachine(OnDisconnected, OnConnecting, OnConnected, OnResetting);
            this.notifyOnConnected = onConnected;
            this.notifyOnDisconnected = onDisconnected;
            this.uri = new Uri(uri);
            this.target = target;
            this.logger = logger;
        }

        private void OnConnecting()
        {
            this.backgroundReaderTask = Task.Run(async () =>
            {
                this.logger.LogInformation($"{nameof(ManagedWebSocket.OnConnecting)}");
                this.cancellationTokenSource = new CancellationTokenSource();
                this.webSocket = new ClientWebSocket();
                webSocket.Options.KeepAliveInterval = KeepAliveInterval;

                try
                {
                    await this.webSocket.ConnectAsync(this.uri, cancellationTokenSource.Token);
                }
                catch(WebSocketException ex)
                {
                    this.logger.LogError(ex.Message, ex);
                    await this.stateMachine.FireAsync(Trigger.Reset);
                }

                this.stateMachine.Fire(Trigger.SetConnectComplete);
            });
        }
        
        private void OnDisconnected()
        {
            this.logger.LogInformation($"{nameof(ManagedWebSocket.OnDisconnected)} with {nameof(isDisposing)}={isDisposing}");
            if (isDisposing == false)
                this.stateMachine.Fire(Trigger.Connect);
        }

        private void OnResetting()
        {
            this.logger.LogInformation($"{nameof(ManagedWebSocket.OnResetting)}");
            FreeResources();
            this.stateMachine.Fire(Trigger.SetResetComplete);
        }

        private void OnConnected()
        {
            this.logger.LogInformation($"{nameof(ManagedWebSocket.OnConnected)}");
            this.backgroundReaderTask = Task.Run(BackgroundReaderTask);
        }

        public async Task SendAsync(string message)
        {
            await SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text);
        }

        public async Task SendAsync(byte[] data, WebSocketMessageType webSocketMessageType)
        {
            if (this.stateMachine.State != State.Connected)
                throw new Exception($"{nameof(ManagedWebSocket)} is not yet connected.");

            try
            {
                await webSocket
                        .SendAsChunksAsync(data, webSocketMessageType, SendChunkSize, this.cancellationTokenSource.Token)
                        .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, ex.Message);
                await this.stateMachine.FireAsync(Trigger.Reset);
            }
        }

        public void Start()
        {
            this.stateMachine.Fire(Trigger.Start);
        }
     
        private async Task BackgroundReaderTask()
        {
            try
            {
                this.logger.LogInformation($"{nameof(ManagedWebSocket.BackgroundReaderTask)}");
                await notifyOnConnected();

                // returns when the internal frame loop completes with websocket close, or by throwing an exception
                await this.webSocket.ReceiveFramesLoopAsync(target.SendAsync, ReceiveChunkSize, this.cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex.Message, ex);
                await notifyOnDisconnected();
            }

            await this.stateMachine.FireAsync(Trigger.Reset);
        }     

        public void FreeResources()
        {
            this.logger.LogDebug($"{nameof(ManagedWebSocket.FreeResources)}");
            this.cancellationTokenSource?.Cancel();
            this.backgroundReaderTask?.Wait();
            this.cancellationTokenSource?.Dispose();
            this.backgroundReaderTask?.Dispose();
        }

        public void Dispose()
        {
            if (isDisposing)
                return;

            isDisposing = true;

            this.logger.LogDebug($"{nameof(ManagedWebSocket.FreeResources)}");
            this.cancellationTokenSource?.Cancel();
            this.backgroundReaderTask?.Wait();
            this.cancellationTokenSource?.Dispose();
            this.backgroundReaderTask?.Dispose();
        }
    }
}