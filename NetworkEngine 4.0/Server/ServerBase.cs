using NetworkEngine_5._0.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkEngine_5._2.Server
{
    public abstract class ServerBase
    {

        public static event Action? OnServerFull;
        public static event Action<int, byte[]>? OnReceive;

        public Dictionary<int, ClientConnectionBase> clients = new();
        public Dictionary<int, ClientConnectionBase> connectingClients = new();

        protected ServerStatus status = ServerStatus.Offline;

        public int nextClientID = 0;

        protected int port;
        protected int maxClient;
        public bool serverLog = true;

        protected bool acceptConnection = true;

        public ServerBase(int _port, int _maxClient)
        {

            port = _port;
            maxClient = _maxClient;

        }

        public abstract void Start();
        public abstract void Stop();

        public ServerStatus GetStatus()
        {
            return status;
        }


        public void print(string msg, ConsoleColor color, string log = "[SERVER]")
        {
            if (serverLog)
            {
                Console.ForegroundColor = color;
                Console.WriteLine($"{log} " + msg);
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

        protected void RaiseServerFull() => OnServerFull?.Invoke();
        protected void RaiseReceive(int clientId, byte[] bytes) => OnReceive?.Invoke(clientId, bytes);


        public enum ServerStatus
        {
            Offline = 0,
            Starting = 1,
            Online = 2,
        };

    }
}
