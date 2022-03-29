﻿using System;
using System.ComponentModel;
using System.Linq;
using NewLife.Configuration;
using NewLife.Reflection;
using NewLife.Security;

namespace NewLife.Common
{
    /// <summary>系统设置。提供系统名称、版本等基本设置</summary>
    [DisplayName("系统设置")]
    public class SysConfig : Config<SysConfig>
    {
        #region 属性
        /// <summary>系统名称</summary>
        [DisplayName("系统名称")]
        [Description("用于标识系统的英文名")]
        public String Name { get; set; } = "";

        /// <summary>系统版本</summary>
        [DisplayName("系统版本")]
        public String Version { get; set; } = "";

        /// <summary>显示名称</summary>
        [DisplayName("显示名称")]
        [Description("用户可见的名称")]
        public String DisplayName { get; set; } = "";

        /// <summary>公司</summary>
        [DisplayName("公司")]
        public String Company { get; set; } = "";

        /// <summary>应用实例。单应用多实例部署时用于唯一标识实例节点</summary>
        [DisplayName("应用实例。单应用多实例部署时用于唯一标识实例节点")]
        public Int32 Instance { get; set; }

        /// <summary>开发者模式</summary>
        [DisplayName("开发者模式")]
        public Boolean Develop { get; set; } = true;

        /// <summary>启用</summary>
        [DisplayName("启用")]
        public Boolean Enable { get; set; } = true;

        /// <summary>安装时间</summary>
        [DisplayName("安装时间")]
        public DateTime InstallTime { get; set; } = DateTime.Now;
        #endregion

        #region 方法
        /// <summary>加载后触发</summary>
        protected override void OnLoaded()
        {
            if (IsNew)
            {
                var asmx = SysAssembly;

                Name = asmx?.Name ?? "NewLife.Cube";
                Version = asmx?.Version ?? "0.1";
                DisplayName = (asmx?.Title ?? asmx?.Name) ?? "魔方平台";
                Company = asmx?.Company ?? "新生命开发团队";
                //Address = "新生命开发团队";

                if (DisplayName.IsNullOrEmpty()) DisplayName = "系统设置";
            }

            // 本地实例，取IPv4地址后两段
            if (Instance <= 0)
            {
                try
                {
                    var ip = NetHelper.MyIP();
                    var buf = ip.GetAddressBytes();
                    Instance = (buf[2] << 8) | buf[3];
                }
                catch
                {
                    // 异常时随机
                    Instance = Rand.Next(1, 1024);
                }
            }

            base.OnLoaded();
        }

        /// <summary>系统主程序集</summary>
        public static AssemblyX SysAssembly
        {
            get
            {
                try
                {
                    var asm = AssemblyX.Entry;
                    if (asm != null) return asm;

                    var list = AssemblyX.GetMyAssemblies();

                    // 最后编译那一个
                    list = list.OrderByDescending(e => e.Compile)
                        .ThenByDescending(e => e.Name.EndsWithIgnoreCase("Web"))
                        .ToList();

                    return list.FirstOrDefault();

                }
                catch { return null; }
            }
        }
        #endregion
    }
}