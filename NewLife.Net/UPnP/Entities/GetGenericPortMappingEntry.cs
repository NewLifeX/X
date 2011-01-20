using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Xml;
using System.Xml.Serialization;

namespace NewLife.Net.UPnP
{
    public class PortMappingEntryClient : PortMappingEntry2
    {
        private Int32 _NewPortMappingIndex;
        /// <summary>索引</summary>
        [XmlElement(IsNullable = false, Namespace = null)]
        public Int32 NewPortMappingIndex
        {
            get { return _NewPortMappingIndex; }
            set { _NewPortMappingIndex = value; }
        }
    }

    [XmlRoot("GetGenericPortMappingEntry", Namespace = "urn:schemas-upnp-org:service:WANIPConnection:1")]
    public class PortMappingEntry2
    {
        private String _NewRemoteHost = String.Empty;
        /// <summary>主机</summary>
        [XmlElement(IsNullable = false, Namespace = null)]
        public String NewRemoteHost
        {
            get { return _NewRemoteHost; }
            set { _NewRemoteHost = value; }
        }

        private String _NewExternalPort;
        /// <summary>端口</summary>
        public String NewExternalPort
        {
            get { return _NewExternalPort; }
            set { _NewExternalPort = value; }
        }

        private String _NewProtocol = String.Empty;
        /// <summary>协议</summary>
        public String NewProtocol
        {
            get { return _NewProtocol; }
            set { _NewProtocol = value; }
        }

        private String _NewInternalPort;
        /// <summary>内部端口</summary>
        public String NewInternalPort
        {
            get { return _NewInternalPort; }
            set { _NewInternalPort = value; }
        }

        private String _NewInternalClient = String.Empty;
        /// <summary>内部客户端</summary>
        public String NewInternalClient
        {
            get { return _NewInternalClient; }
            set { _NewInternalClient = value; }
        }

        private String _NewEnabled;
        /// <summary>是否启用</summary>
        public String NewEnabled
        {
            get { return _NewEnabled; }
            set { _NewEnabled = value; }
        }

        private String _NewPortMappingDescription = String.Empty;
        /// <summary>描述</summary>
        public String NewPortMappingDescription
        {
            get { return _NewPortMappingDescription; }
            set { _NewPortMappingDescription = value; }
        }

        private String _NewLeaseDuration = String.Empty;
        /// <summary>属性说明</summary>
        public String NewLeaseDuration
        {
            get { return _NewLeaseDuration; }
            set { _NewLeaseDuration = value; }
        }
    }
}
