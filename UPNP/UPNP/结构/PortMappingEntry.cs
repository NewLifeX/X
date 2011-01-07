using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace NewLife.UPNP
{
    /// <summary>
    /// PortMappingEntry
    /// </summary>
    public class PortMappingEntry
    {

        private String _NewRemoteHost;
        /// <summary>远程主机</summary>
        [XmlElement(Namespace = "")]
        public String NewRemoteHost
        {
            get { return _NewRemoteHost; }
            set { _NewRemoteHost = value; }
        }

        private Int32 _NewExternalPort;
        /// <summary>外部端口</summary>
        [XmlElement(Namespace = "")]
        public Int32 NewExternalPort
        {
            get { return _NewExternalPort; }
            set { _NewExternalPort = value; }
        }

        private String _NewProtocol;
        /// <summary>TCP/UDP</summary>
        [XmlElement(Namespace = "")]
        public String NewProtocol
        {
            get { return _NewProtocol; }
            set { _NewProtocol = value; }
        }

        private Int32 _NewInternalPort;
        /// <summary>内部端口</summary>
        [XmlElement(Namespace = "")]
        public Int32 NewInternalPort
        {
            get { return _NewInternalPort; }
            set { _NewInternalPort = value; }
        }

        private String _NewInternalClient;
        /// <summary>主机IP</summary>
        [XmlElement(Namespace = "")]
        public String NewInternalClient
        {
            get { return _NewInternalClient; }
            set { _NewInternalClient = value; }
        }

        private Int32 _NewEnabled;
        /// <summary>是否启用</summary>
        [XmlElement(Namespace = "")]
        public Int32 NewEnabled
        {
            get { return _NewEnabled; }
            set { _NewEnabled = value; }
        }

        private String _NewPortMappingDescription;
        /// <summary>描述</summary>
        [XmlElement(Namespace = "")]
        public String NewPortMappingDescription
        {
            get { return _NewPortMappingDescription; }
            set { _NewPortMappingDescription = value; }
        }

        private Int32 _NewLeaseDuration;
        /// <summary>有效期</summary>
        [XmlElement(Namespace = "")]
        public Int32 NewLeaseDuration
        {
            get { return _NewLeaseDuration; }
            set { _NewLeaseDuration = value; }
        }
    }
}
