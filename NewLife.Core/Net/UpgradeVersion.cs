using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace NewLife.Net
{
    /// <summary>更新版本信息</summary>
    public class UpgradeVersion
    {
        #region 属性
        private String _Name;
        /// <summary>更新包名称</summary>
        [Description("更新包名称")]
        public String Name { get { return _Name; } set { _Name = value; } }

        private Version _MinVersion;
        /// <summary>最低要求版本</summary>
        [Description("最低要求版本")]
        public Version MinVersion { get { return _MinVersion; } set { _MinVersion = value; } }

        private Version _Version;
        /// <summary>新版本</summary>
        [Description("新版本")]
        public Version Version { get { return _Version; } set { _Version = value; } }

        private String _Url;
        /// <summary>更新包地址</summary>
        [Description("更新包地址")]
        public String Url { get { return _Url; } set { _Url = value; } }

        private Int32 _Size;
        /// <summary>更新包大小</summary>
        [Description("更新包大小")]
        public Int32 Size { get { return _Size; } set { _Size = value; } }

        private String _Crc;
        /// <summary>更新包校验</summary>
        [Description("更新包校验")]
        public String Crc { get { return _Crc; } set { _Crc = value; } }

        private String _Upgrader;
        /// <summary>更新程序</summary>
        [Description("更新程序")]
        public String Upgrader { get { return _Upgrader; } set { _Upgrader = value; } }

        private String _UpgraderUrl;
        /// <summary>更新程序的地址</summary>
        [Description("更新程序的地址")]
        public String UpgraderUrl { get { return _UpgraderUrl; } set { _UpgraderUrl = value; } }

        private String _UpgraderCrc;
        /// <summary>更新程序的校验</summary>
        [Description("更新程序的校验")]
        public String UpgraderCrc { get { return _UpgraderCrc; } set { _UpgraderCrc = value; } }

        private String _Description;
        /// <summary>描述信息</summary>
        [Description("描述信息")]
        public String Description { get { return _Description; } set { _Description = value; } }
        #endregion

        #region 方法
        #endregion
    }
}