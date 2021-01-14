using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Core.Models.Contexts;

namespace TX.Core.Interfaces
{
    interface IPersistable
    {
        byte[] ToPersistentByteArray();
    }
}
