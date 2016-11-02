using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Net;

namespace NewLife.Remoting
{
    class ApiNetServer : NetServer<ApiNetSession>, IApiServer
    {
        public ApiNetServer()
        {
            Name = "ApiNet";
        }

        /// <summary>初始化</summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public Boolean Init(String config)
        {
            Local = new NetUri(config);

            return true;
        }
    }

    class ApiNetSession : NetSession, IApiSession
    {

    }
}
