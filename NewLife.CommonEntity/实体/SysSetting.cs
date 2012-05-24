using System;
using System.Collections.Generic;
using System.Text;
using XCode;
using NewLife.Reflection;

namespace NewLife.CommonEntity
{
    /// <summary>系统设置。提供系统名称、版本等基本设置。</summary>
    /// <remarks>
    /// 由<see cref="Setting"/>支撑，也可自己扩展<see cref="Setting"/>，然后修改这里的<see cref="Sys"/>。
    /// </remarks>
    public static class SysSetting
    {
        #region 属性
        private static ISetting _Sys;
        /// <summary>系统设置</summary>
        public static ISetting Sys { get { return _Sys ?? (_Sys = Setting.Root.Create("Sys")); } set { _Sys = value; } }

        /// <summary>获取设置</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T Get<T>(String name)
        {
            return Sys.Create(name).Get<T>();
        }

        /// <summary>设置设定项</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public static void Set<T>(String name, T val)
        {
            Sys.Create(name).Set<T>(val);
        }
        #endregion

        #region 静态构造
        static SysSetting()
        {
            var asmx = AssemblyX.Entry;
            var asmx2 = AssemblyX.Executing;
            if (asmx == null) asmx = asmx2;
            Sys
                .Ensure<String>("Name", asmx.Name ?? asmx2.Name, "系统名称")
                .Ensure<String>("Version", asmx.Version ?? asmx2.Version, "系统版本")
                .Ensure<String>("DisplayName", asmx.Title ?? asmx.Name ?? asmx2.Title ?? asmx2.Name, "显示名称")
                .Ensure<String>("Company", asmx.Company ?? asmx2.Company, "公司")
                .Ensure<String>("Address", "新生命开发团队", "地址")
                .Ensure<String>("Tel", "", "电话")
                .Ensure<String>("Fax", "", "传真")
                .Ensure<String>("EMail", "", "电子邮件");
        }
        #endregion

        #region 业务属性
        /// <summary>系统名称</summary>
        public static String Name { get { return Get<String>("Name"); } set { Set<String>("Name", value); } }

        /// <summary>系统版本</summary>
        public static String Version { get { return Get<String>("Version"); } set { Set<String>("Version", value); } }

        /// <summary>显示名称</summary>
        public static String DisplayName { get { return Get<String>("DisplayName"); } set { Set<String>("DisplayName", value); } }

        /// <summary>公司</summary>
        public static String Company { get { return Get<String>("Company"); } set { Set<String>("Company", value); } }

        /// <summary>地址</summary>
        public static String Address { get { return Get<String>("Address"); } set { Set<String>("Address", value); } }

        /// <summary>电话</summary>
        public static String Tel { get { return Get<String>("Tel"); } set { Set<String>("Tel", value); } }

        /// <summary>传真</summary>
        public static String Fax { get { return Get<String>("Fax"); } set { Set<String>("Fax", value); } }

        /// <summary>电子邮件</summary>
        public static String EMail { get { return Get<String>("EMail"); } set { Set<String>("EMail", value); } }
        #endregion

        #region 方法
        #endregion
    }
}