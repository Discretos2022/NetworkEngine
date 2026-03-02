using System;
using System.Net.Sockets;

namespace NetworkEngine_5._2.Engine
{
    public class NetworkUtils
    {

        public static async Task<byte[]> ReadByte(NetworkStream stream, int bytes)
        {

            byte[] buffer = new byte[bytes];

            int offset = 0;

            while (offset < bytes)
            {
                int read = await stream.ReadAsync(buffer, offset, bytes - offset);

                /// Arret sans erreurs !
                if (read == 0)
                {
                    return Array.Empty<byte>();
                }

                offset += read;

            }

            return buffer;

        }

        public static int ByteToInt(byte[] bytes)
        {

            int result = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                result |= bytes[i] << (i * 8);
            }
            return result;

        }

    }
}
