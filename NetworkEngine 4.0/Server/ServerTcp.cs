using NetworkEngine_5._0.Server;
using NetworkEngine_5._2.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NetworkEngine_5._2.Server
{
    public partial class ServerTcp : ServerBase
    {

        private byte SERVER_YES = 0;
        private byte SERVER_FAIL = 1;
        private byte SERVER_NO = 2;
        private byte SERVER_FULL = 3;

        private TcpListener tcpListener;

        public ServerTcp(int _port, int _maxClient = 1000) : base(_port, _maxClient)
        {
            
            tcpListener = new TcpListener(IPAddress.Any, port);

        }

        public override void Start()
        {

            clients = new Dictionary<int, ClientConnectionBase>();

            _ = StartTcpListener();

            status = ServerStatus.Online;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Le server a été lancé ! | Public IP : /public" + " | Private IP : " + "/privateIP/" + " | Port : " + port + " | Max : " + maxClient);
            Console.ForegroundColor = ConsoleColor.White;

        }

        public override void Stop()
        {
            if (status == ServerStatus.Offline)
            {
                print("Server is already offline !\n", ConsoleColor.DarkRed);
                return;
            }

            nextClientID = 0;
            tcpListener?.Stop();

            foreach (var client in clients)
            {
                client.Value.Disconnect();
            }

            clients.Clear();

            print("Server Shutdown ! \n", ConsoleColor.Yellow);
            status = ServerStatus.Offline;
        }


        private async Task StartTcpListener()
        {
            try
            {

                tcpListener.Start();

                while (true)
                {

                    TcpClient cl = await tcpListener.AcceptTcpClientAsync();

                    ClientConnectionTcp newC = new ClientConnectionTcp(cl, nextClientID, this);

                    nextClientID += 1;

                    try
                    {

                        var (success, result) = await CreateTimeout(newC.Receive(), 3000);

                        if (Encoding.UTF8.GetString(result) != "5.2")
                        {
                            newC.Disconnect();
                            continue;
                        }

                    }
                    catch (TimeoutException e)
                    {
                        newC.Disconnect();
                        continue;
                    }

                    if (clients.Count == maxClient)
                    {
                        newC.Send(new Packet(new byte[] { SERVER_FULL }));
                        RaiseServerFull();
                        print("Une connection refusé ! Server plein", ConsoleColor.Red);
                    }
                    else if (!acceptConnection)
                    {
                        newC.Send(new Packet(new byte[] { SERVER_NO }));
                        print("Une connection refusé !", ConsoleColor.Red);
                    }
                    else
                    {
                        newC.Send(new Packet(new byte[] { SERVER_YES }));
                        try
                        {

                            MemoryStream ms = new MemoryStream();
                            BinaryWriter bw = new BinaryWriter(ms);
                            bw.Write(newC.GetID());

                            newC.Send(new Packet(ms.ToArray()));

                            clients.Add(newC.GetID(), newC);

                            newC.cts = new CancellationTokenSource();
                            _ = newC.RecepterTCP(newC.cts.Token);

                            print("Une connection établie : " + "newC.GetIP()" + " ID : " + newC.GetID(), ConsoleColor.DarkMagenta);

                        }
                        catch (IOException e)
                        {
                            print($"Client {newC.GetID()} : Connection failed  !", ConsoleColor.Red);
                        }

                    }

                }

            }
            catch (SocketException e)
            {
                //Console.WriteLine(e);
            }
        }

        public void Send(Packet packet, int clientId)
        {
            /// Le client n'existe pas lors de la connexion...
            ((ClientConnectionTcp)clients[clientId]).Send(packet);
        }

    }
}
