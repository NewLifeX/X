using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.CommonEntity
{
    //TODO 需要增加一个系统参数的实体，因为各种情况需要用到系统参数设置
    //TODO 需要增加一个用户预置文件UserProfile，以用于处理一些用户特性

    /// <summary>
    /// 管理员
    /// </summary>
    [Serializable]
    public class Administrator : Administrator<Administrator, Role, Menu, RoleMenu, Log> { }

    /// <summary>
    /// 菜单
    /// </summary>
    [Serializable]
    public class Menu : Menu<Menu> { }

    /// <summary>
    /// 地区
    /// </summary>
    [Serializable]
    public class Role : Role<Role, Menu, RoleMenu> { }

    /// <summary>
    /// 角色和菜单
    /// </summary>
    [Serializable]
    public class RoleMenu : RoleMenu<RoleMenu> { }

    /// <summary>
    /// 日志
    /// </summary>
    [Serializable]
    public class Log : Log<Log> { }

    /// <summary>
    /// 地区
    /// </summary>
    [Serializable]
    public class Area : Area<Area> { }

}