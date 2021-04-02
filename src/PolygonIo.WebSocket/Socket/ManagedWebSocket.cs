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

        Task lifetimeTask;
        TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(20);

        private ClientWebSocket webSocket;
        private readonly Uri uri;
        private readonly ITargetBlock<byte[]> target;
        private readonly ILogger<ManagedWebSocket> logger;
        private CancellationTokenSource cancellationTokenSource;

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
            this.lifetimeTask = Task.Run(async () =>
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
                    await this.stateMachine.FireAsync(Trigger.Reset);
                }
                this.stateMachine.Fire(Trigger.SetConnectComplete);
            });
        }
        
        private void OnDisconnected()
        {
            // if (this.lastConnected == null || this.lastConnected + ReconnectTime < DateTimeOffset.UtcNow)           
            this.stateMachine.Fire(Trigger.Connect);
        }

        private async Task OnResetting()
        {
            this.cancellationTokenSource?.Cancel();

            if (this.lifetimeTask != null)
                await this.lifetimeTask;

            this.cancellationTokenSource?.Dispose();
            this.lifetimeTask?.Dispose();
            this.cancellationTokenSource = null;
            this.lifetimeTask = null;

            this.stateMachine.Fire(Trigger.SetResetComplete);
        }

        private void OnConnected()
        {
            this.logger.LogInformation($"{nameof(ManagedWebSocket.OnConnected)}");
            this.lifetimeTask = Task.Run(LifetimeTask);
        }

        public async Task SendAsync(string message)
        {
            await SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text);
        }

        public async Task SendAsync(byte[] data, WebSocketMessageType webSocketMessageType)
        {
            if (this.stateMachine.State != State.Connected)
                throw new Exception($"{nameof(ManagedWebSocket)} is not yet connected");

            try
            {
                await webSocket
                        .SendAsync(data, webSocketMessageType, SendChunkSize, this.cancellationTokenSource.Token)
                        .ConfigureAwait(false);
            }
            catch (WebSocketException ex)
            {
                this.logger.LogError(ex, ex.Message);
                await this.stateMachine.FireAsync(Trigger.Reset);
            }
        }

        public void Start()
        {
            this.stateMachine.Fire(Trigger.Start);
        }

     /*   public async Task Stop()
        {
            this.stateMachine.Fire(Trigger.Start);
        }
     */
        private async Task LifetimeTask()
        {
            while (cancellationTokenSource.Token.IsCancellationRequested == false)
            {
                try
                {
                    this.logger.LogInformation($"{nameof(ManagedWebSocket.LifetimeTask)}");
                    await notifyOnConnected();
                    await this.webSocket.ReceiveFramesLoopAsync(target.SendAsync, ReceiveChunkSize, this.cancellationTokenSource.Token);
                    break;
                }
                catch (WebSocketException ex)
                {
                    this.logger.LogError(ex.Message, ex);
                    await notifyOnDisconnected();
                }

                // await Task.Delay(this.ReconnectTime, cancellationTokenSource.Token);
            }
            await this.stateMachine.FireAsync(Trigger.Reset);
        }     

        public void Dispose()
        {
            this.cancellationTokenSource?.Cancel();

            /* how to wait for lifetime task to complete */
           // if (this.lifetimeTask != null)
           //     await this.lifetimeTask;

            this.cancellationTokenSource?.Dispose();
            this.lifetimeTask?.Dispose();
            this.cancellationTokenSource = null;
            this.lifetimeTask = null;
        }
    }
}