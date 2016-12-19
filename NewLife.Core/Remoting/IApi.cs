using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLife.Remoting
{
    /// <summary>Api接口</summary>
    public interface IApi
    {
        /// <summary>会话</summary>
        IApiSession Session { get; set; }
    }
}