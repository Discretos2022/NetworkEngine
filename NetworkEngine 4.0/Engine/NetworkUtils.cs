using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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

    }
}
