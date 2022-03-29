using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace XUnitTest.Serialization
{
    [XmlRoot("StarAgent")]
    class StarSetting
    {
        #region 属性
        /// <summary>调试开关。默认true</summary>
        [Description("调试开关。默认true")]
        public Boolean Debug { get; set; } = true;

        /// <summary>证书</summary>
        [Description("证书")]
        public String Code { get; set; }

        /// <summary>密钥</summary>
        [Description("密钥")]
        public String Secret { get; set; }

        /// <summary>本地服务。默认udp://127.0.0.1:5500</summary>
        [Description("本地服务。默认udp://127.0.0.1:5500")]
        public String LocalServer { get; set; } = "udp://127.0.0.1:5500";

        /// <summary>更新通道。默认Release</summary>
        [Description("更新通道。默认Release")]
        public String Channel { get; set; } = "Release";

        /// <summary>应用服务集合</summary>
        [Description("应用服务集合")]
        public ServiceInfo[] Services { get; set; }
        #endregion
    }

    /// <summary>应用服务信息</summary>
    public class ServiceInfo
    {
        #region 属性
        /// <summary>名称。全局唯一，默认应用名，根据场景可以加dev等后缀</summary>
        [XmlAttribute]
        public String Name { get; set; }

        /// <summary>文件</summary>
        [XmlAttribute]
        public String FileName { get; set; }

        /// <summary>参数</summary>
        [XmlAttribute]
        public String Arguments { get; set; }

        /// <summary>工作目录</summary>
        [XmlAttribute]
        public String WorkingDirectory { get; set; }

        /// <summary>是否自动启动</summary>
        [XmlAttribute]
        public Boolean AutoStart { get; set; }

        /// <summary>启动失败时的重试次数，默认3次</summary>
        [XmlAttribute]
        public Int32 Retry { get; set; } = 3;

        /// <summary>是否自动重启。应用进程退出后，自动拉起，默认true</summary>
        [XmlAttribute]
        public Boolean AutoRestart { get; set; } = true;

        /// <summary>是否单实例。按文件路径确保唯一实例，默认false</summary>
        [XmlAttribute]
        public Boolean Singleton { get; set; }

        ///// <summary>重启退出代码。仅有该退出代码才会重启</summary>
        //[XmlAttribute]
        //public String RestartExistCodes { get; set; }
        #endregion
    }
}