using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NetworkEngine_5._0.Server
{
    public static class Server
    {

        public static event Action? OnServerFull;


        private static TcpListener tcpListener;
        private static UdpClient udpListener;

        public static Dictionary<int, Clients> clients = new();
        public static Dictionary<int, Clients> connectingClients = new();

        private static NetworkStream networkStream;
        private static StreamReader reader;
        private static StreamWriter writer;

        private static string publicIP = "127.0.0.1";

        public static bool stopRequest = false;

        private static ServerStatus status = ServerStatus.Offline;
        private static ServerMode serverMode;
        private static int udpTimeout;

        public static int nextClientID = 0;

        public static bool serverLog = true;
        public static bool udpLog = true;

        private static bool acceptConnection = true;

        public static void Start(int _port = 7777, int _maxClient = 1000, ServerMode _serverMode = ServerMode.TcpUdp, int _udpTimeout = 3000)
        {
            if (status != ServerStatus.Offline)
            {
                print("Server is already started !", ConsoleColor.DarkRed);
                return;
            }

            status = ServerStatus.Starting;

            serverMode = _serverMode;
            udpTimeout = _udpTimeout;

            if (!IsTcpPortAvailable(_port))
            {
                print($"TCP port {_port} is already used !", ConsoleColor.DarkRed);
                status = ServerStatus.Offline;
                return;
            }

            if (serverMode != ServerMode.TcpOnly)
            {
                if (!IsUdpPortAvailable(_port))
                {
                    print($"UDP port {_port} is already used !", ConsoleColor.DarkRed);
                    status = ServerStatus.Offline;
                    return;
                }
            }

            //WriteTitle();
            StartTCP(_port, _maxClient);

            if (serverMode != ServerMode.TcpOnly)
                StartUDP(_port);


            string privateIP = GetPrivateIP();

            //#region SearchPublicIP

            //try
            //{
            //    using HttpClient client = new HttpClient();
            //    publicIP = client.GetString("https://api.ipify.org");
            //}
            //catch
            //{
            //    publicIP = "127.0.0.1";
            //}

            //#endregion

            clients = new Dictionary<int, Clients>();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Le server a été lancé ! | Public IP : /public" + " | Private IP : " + privateIP + " | Port : " + _port + " | Max : " + _maxClient);
            Console.ForegroundColor = ConsoleColor.White;

            status = ServerStatus.Online;

            Console.WriteLine("");

        }


        private static void StartTCP(int port = 7777, int _maxClient = 1000)
        {

            tcpListener = new TcpListener(IPAddress.Any, port);
            _ = StartTcpListener(port, _maxClient);

        }

        private static async Task StartTcpListener(int port = 7777, int _maxClient = 1000)
        {

            try
            {
                tcpListener.Start();

                while (true)
                {

                    TcpClient cl = await tcpListener.AcceptTcpClientAsync();

                    Clients newC = new Clients(cl, nextClientID);
                    nextClientID += 1;

                    if (clients.Count == _maxClient)
                    {
                        newC.writer.WriteLine("#FULL");
                        OnServerFull?.Invoke();
                        print("Une connection refusé ! Server plein", ConsoleColor.Red);
                    }
                    else if (!acceptConnection)
                    {
                        newC.writer.WriteLine("#NO");
                        print("Une connection refusé !", ConsoleColor.Red);
                    }
                    else
                    {
                        newC.writer.WriteLine("#YES");
                        try
                        {
                            //var endPointUDP = newC.reader.ReadLine();
                            //Console.WriteLine($"{endPointUDP}");
                            //var ip = endPointUDP.Split(":")[0];
                            //var p = int.Parse(endPointUDP.Split(":")[1]);
                            //newC.udpEndPoint = new IPEndPoint(IPAddress.Parse(ip), p);

                            //newC.udpClient.Connect(newC.);

                            newC.writer.WriteLine("#" + newC.GetID());

                            if (serverMode != ServerMode.TcpOnly)
                            {
                                connectingClients.Add(newC.GetID(), newC);

                                var udpTask = newC.IsUDPReady.Task;
                                var timeoutTask = Task.Delay(3000);

                                var finished = await Task.WhenAny(udpTask, timeoutTask);

                                if (finished == timeoutTask)
                                {
                                    print($"Client {newC.GetID()} : UDP was failed !", ConsoleColor.Red);
                                    newC.Disconnect();
                                    connectingClients.Remove(newC.GetID());
                                    continue;
                                }

                                newC.udpEndPoint = udpTask.Result;

                                connectingClients.Remove(newC.GetID());
                            }


                            clients.Add(newC.GetID(), newC);

                            newC.cts = new CancellationTokenSource();
                            _ = newC.RecepterTCP(newC.cts.Token);

                            print("Une connection établie : " + newC.GetIP() + " ID : " + newC.GetID(), ConsoleColor.DarkMagenta);

                        }
                        catch (IOException e)
                        {
                            print("Connection failed : UDP was failed !", ConsoleColor.Red);
                        }

                    }

                }

            }
            catch (SocketException e)
            {

            }

        }

        private static void StartUDP(int port = 7777)
        {
            udpListener = new UdpClient(port);
            _ = RecepterUDP();
        }


        public static void StopServer()
        {

            nextClientID = 0;

            if (tcpListener != null)
            {
                stopRequest = true;

                if (networkStream != null)
                {
                    writer.Close();
                    reader.Close();
                    networkStream.Close();
                }

                tcpListener.Stop();
                tcpListener = null;

                udpListener.Close();
                udpListener = null;


                foreach (var client in clients) 
                {
                    client.Value.Disconnect();
                }

                clients.Clear();

                print("Server Shutdown ! \n", ConsoleColor.Yellow);
                status = ServerStatus.Offline;
            }
            else
            {
                print("Server is not online !\n", ConsoleColor.DarkRed);
            }

        }


        
        public static ServerStatus GetStatus()
        {
            return status;
        }

        //public static List<Clients> GetClients()
        //{
        //    return clients;
        //}



        public static async void SendTCP(string data, int clientID = 0, int except = -1)
        {
            if (clientID != 0)
                await clients[clientID].writer.WriteLineAsync(data);
            else
            {
                for (int i = 1; i < clients.Count; i++)
                    if (i != except)
                        await clients[i].writer.WriteLineAsync(data);
            }
        }

        /**
         Change TCPSend()
         */
        public static async void SendUDP(string message, int clientID = 0, int except = -1)
        {

            byte[] bytes = Encoding.UTF8.GetBytes(message);

            if (clientID != 0)
            {
                if (clients[clientID].udpEndPoint != null)
                    await udpListener.SendAsync(bytes, bytes.Length, clients[clientID].udpEndPoint);
                else
                    SendTCP(message + " (only TCP)", clientID);
            }
                
            else
            {
                for (int i = 1; i < clients.Count; i++)
                {
                    if (clients[clientID].udpEndPoint != null)
                        if(i != except)
                            await udpListener.SendAsync(bytes, bytes.Length, clients[i].udpEndPoint);
                    else
                        SendTCP(message + " (only TCP)", i);
                }
            }


            
        }

        public static async Task RecepterUDP()
        {

            try
            {

                while (true)
                {

                    UdpReceiveResult result = await udpListener.ReceiveAsync();
                    byte[] bytes = result.Buffer;
                    string msg = Encoding.UTF8.GetString(bytes);

                    if(msg.Split(" ")[0] != "#CONNECTION")
                    {
                        if(udpLog)
                            print($"New UDP Message : " + msg, ConsoleColor.Cyan);
                        ServerReader.ReadUDPPacket(msg);
                    }
                        
                    else
                    {
                        int id;
                        bool parsedID = int.TryParse(msg.Split(" ")[1], out id);
                        if (!parsedID) id = -1;

                        Clients c;
                        bool success = connectingClients.TryGetValue(id, out c);

                        if (success)
                        {
                            c.IsUDPReady.TrySetResult(result.RemoteEndPoint);
                            print($"User {c.GetID()} UDP endPoint receive : " + result.RemoteEndPoint.ToString(), ConsoleColor.DarkMagenta);
                        }

                    }

                }

            }
            catch (IOException e)
            {

            }
            catch (SocketException e)
            {

            }

        }


        private static async void SearchPublicIP()
        {
            try
            {
                String direction = "";
                HttpWebRequest request = HttpWebRequest.CreateHttp("http://checkip.dyndns.org/");
                using (WebResponse response = await request.GetResponseAsync())
                {
                    using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                    {
                        direction = stream.ReadToEnd();
                    }
                }
                //Search for the ip in the html
                int first = direction.IndexOf("Address: ") + 9;
                int last = direction.LastIndexOf("");
                direction = direction.Substring(first, last - first - 16);
                publicIP = direction;
            }
            catch (Exception ex)
            {
                publicIP = "127.0.0.1";
            }
        }


        public static string GetPrivateIP()
        {
            for (int i = 0; i < Dns.GetHostEntry(Dns.GetHostName()).AddressList.Length; i++)
            {
                if (Dns.GetHostEntry(Dns.GetHostName()).AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    return Dns.GetHostEntry(Dns.GetHostName()).AddressList[i].ToString();
            }

            return "???";

        }


        public static string GetPublicIP()
        {
            return publicIP;
        }


        public static void SetAcceptConnection(bool accept)
        {
            acceptConnection = accept;
        }

        public static bool IsAcceptConnection() { return acceptConnection; }


        public static void print(string msg, ConsoleColor color, string log = "[SERVER]")
        {
            if (serverLog)
            {
                Console.ForegroundColor = color;
                Console.WriteLine($"{log} " + msg);
                Console.ForegroundColor = ConsoleColor.White;
            }
            
        }

        public enum ServerStatus
        {
            Offline = 0,
            Starting = 1,
            Online = 2,
        };


        public static void WriteTitle()
        {
            string title = "NetworkEngine 5.2  Copyright © 2024-2025 SIEDEL Joshua \n";

            int[] table = { 1, 3, 9, 11, 10, 2, 14, 6, 12, 4, 5, 13 };

            int color = 0;
            int v = 1;
            for (int i = 0; i < title.Length; i++)
            {
                printColor(title[i].ToString(), (ConsoleColor)table[color]);
                color += v;
                if (color >= table.Length - 1 || color <= 0)
                    v *= -1;
            }

            Console.WriteLine("");
        }

        public static void printColor(string msg, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(msg);
            Console.ForegroundColor = ConsoleColor.White;
        }


        private static bool IsTcpPortAvailable(int _port)
        {

            try
            {
                TcpListener tcpListener = new TcpListener(IPAddress.Any, _port);
                tcpListener.Start();
                tcpListener.Stop();
                return true;
            }
            catch(SocketException e)
            {
                return false;
            }

        }

        private static bool IsUdpPortAvailable(int _port)
        {

            try
            {
                UdpClient udpClient = new UdpClient(_port);
                udpClient.Close();
                return true;
            }
            catch (SocketException e)
            {
                return false;
            }

        }


        public enum ServerMode
        {
            TcpOnly = 0,
            UdpWithClientTcpFallback = 1,
            TcpUdp = 2,
        }


    }
}
