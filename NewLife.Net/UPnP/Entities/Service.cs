using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Net.UPnP
{
    /// <summary>服务项</summary>
    public class Service
    {
        private String _serviceType;
        /// <summary>UPnP 服务类型。不得包含散列符</summary>
        public String serviceType
        {
            get { return _serviceType; }
            set { _serviceType = value; }
        }

        private String _serviceId;
        /// <summary>服务标识符</summary>
        public String serviceId
        {
            get { return _serviceId; }
            set { _serviceId = value; }
        }

        private String _controlURL;
        /// <summary>控制网址</summary>
        public String controlURL
        {
            get { return _controlURL; }
            set { _controlURL = value; }
        }

        private String _eventSubURL;
        /// <summary>事件的URL</summary>
        public String eventSubURL
        {
            get { return _eventSubURL; }
            set { _eventSubURL = value; }
        }

        private String _SCPDURL;
        /// <summary>服务描述的URL</summary>
        public String SCPDURL
        {
            get { return _SCPDURL; }
            set { _SCPDURL = value; }
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (!String.IsNullOrEmpty(serviceId))
            {
                var ss = serviceId.Split(":");
                return ss[ss.Length - 1];
            }
            return base.ToString();
        }
    }
}