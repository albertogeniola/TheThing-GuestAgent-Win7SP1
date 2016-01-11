using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkProtocol
{
    public class ProtocolException:Exception
    {
        public ProtocolException(string msg):base(msg){}
    }
}
