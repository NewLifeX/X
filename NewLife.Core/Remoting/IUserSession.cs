using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLife.Remoting
{
    /// <summary>用户会话接口</summary>
    public interface IUserSession
    {
        /// <summary>是否已登录</summary>
        Boolean Logined { get; }

    }
}