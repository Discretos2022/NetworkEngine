using NetworkEngine_5._0.Server;
using NetworkEngine_5._2.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkEngine_5._2.Server
{

    public partial class ServerTcp
    {

        public class ClientConnectionTcp : ClientConnectionBase
        {

            private TcpClient client;
            public CancellationTokenSource cts;
            public NetworkStream stream;

            public ClientConnectionTcp(TcpClient client, int ID, ServerTcp server) : base(ID, server)
            {
                this.client = client;
                this.stream = client.GetStream();
                cts = new CancellationTokenSource();
            }

            public override void Disconnect()
            {
                cts?.Cancel();
                stream?.Socket?.Shutdown(SocketShutdown.Both);
            }

            public async Task<byte[]> Receive()
            {

                byte[] bufferSize = await Engine.NetworkUtils.ReadByte(stream, Packet.MESSAGE_LENGTH_BYTE);
                int size = BitConverter.ToInt32(bufferSize, 0);


                byte[] bufferData = await Engine.NetworkUtils.ReadByte(stream, size);

                return bufferData;

            }

            public void Send(Packet packet)
            {
                byte[] bytes = new byte[Packet.MESSAGE_LENGTH_BYTE + packet.GetLength()];

                Span<byte> span = bytes;
                bool s = BitConverter.TryWriteBytes(span, packet.GetLength());
                packet.GetBytes().CopyTo(span[Packet.MESSAGE_LENGTH_BYTE..]);

                _ = InternalSend(bytes);
            }

            private async Task InternalSend(byte[] bytes)
            {
                await stream.WriteAsync(bytes, 0, bytes.Length);
            }

            public void StartRecepter()
            {

                _ = RecepterTCP(cts.Token);

            }


            public async Task RecepterTCP(CancellationToken token)
            {

                bool isDisconnection = false;

                try
                {

                    while (!token.IsCancellationRequested)
                    {

                        byte[] bufferSize = await NetworkUtils.ReadByte(stream, Packet.MESSAGE_LENGTH_BYTE);

                        if (bufferSize.Length == 0)
                        {
                            isDisconnection = true;
                            break;
                        }

                        int size = BitConverter.ToInt32(bufferSize, 0);


                        byte[] bufferData = await NetworkUtils.ReadByte(stream, size);

                        if (bufferData.Length == 0)
                        {
                            isDisconnection = true;
                            break;
                        }

                        server.RaiseReceive(ID, bufferData);


                        //Server.print($"New Message : " + msg, ConsoleColor.Cyan, $"[CLIENT {ID}]");
                        //ServerReader.ReadTCPPacket(msg, ID);

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
                        server.print($"Client {ID} disconnected ! \n", ConsoleColor.DarkMagenta);
                        // OnClientDisconnect?.Invoke();
                    }
                    else if (token.IsCancellationRequested)
                    {
                        server.print($"Client {ID} disconnected by server ! \n", ConsoleColor.DarkMagenta);
                        // OnClientDisconnect?.Invoke();
                    }
                    else
                    {
                        server.print($"Client {ID} lost connection ! \n", ConsoleColor.DarkMagenta);
                        // OnClientLostConnection?.Invoke();
                    }

                    server.clients.Remove(ID);

                }

            }

            public void CloseConnection()
            {
                stream?.Close();
                client?.Close();
            }


        }

    }

}
