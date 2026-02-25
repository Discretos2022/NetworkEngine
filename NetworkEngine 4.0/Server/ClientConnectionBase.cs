using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkEngine_5._2.Server
{
    public abstract class ClientConnectionBase
    {

        protected int ID;
        protected ServerTcp server;

        public ClientConnectionBase(int ID, ServerTcp server)
        {
            this.ID = ID;
            this.server = server;
        }

        public abstract void Disconnect();

        public int GetID()
        {
            return ID;
        }

    }
}
