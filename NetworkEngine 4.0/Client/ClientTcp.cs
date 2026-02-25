using NetworkEngine_5._2.Engine;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkEngine_5._2.Client
{
    public class ClientTcp : ClientBase
    {

        private byte SERVER_YES = 0;
        private byte SERVER_FAIL = 1;
        private byte SERVER_NO = 2;
        private byte SERVER_FULL = 3;

        public TcpClient client;
        public NetworkStream stream;
        public StreamReader reader;
        public StreamWriter writer;

        private CancellationTokenSource cts;

        private string ip;
        private int port;

        public ClientTcp(string _ip, int _port)
        {
            client = new TcpClient();
            ip = _ip;
            port = _port;
        }


        public override void Connect()
        {

            if (state == ClientState.Connected)
            {
                print("You are already connected !", ConsoleColor.Yellow);
                return;
            }

            if (state == ClientState.Connecting)
            {
                print("Wait... connecting... !", ConsoleColor.Yellow);
                return;
            }

            state = ClientState.Connecting;

            _ = ConnectClient();

        }

        public override void Disconnect()
        {
            
            if (state == ClientState.Disconnected)
            {
                print("You are already disconnected !", ConsoleColor.Yellow);
                return;
            }

            cts?.Cancel();
            stream?.Socket?.Shutdown(SocketShutdown.Both);

        }

        public void Send(byte[] bytes)
        {

            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(bytes.Length);
            bw.Write(bytes);

            _ = InternalSend(ms.ToArray());
        }

        public void Send(Packet packet)
        {

            byte[] bytes = new byte[Packet.MESSAGE_LENGTH_BYTE + packet.GetLength()];

            Span<byte> span = bytes;
            BitConverter.TryWriteBytes(span, packet.GetLength());
            packet.GetBytes().CopyTo(span[Packet.MESSAGE_LENGTH_BYTE..]);

            _ = InternalSend(bytes);
        }

        private async Task InternalSend(byte[] bytes)
        {
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }

        public async Task<byte[]> Receive()
        {

            byte[] bufferSize = await NetworkUtils.ReadByte(stream, Packet.MESSAGE_LENGTH_BYTE);
            int size = BitConverter.ToInt32(bufferSize, 0);


            byte[] bufferData = await NetworkUtils.ReadByte(stream, size);

            return bufferData;

        }

        private async Task ConnectClient()
        {
            try
            {

                client = new TcpClient();
                await client.ConnectAsync(ip, port);

                stream = client.GetStream();
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream);
                writer.AutoFlush = true;

                Send(Encoding.UTF8.GetBytes("5.2"));

                byte response = SERVER_FAIL;

                try
                {
                    var (success, result) = await CreateTimeout(Receive(), 3000);
                    // response = Encoding.UTF8.GetString(result);
                    if (result.Length > 0) response = result[0];
                }
                catch (TimeoutException e)
                {
                    response = SERVER_FAIL;
                }


                if (response == SERVER_FULL)
                {
                    RaiseServerFull();
                    print("Connection failed : Server is full !", ConsoleColor.Red);
                    CloseConnection();
                    return;
                }
                else if (response == SERVER_NO)
                {
                    RaiseConnectionRefused();
                    print("Connection failed : Server connection refused !", ConsoleColor.Red);
                    CloseConnection();
                    return;
                }
                else if (response == SERVER_YES)
                {
                    int clientId = -1;
                    try
                    {
                        var (success, result) = await CreateTimeout(Receive(), 3000);
                        clientId = BitConverter.ToInt32(result);
                    }
                    catch (TimeoutException e)
                    {
                        RaiseConnectionFail();
                        print("Connection failed : Server connection failed !", ConsoleColor.Red);
                        CloseConnection();
                        return;
                    }


                    // ID = int.Parse(clientId.Substring(1));
                    ID = clientId;

                    cts = new CancellationTokenSource();
                    _ = RecepterTCP(cts.Token);

                    state = ClientState.Connected;

                    RaiseConnected();
                    print("Connexion réussi !", ConsoleColor.Yellow);

                }
                else // if (response == SERVER_FAIL)
                {
                    RaiseConnectionFail();
                    print("Connection failed : Server connection failed !", ConsoleColor.Red);
                    CloseConnection();
                    return;
                }

            }
            catch (Exception e)
            {
                CloseConnection();
                RaiseConnectionFail();
                print("Connection failed : Server connection failed !", ConsoleColor.Red);
            }
        }



        public async Task RecepterTCP(CancellationToken token)
        {

            bool isServerShutdown = false;

            try
            {

                while (!token.IsCancellationRequested)
                {

                    /// Récupérer la taille du packet
                    //byte[] bufferSize = new byte[4];
                    //int offset = 0;

                    //while (offset < 4)
                    //{
                    //    int read = await stream.ReadAsync(bufferSize, offset, 4 - offset);

                    //    /// Arret sans erreurs !
                    //    if (read == 0)
                    //    {
                    //        isServerShutdown = true;
                    //        break;
                    //    }

                    //    offset += read;

                    //}

                    byte[] bufferSize = await Engine.NetworkUtils.ReadByte(stream, 4);

                    if (bufferSize.Length == 0)
                    {
                        isServerShutdown = true;
                        break;
                    }

                    int size = BitConverter.ToInt32(bufferSize, 0);

                    //if (isServerShutdown) break;

                    /// Lire les données du packet
                    byte[] bufferData = new byte[size];
                    int offsetData = 0;

                    int decoupe = 0;

                    while (offsetData < size)
                    {
                        int read = await stream.ReadAsync(bufferData, offsetData, size - offsetData);

                        /// Arret sans erreurs !
                        if (read == 0)
                        {
                            isServerShutdown = true;
                            break;
                        }

                        offsetData += read;

                        decoupe += 1;

                    }

                    if (isServerShutdown) break;

                    Console.WriteLine("decoupe : " + decoupe + " / size : " + bufferData.Length);

                    RaiseReceive(new Packet(bufferData));




                    //var msg = await reader.ReadLineAsync();

                    ///// Arret sans erreurs !
                    //if (msg == null)
                    //{
                        //isServerShutdown = true;
                        //break;
                    //}

                    //print("new Message : " + msg, ConsoleColor.Cyan);
                    //ClientReader.ReadTCPPacket(msg);

                }

            }
            catch (SocketException e)
            {
                Console.WriteLine("ERROR 404 : " + e);
            }
            catch (Exception e)
            {
                // Console.WriteLine(e);
            }
            finally
            {
                CloseConnection();

                if (token.IsCancellationRequested)
                {
                    print("You are disconnected !", ConsoleColor.Yellow);
                    RaiseDisconnected();
                }
                else if (isServerShutdown)
                {
                    print("Server shutdown, you are disconnected !", ConsoleColor.Yellow);
                    RaiseServerShutdown();
                }
                else
                {
                    print("Connection lost !", ConsoleColor.Red);
                    RaiseConnectionLost();
                }

            }

        }



        private void CloseConnection()
        {
            if (state == ClientState.Disconnected) return;

            writer?.Close();
            reader?.Close();
            stream?.Close();
            client?.Close();

            state = ClientState.Disconnected;
        }

    }
}
