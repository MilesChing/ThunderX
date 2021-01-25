using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TXUnitTest.Utils
{
    static class StreamExtension
    {
        public static bool Compare(this Stream tstream, Stream istream)
        {
            int tb = -1, ib = -1;
            while (tb >= 0)
            {
                tb = tstream.ReadByte();
                ib = istream.ReadByte();
                if (tb == ib) continue;
                else return false;
            }
            return true;
        }
    }
}
