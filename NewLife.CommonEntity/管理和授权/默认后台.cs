using System;
using XCode;
using XCode.DataAccessLayer;

namespace NewLife.CommonEntity
{
    /// <summary>管理员</summary>
    [Serializable]
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Administrator : Administrator<Administrator> { }

    /// <summary>菜单</summary>
    [Serializable]
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Menu : Menu<Menu> { }

    /// <summary>角色</summary>
    [Serializable]
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Role : Role<Role> { }

    ///// <summary>角色和菜单</summary>
    //[Serializable]
    //[ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    //public class RoleMenu : RoleMenu<RoleMenu> { }

    /// <summary>日志</summary>
    [Serializable]
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Log : Log<Log> { }

    /// <summary>地区</summary>
    [Serializable]
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    public class Area : Area<Area> { }

    /// <summary>手册</summary>
    [ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]
    [BindTable("CommonManual", Description = "手册", ConnName = "CommonManual", DbType = DatabaseType.SqlServer)]
    public class CommonManual : Manual<CommonManual> { }
}