using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLife.Remoting
{
    interface IApi
    {
        IApiSession Session { get; set; }
    }
}
