using System;
using System.Collections.Generic;
using System.Text;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>管理员</summary>>
    [Serializable]
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Administrator : Administrator<Administrator, Role, Menu, RoleMenu, Log> { }

    /// <summary>菜单</summary>>
    [Serializable]
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Menu : Menu<Menu> { }

    /// <summary>角色</summary>>
    [Serializable]
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Role : Role<Role, Menu, RoleMenu> { }

    /// <summary>角色和菜单</summary>>
    [Serializable]
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class RoleMenu : RoleMenu<RoleMenu> { }

    /// <summary>日志</summary>>
    [Serializable]
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Log : Log<Log> { }

    /// <summary>地区</summary>>
    [Serializable]
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Area : Area<Area> { }
}