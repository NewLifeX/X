using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace NewLife.Net.UPnP
{
    /// <summary>端口映射实体</summary>
    public class PortMappingEntryRequest : PortMappingEntry
    {
        private Int32 _NewPortMappingIndex;
        /// <summary>索引</summary>
        public Int32 NewPortMappingIndex
        {
            get { return _NewPortMappingIndex; }
            set { _NewPortMappingIndex = value; }
        }
    }

    /// <summary>端口映射实体</summary>
    public class PortMappingEntry : UPnPAction<PortMappingEntry>
    {
        #region 属性
        private String _RemoteHost;
        /// <summary>远程主机</summary>
        [XmlElement("NewRemoteHost")]
        public String RemoteHost
        {
            get { return _RemoteHost; }
            set { _RemoteHost = value; }
        }

        private Int32 _ExternalPort;
        /// <summary>外部端口</summary>
        [XmlElement("NewExternalPort")]
        public Int32 ExternalPort
        {
            get { return _ExternalPort; }
            set { _ExternalPort = value; }
        }

        private String _Protocol;
        /// <summary>TCP/UDP</summary>
        [XmlElement("NewProtocol")]
        public String Protocol
        {
            get { return _Protocol; }
            set { _Protocol = value; }
        }

        private Int32 _InternalPort;
        /// <summary>内部端口</summary>
        [XmlElement("NewInternalPort")]
        public Int32 InternalPort
        {
            get { return _InternalPort; }
            set { _InternalPort = value; }
        }

        private String _InternalClient;
        /// <summary>主机IP</summary>
        [XmlElement("NewInternalClient")]
        public String InternalClient
        {
            get { return _InternalClient; }
            set { _InternalClient = value; }
        }

        private Int32 _Enabled;
        /// <summary>是否启用</summary>
        [XmlElement("NewEnabled")]
        public Int32 Enabled
        {
            get { return _Enabled; }
            set { _Enabled = value; }
        }

        private String _Description;
        /// <summary>描述</summary>
        [XmlElement("NewPortMappingDescription")]
        public String Description
        {
            get { return _Description; }
            set { _Description = value; }
        }

        private Int32 _LeaseDuration;
        /// <summary>有效期</summary>
        [XmlElement("NewLeaseDuration")]
        public Int32 LeaseDuration
        {
            get { return _LeaseDuration; }
            set { _LeaseDuration = value; }
        }
        #endregion

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0} {1}://{2}:{3} {4}", Description, Protocol, InternalClient, InternalPort, ExternalPort);
        }
    }
}