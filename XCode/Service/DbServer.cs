using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Remoting;

namespace XCode.Service
{
    /// <summary>数据服务器</summary>
    public class DbServer : ApiServer
    {
        /// <summary>实例化数据服务</summary>
        public DbServer()
        {
            Port = 3305;
        }
    }
}
