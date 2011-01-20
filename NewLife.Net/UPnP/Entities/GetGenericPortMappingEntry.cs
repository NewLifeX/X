using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Xml;
using System.Xml.Serialization;

namespace NewLife.Net.UPnP
{
    [XmlRoot("GetGenericPortMappingEntry")]
    public class GetGenericPortMappingEntry : XmlEntity<GetGenericPortMappingEntry>
    {
        private Int32 _NewPortMappingIndex;
        /// <summary>索引</summary>
        public Int32 NewPortMappingIndex
        {
            get { return _NewPortMappingIndex; }
            set { _NewPortMappingIndex = value; }
        }

        private String _NewRemoteHost;
        /// <summary>主机</summary>
        public String NewRemoteHost
        {
            get { return _NewRemoteHost; }
            set { _NewRemoteHost = value; }
        }

        private Int32 _NewExternalPort;
        /// <summary>端口</summary>
        public Int32 NewExternalPort
        {
            get { return _NewExternalPort; }
            set { _NewExternalPort = value; }
        }

        private String _NewProtocol;
        /// <summary>协议</summary>
        public String NewProtocol
        {
            get { return _NewProtocol; }
            set { _NewProtocol = value; }
        }

        private Int32 _NewInternalPort;
        /// <summary>内部端口</summary>
        public Int32 NewInternalPort
        {
            get { return _NewInternalPort; }
            set { _NewInternalPort = value; }
        }

        private String _NewInternalClient;
        /// <summary>内部客户端</summary>
        public String NewInternalClient
        {
            get { return _NewInternalClient; }
            set { _NewInternalClient = value; }
        }

        private Boolean _NewEnabled;
        /// <summary>是否启用</summary>
        public Boolean NewEnabled
        {
            get { return _NewEnabled; }
            set { _NewEnabled = value; }
        }

        private String _NewPortMappingDescription;
        /// <summary>描述</summary>
        public String NewPortMappingDescription
        {
            get { return _NewPortMappingDescription; }
            set { _NewPortMappingDescription = value; }
        }

        private String _NewLeaseDuration;
        /// <summary>属性说明</summary>
        public String NewLeaseDuration
        {
            get { return _NewLeaseDuration; }
            set { _NewLeaseDuration = value; }
        }
    }
}
