using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;

namespace NewLife.Remoting
{
    class ApiHttpClient : IApiClient
    {
        public void Open()
        {
        }

        public void Close()
        {
        }

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}