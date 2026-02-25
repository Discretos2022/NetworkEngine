using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NetworkEngine_5._2.Engine
{
    public class Packet
    {

        public const int MESSAGE_LENGTH_BYTE = 4;

        private MemoryStream ms;
        private BinaryWriter bw;
        private BinaryReader br;

        public Packet()
        {
            ms = new MemoryStream();
            bw = new BinaryWriter(ms);
            br = new BinaryReader(ms);
        }

        public Packet(byte[] bytes)
        {
            ms = new MemoryStream(bytes);
            bw = new BinaryWriter(ms);
            br = new BinaryReader(ms);
        }

        public void WriteInt(int val)
        {
            bw.Write(val);
        }

        public void WriteFloat(float val)
        {
            bw.Write(val);
        }

        public void WriteString(string val)
        {
            bw.Write(val);
        }

        public void WriteBool(bool val)
        {
            bw.Write(val);
        }

        public int ReadInt() => br.ReadInt32();
        public float ReadFloat() => br.ReadSingle();
        public string ReadString() => br.ReadString();
        public bool ReadBool() => br.ReadBoolean();


        public byte[] GetBytes()
        {
            return ms.ToArray();
        }

        public int GetLength()
        {
            return (int)ms.Length;
        }

    }
}
