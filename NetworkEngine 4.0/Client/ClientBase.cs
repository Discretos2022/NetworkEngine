using NetworkEngine_5._2.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkEngine_5._2.Client
{
    public abstract class ClientBase
    {

        public static event Action? OnTimeOut;
        public static event Action? OnServerFull;
        public static event Action? OnConnectionRefused;
        public static event Action? OnConnectionFail;
        public static event Action? OnConnectionLost;
        public static event Action? OnServerShutdown;
        public static event Action? OnConnected;
        public static event Action? OnDisconnected;

        public static event Action<Packet>? OnReceive;

        public static bool clientLog = true;

        protected ClientState state = ClientState.Disconnected;

        protected int ID;


        public ClientBase()
        {

        }


        public abstract void Connect();
        public abstract void Disconnect();


        public static void print(string msg, ConsoleColor color)
        {
            if (clientLog)
            {
                Console.ForegroundColor = color;
                Console.WriteLine("[LOCAL] " + msg);
                Console.ForegroundColor = ConsoleColor.White;
            }

        }


        protected async Task<(bool success, T result)> CreateTimeout<T>(Task<T> task, int timeout)
        {

            var timeoutTask = Task.Delay(timeout);
            var finished = await Task.WhenAny(task, timeoutTask);

            if (finished == timeoutTask)
                throw new TimeoutException();

            return (true, await task);

        }

        protected void RaiseConnected() => OnConnected?.Invoke();
        protected void RaiseDisconnected() => OnDisconnected?.Invoke();
        protected void RaiseServerFull() => OnServerFull?.Invoke();
        protected void RaiseConnectionRefused() => OnConnectionRefused?.Invoke();
        protected void RaiseConnectionFail() => OnConnectionFail?.Invoke();
        protected void RaiseConnectionLost() => OnConnectionLost?.Invoke();
        protected void RaiseServerShutdown() => OnServerShutdown?.Invoke();

        protected void RaiseReceive(Packet packet) => OnReceive?.Invoke(packet);


        public enum ClientState
        {
            Disconnected = 0,
            Connecting = 1,
            Connected = 2,
        }


    }
}
