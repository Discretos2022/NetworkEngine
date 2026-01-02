using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkEngine_5._0.Client
{
    public static class Client
    {

        public static event Action? OnTimeOut;
        public static event Action? OnServerFull;
        public static event Action? OnConnectionRefused;
        public static event Action? OnConnectionLost;
        public static event Action? OnServerShutdown;
        public static event Action? OnConnected;
        public static event Action? OnDisconnected;



        public static TcpClient client = new TcpClient();
        public static UdpClient udpClient = new UdpClient();

        public static NetworkStream stream;
        public static StreamReader reader;
        public static StreamWriter writer;

        private static CancellationTokenSource cts;

        public static int ID = 0;

        private static ClientState state = ClientState.Disconnected;
        private static ConnectionMode connectionMode;

        public static bool clientLog = true;
        public static bool udpLog = true;
        public static bool exception = false;

        public static void Connect(string _ip, int _port = 7777, ConnectionMode _connectionMode = ConnectionMode.TcpUdp)
        {

            if (state == ClientState.Connected)
            {
                print("You are already connected !", ConsoleColor.Yellow);
                return;
            }

            connectionMode = _connectionMode;

            switch (connectionMode)
            {
                case ConnectionMode.TcpOnly:
                    _ = ConnectTcpOnly(_ip, _port);
                    break;

                case ConnectionMode.TcpUdp:
                    _ = ConnectTcpUdp(_ip, _port);
                    break;
            }

        }

        public static async Task ConnectTcpOnly(string _ip, int _port = 7777)
        {

            try
            {

                state = ClientState.Connecting;

                client = new TcpClient();
                await client.ConnectAsync(_ip, _port);

                stream = client.GetStream();
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream);
                writer.AutoFlush = true;

                string response = await reader.ReadLineAsync() ?? "";

                if (response == "#FULL")
                {
                    OnServerFull?.Invoke();
                    print("Connection failed : Server is full !", ConsoleColor.Red);
                    CloseConnection();
                    return;
                }
                else if (response == "#NO")
                {
                    OnConnectionRefused?.Invoke();
                    print("Connection failed : Server connection refused !", ConsoleColor.Red);
                    CloseConnection();
                    return;
                }
                else
                {
                    string clientId = await reader.ReadLineAsync() ?? "#-1";
                    ID = int.Parse(clientId.Substring(1));

                    cts = new CancellationTokenSource();
                    _ = RecepterTCP(cts.Token);

                    state = ClientState.Connected;

                    OnConnected?.Invoke();
                    print("Connexion réussi !", ConsoleColor.Yellow);

                }

            }
            catch (SocketException e)
            {
                OnTimeOut?.Invoke();
                print("Connection failed : Check IP and Port or Server is not online ! \n", ConsoleColor.Red);
                CloseConnection();
            }

        }

        public static async Task ConnectTcpUdp(string _ip, int _port = 7777)
        {

            try
            {

                state = ClientState.Connecting;

                /// Test UDP avant tout
                udpClient = new UdpClient();
                udpClient.Connect(_ip, _port);

                SendUDP("#CONNECTION");

                var udpTask = udpClient.ReceiveAsync();
                var timeoutTask = Task.Delay(3000);

                var finished = await Task.WhenAny(udpTask, timeoutTask);

                if (finished == timeoutTask) //  || udpTask.IsFaulted
                {
                    print("Connection was failed !", ConsoleColor.Red);
                    CloseConnection();
                    return;
                }

                UdpReceiveResult result = await udpTask;
                byte[] bytes = result.Buffer;
                string id = Encoding.UTF8.GetString(bytes);

                ID = int.Parse(id);


                client = new TcpClient();
                await client.ConnectAsync(_ip, _port);

                stream = client.GetStream();
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream);
                writer.AutoFlush = true;

                await writer.WriteLineAsync(ID.ToString());

                string response = await reader.ReadLineAsync() ?? "";

                if (response == "#FULL")
                {
                    OnServerFull?.Invoke();
                    print("Connection failed : Server is full !", ConsoleColor.Red);
                    CloseConnection();
                    return;
                }
                else if (response == "#NO")
                {
                    OnConnectionRefused?.Invoke();
                    print("Connection failed : Server connection refused !", ConsoleColor.Red);
                    CloseConnection();
                    return;
                }
                else
                {

                    cts = new CancellationTokenSource(); 
                    _ = RecepterTCP(cts.Token);
                    RecepterUDP();

                    state = ClientState.Connected;

                    OnConnected?.Invoke();
                    print("Connexion réussi !", ConsoleColor.Yellow);

                }

            }
            catch (SocketException e)
            {
                OnTimeOut?.Invoke();
                print("Connection failed : Check IP and Port or Server is not online ! \n", ConsoleColor.Red);
                CloseConnection();
            }

        }

        public static void Disconnect()
        {
            if(state != ClientState.Disconnected)
            {
                cts?.Cancel();
                reader?.Close();
            }
            else
                print("You are already disconnected !", ConsoleColor.Yellow);

        }

        private static void CloseConnection()
        {
            if (state == ClientState.Disconnected) return;

            writer?.Close();
            reader?.Close();
            stream?.Close();
            client?.Close();
            udpClient?.Close();

            state = ClientState.Disconnected;

        }

        public static ClientState GetState()
        {
            return state;
        }


        public static async Task RecepterTCP(CancellationToken token)
        {

            bool isServerShutdown = false;

            try
            {

                while (!token.IsCancellationRequested)
                {

                    var msg = await reader.ReadLineAsync();

                    /// Arret sans erreurs !
                    if (msg == null)
                    {
                        isServerShutdown = true;
                        break;
                    }

                    print("new Message : " + msg, ConsoleColor.Cyan);
                    ClientReader.ReadTCPPacket(msg);

                }

            }
            catch (SocketException e)
            {
                Console.WriteLine("ERROR 404 : " + e);
            }
            catch (IOException e)
            {

            }
            finally
            {
                CloseConnection();

                if (token.IsCancellationRequested)
                {
                    print("You are disconnected !", ConsoleColor.Yellow);
                    OnDisconnected?.Invoke();
                }
                else if (isServerShutdown)
                {
                    print("Server shutdown, you are disconnected !", ConsoleColor.Yellow);
                    OnServerShutdown?.Invoke();
                }
                else
                {
                    print("Connection lost !", ConsoleColor.Red);
                    OnConnectionLost?.Invoke();
                }

            }

        }


        public static async void RecepterUDP()
        {

            try
            {

                while (true)
                {

                    UdpReceiveResult result = await udpClient.ReceiveAsync();
                    byte[] bytes = result.Buffer;
                    string msg = Encoding.UTF8.GetString(bytes);

                    if(udpLog)
                        print($"New UDP Message : " + msg + "\n", ConsoleColor.Cyan);

                    ClientReader.ReadUDPPacket(msg);

                }

            }
            catch (SocketException e)
            {
                //print("SO" + e.ToString(), ConsoleColor.Red);
            }
            catch (IOException e)
            {
                //print("IO" + e.ToString(), ConsoleColor.Red);
            }

        }


        public static async void SendTCP(string message)
        {
            await writer.WriteLineAsync(message);
        }

        public static async void SendUDP(string message)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                await udpClient.SendAsync(bytes);
            }
            catch(SocketException) {}
            
        }


        public static void print(string msg, ConsoleColor color)
        {
            if (clientLog)
            {
                Console.ForegroundColor = color;
                Console.WriteLine("[LOCAL] " + msg);
                Console.ForegroundColor = ConsoleColor.White;
            }
            
        }


        public enum ClientState
        {
            Disconnected = 0,
            Connecting = 1,
            Connected = 2,
        }

        public enum ConnectionMode
        {
            TcpOnly = 0,
            UdpWithClientTcpFallback = 1,
            TcpUdp = 2,
        }


    }
}
