using NetworkEngine_5._0.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NetworkEngine_5._0.Server.Server;

namespace NetworkEngine_4._0.Server
{
    public abstract class ServerBase
    {

        public event Action? OnServerFull;

        public Dictionary<int, Clients> clients = new();
        public Dictionary<int, Clients> connectingClients = new();

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

    }
}
