using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XCode.DataAccessLayer;

namespace XCode.Service
{
    /// <summary>登录信息</summary>
    public class LoginInfo
    {
        /// <summary>数据类型</summary>
        public DatabaseType DbType { get; set; }

        /// <summary>版本</summary>
        public String Version { get; set; }
    }
}