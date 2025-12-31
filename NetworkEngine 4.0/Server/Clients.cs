using NetworkEngine_5._0.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkEngine_5._0.Server
{
    public class Clients
    {

        public TaskCompletionSource<IPEndPoint> IsUDPReady = new TaskCompletionSource<IPEndPoint>();

        private TcpClient client;
        private int ID;
        public IPEndPoint udpEndPoint;

        public StreamReader reader;
        public StreamWriter writer;
        public NetworkStream stream;

        public CancellationTokenSource cts;

        public Clients(TcpClient _client, int _ID)
        {
            client = _client;
            ID = _ID;

            stream = client.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);
            writer.AutoFlush = true;

        }


        public async Task RecepterTCP(CancellationToken token)
        {

            bool isDisconnection = false;

            try
            {

                while (!token.IsCancellationRequested)
                {

                    var msg = await reader.ReadLineAsync();

                    /// Déconnection sans erreurs !
                    if (msg == null)
                    {
                        isDisconnection = true;
                        break;
                    }

                    Server.print($"New Message : " + msg, ConsoleColor.Cyan, $"[CLIENT { ID}]");
                    ServerReader.ReadTCPPacket(msg, ID);

                }

            }
            catch (IOException e)
            {
                
            }
            finally
            {
                CloseConnection();

                if (isDisconnection)
                {
                    Server.print($"Client {ID} disconnected ! \n", ConsoleColor.DarkMagenta);
                    // OnClientDisconnect?.Invoke();
                }
                else if (token.IsCancellationRequested)
                {
                    Server.print($"Client {ID} disconnected by server ! \n", ConsoleColor.DarkMagenta);
                    // OnClientDisconnect?.Invoke();
                }
                else
                {
                    Server.print($"Client {ID} lost connection ! \n", ConsoleColor.DarkMagenta);
                    // OnClientLostConnection?.Invoke();
                }

                Server.clients.Remove(ID);

            }

        }


        public async void Send(string msg)
        {
            await writer.WriteLineAsync(msg);
        }


        public void Disconnect()
        {
            cts?.Cancel();
            reader?.Close();
        }


        public void CloseConnection()
        {
            writer?.Close();
            reader?.Close();
            stream?.Close();
            client?.Close();
        }


        public string GetIP()
        {
            if(client.Client.LocalEndPoint != null)
                return client.Client.RemoteEndPoint.ToString();

            return "ERROR";
        }

        public int GetID()
        {
            return ID;
        }

    }
}
