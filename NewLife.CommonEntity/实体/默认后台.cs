using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 管理员
    /// </summary>
    public class Administrator : Administrator<Administrator, Role, Menu, RoleMenu, Log> { }

    /// <summary>
    /// 菜单
    /// </summary>
    public class Menu : Menu<Menu> { }

    /// <summary>
    /// 地区
    /// </summary>
    public class Role : Role<Role, Menu, RoleMenu> { }

    /// <summary>
    /// 角色和菜单
    /// </summary>
    public class RoleMenu : RoleMenu<RoleMenu> { }

    /// <summary>
    /// 日志
    /// </summary>
    public class Log : Log<Log> { }

    /// <summary>
    /// 地区
    /// </summary>
    public class Area : Area<Area> { }

}