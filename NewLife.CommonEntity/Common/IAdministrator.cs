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
        /// 根据权限名（权限路径）找到权限菜单实体
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IEntity FindPermissionMenu(String name);

        /// <summary>
        /// 申请指定菜单指定操作的权限
        /// </summary>
        /// <param name="menuID"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        Boolean Acquire(Int32 menuID, PermissionFlags flag);

        /// <summary>
        /// 创建指定类型指定动作的日志实体
        /// </summary>
        /// <param name="type"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        IEntity CreateLog(Type type, String action);
    }
}