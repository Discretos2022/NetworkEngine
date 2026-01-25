using Microsoft.VisualBasic;
using NetworkEngine_5._0.Client;
using NetworkEngine_5._0.Server;
using NetworkEngine_5._0.Error;
using System;
using System.Data;
using System.Text;
using NetworkEngine_4._0.Server;

/**
 * NetworkEngine
 * Version : 5.2
 * Build : 0
 * SIEDEL Joshua © 2023-2026
 * Copyright © 2023-2026 SIEDEL Joshua
 */
namespace Tester
{
    internal class Test
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("NetworkEngine 5.0  Copyright © 2024 SIEDEL Joshua \n");

            string title = "NetworkEngine 5.2  Copyright © 2024-2026 SIEDEL Joshua \n";

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

            //Server.StartTCP();
            //Server.StartUDP();

            //Client.Connect("172.20.10.8", 7777); // 192.168.1.25  172.20.10.8  10.93.15.97

            Server.OnServerFull += () => 
            {
                Console.WriteLine("SERVER FULL, une connection a été refusée !");
            };

            Client.OnTimeOut += () =>
            {
                Console.WriteLine("TIMEOUT !");
            };

            while (true)
            {

                Command();

            }

        }

        static void Command()
        {

            string msg = Console.ReadLine();

            if (msg == null)
                return;

            string[] args = msg.Split(' ');

            if (args[0] == "/users")
            {
                print("\nConnected users : \n", ConsoleColor.Cyan);

                if(Server.clients.Count == 0)
                    print("\tThere are no user !", ConsoleColor.Cyan);

                for (int i = 0; i < Server.clients.Count; i++)
                {
                    print("\t" + Server.clients[i].GetID() + " : " + Server.clients[i].GetIP() + " | " + Server.clients[i].udpEndPoint, ConsoleColor.Cyan);
                }

                Console.WriteLine("");

            }

            ServerBase server = null;

            if (args[0] == "/start")
            {

                //if (args.Length == 2)
                //{
                //    Server.Start(int.Parse(args[1]));
                //}
                //else if (args.Length == 3)
                //{
                //    Server.Start(int.Parse(args[1]), int.Parse(args[2]));
                //}
                //// Le serveur a désormais un paramètre pour tcp fallback si l'udp n'est pas dispo
                ////else if (args.Length == 4)
                ////{
                ////    Server.Start(int.Parse(args[1]), int.Parse(args[2]), bool.Parse(args[3]));
                ////}
                //else
                //{
                //    Server.Start();
                //}

                server = new ServerTcp(7777, 1000);
                server.Start();

                
            }

            if (args[0] == "/stop")
            {
                Server.StopServer();
                server?.Stop();
            }

            if (args[0] == "/connect")
            {
                // Client.Connect(args[1], int.Parse(args[2]));
                Client.Connect("192.168.1.25", 7777);
            }

            if (args[0] == "/disconnect")
            {
                Client.Disconnect();
            }

            if (args[0] == "/send")
            {
                if(args.Length == 1)
                    Client.SendTCP("NetworkEngine 5.2");
                else
                    Client.SendTCP(msg.Substring(6, msg.Length - 6));
            }

            if (args[0] == "/udp")
            {
                if (args.Length == 1)
                    Client.SendUDP("NetworkEngine 5.2");
                else
                    Client.SendUDP(msg.Substring(5, msg.Length - 5));
            }

            if (args[0] == "/stcp")
            {
                if (args.Length == 1)
                    Server.SendTCP("NetworkEngine 5.2");
                else
                    Server.SendTCP(msg.Substring(6 + args[1].Length, msg.Length - (6 + args[1].Length)), int.Parse(args[1]));
            }

            if (args[0] == "/sudp")
            {
                if (args.Length == 1)
                    Server.SendUDP("NetworkEngine 5.2");
                else
                    Server.SendUDP(msg.Substring(6 + args[1].Length, msg.Length - (6 + args[1].Length)), int.Parse(args[1]));
            }

            if (args[0] == "/info")
            {
                print(Client.client.Client.LocalEndPoint.ToString(), ConsoleColor.Cyan);
            }

            if (args[0] == "/status")
            {
                Console.Write("STATUS : ");
                if (Server.GetStatus() == Server.ServerStatus.Offline)
                    print(Server.GetStatus().ToString().ToUpper(), ConsoleColor.Red);
                else if (Server.GetStatus() == Server.ServerStatus.Starting)
                    print(Server.GetStatus().ToString().ToUpper(), ConsoleColor.DarkYellow);
                else if (Server.GetStatus() == Server.ServerStatus.Online)
                    print(Server.GetStatus().ToString().ToUpper(), ConsoleColor.Green);

            }

            if (args[0] == "/state")
            {
                Console.Write("STATE : ");
                if (Client.GetState() == Client.ClientState.Disconnected)
                    print(Client.GetState().ToString().ToUpper(), ConsoleColor.Red);
                else if (Client.GetState() == Client.ClientState.Connecting)
                    print(Client.GetState().ToString().ToUpper(), ConsoleColor.DarkYellow);
                else if (Client.GetState() == Client.ClientState.Connected)
                    print(Client.GetState().ToString().ToUpper(), ConsoleColor.Green);

            }

            if(args[0] == "/clear")
            {
                Console.Clear();
                WriteTitle();
            }


        }

        public static void printColor(string msg, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(msg);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void print(string msg, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.White;
        }


        public static void WriteTitle()
        {
            string title = "NetworkEngine 5.0  Copyright © 2024 SIEDEL Joshua \n";

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

    }
}
