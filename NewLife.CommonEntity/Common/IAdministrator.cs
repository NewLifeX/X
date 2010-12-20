using System;
using System.Collections.Generic;
using System.Text;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 管理员接口
    /// </summary>
    interface IAdministrator
    {
        /// <summary>
        /// 创建指定类型指定动作的日志实体
        /// </summary>
        /// <param name="type"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        IEntity CreateLog(Type type, String action);
    }
}