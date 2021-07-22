using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewLife.Configuration
{
    /// <summary>获取配置委托。便于集成配置中心</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public delegate String GetConfigCallback(String key);
}