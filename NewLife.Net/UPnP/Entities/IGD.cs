using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace NewLife.Net.UPnP
{
    [Serializable, XmlRoot("root", Namespace = "urn:schemas-upnp-org:device-1-0")]
    public class IGD
    {
        private String _ServerHOST;
        /// <summary>UPNP设备IP</summary>
        public String ServerHOST
        {
            get
            {
                if (String.IsNullOrEmpty(_ServerHOST))
                    GetHOSTAndPort();
                return _ServerHOST;

            }
            set { _ServerHOST = value; }
        }

        private Int32 _ServerPort;
        /// <summary>UPNP设备端品</summary>
        public Int32 ServerPort
        {
            get
            {
                if (_ServerPort == 0)
                    GetHOSTAndPort();
                return _ServerPort;
            }
            set { _ServerPort = value; }
        }

        private SpecVersion _specVersion;
        /// <summary>UPnP 设备架构版本</summary>
        [XmlElement("specVersion")]
        public SpecVersion specVersion
        {
            get { return _specVersion; }
            set { _specVersion = value; }
        }

        private String _URLBase;
        /// <summary>URL</summary>
        [XmlElement("URLBase")]
        public String URLBase
        {
            get { return _URLBase; }
            set { _URLBase = value; }
        }

        private Device _device;
        /// <summary>设备</summary>
        [XmlElement("device")]
        public Device device
        {
            get { return _device; }
            set { _device = value; }
        }

        /// <summary>
        /// 分析URLBase 并赋值HOST和Port
        /// </summary>
        public void GetHOSTAndPort()
        {
            Regex Regex = new Regex(@"\d{1,3}.\d{1,3}.\d{1,3}.\d{1,3}:\d{1,5}");
            String HostAndPortStr = Regex.Match(URLBase).Value;
            if (!String.IsNullOrEmpty(HostAndPortStr))
            {
                String[] Arr = HostAndPortStr.Split(new Char[] { ':' });
                ServerHOST = Arr[0];
                ServerPort = Convert.ToInt32(Arr[1]);
            }
            else
                throw new Exception("UPNP设备IP与端口获取出错!");

        }

    }
}
