using Stateless;
using System;
using System.Threading.Tasks;

namespace PolygonIo.WebSocket.Socket
{
    enum State { Stopped, Disconnected, Connecting, Connected, Resetting }
    enum Trigger { Start, Stop, Connect, SetConnectComplete, Reset, SetResetComplete }

    class StateMachine : StateMachine<State, Trigger>
    {
        public StateMachine(Action OnDisconnected, Action OnConnecting, Action OnConnected, Func<Task> OnResetting) : base(State.Stopped)
        {
            this.Configure(State.Stopped)
                .Permit(Trigger.Start, State.Disconnected);

            this.Configure(State.Disconnected)
                .OnEntry(OnDisconnected)
                .Permit(Trigger.Connect, State.Connecting)
                .Permit(Trigger.Stop, State.Stopped);

            this.Configure(State.Connecting)
                .OnEntry(OnConnecting)
                .Permit(Trigger.SetConnectComplete, State.Connected)
                .Permit(Trigger.Reset, State.Resetting);

            this.Configure(State.Connected)
                .OnEntry(OnConnected)
                .Permit(Trigger.Reset, State.Resetting);

            this.Configure(State.Resetting)
                .OnEntryAsync(OnResetting)
                .Permit(Trigger.SetResetComplete, State.Disconnected);
        }
    }
}