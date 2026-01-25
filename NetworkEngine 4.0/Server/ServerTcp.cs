using NetworkEngine_5._0.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static NetworkEngine_5._0.Server.Server;

namespace NetworkEngine_4._0.Server
{
    public class ServerTcp : ServerBase
    {

        private TcpListener tcpListener;

        public ServerTcp(int _port, int _maxClient = 1000) : base(_port, _maxClient)
        {

            tcpListener = new TcpListener(IPAddress.Any, port);

        }

        public override void Start()
        {

            clients = new Dictionary<int, Clients>();

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

                    Clients newC = new Clients(nextClientID);
                    nextClientID += 1;
                    newC.SetTcpClient(cl);


                    try
                    {

                        var (success, result) = await CreateTimeout(newC.reader.ReadLineAsync(), 3000);

                        if (result != "5.2")
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
                        newC.writer.WriteLine("#FULL");
                        RaiseServerFull();
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

                            newC.writer.WriteLine("#" + newC.GetID());

                            clients.Add(newC.GetID(), newC);

                            newC.cts = new CancellationTokenSource();
                            _ = newC.RecepterTCP(newC.cts.Token);

                            print("Une connection établie : " + newC.GetIP() + " ID : " + newC.GetID(), ConsoleColor.DarkMagenta);

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

            }
        }

    }
}
