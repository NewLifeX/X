using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace NewLife.Net.UPnP
{
    /// <summary>设备</summary>
    public class Device
    {
        #region 属性
        private String _deviceType;
        /// <summary>设备类型</summary>
        public String deviceType
        {
            get { return _deviceType; }
            set { _deviceType = value; }
        }

        private String _presentationURL;
        /// <summary>管理网址</summary>
        public String presentationURL
        {
            get { return _presentationURL; }
            set { _presentationURL = value; }
        }

        private String _friendlyName;
        /// <summary>对于用户的简短描述</summary>
        public String friendlyName
        {
            get { return _friendlyName; }
            set { _friendlyName = value; }
        }

        private String _manufacturer;
        /// <summary>生产厂家</summary>
        public String manufacturer
        {
            get { return _manufacturer; }
            set { _manufacturer = value; }
        }

        private String _manufacturerURL;
        /// <summary>制造商的网址</summary>
        public String manufacturerURL
        {
            get { return _manufacturerURL; }
            set { _manufacturerURL = value; }
        }

        private String _modelDescription;
        /// <summary>描述</summary>
        public String modelDescription
        {
            get { return _modelDescription; }
            set { _modelDescription = value; }
        }

        private String _modelName;
        /// <summary>产品名称</summary>
        public String modelName
        {
            get { return _modelName; }
            set { _modelName = value; }
        }

        private String _modelNumber;
        /// <summary>产品型号</summary>
        public String modelNumber
        {
            get { return _modelNumber; }
            set { _modelNumber = value; }
        }

        private String _UDN;
        /// <summary>唯一设备名称</summary>
        public String UDN
        {
            get { return _UDN; }
            set { _UDN = value; }
        }

        private String _UPC;
        /// <summary>通用产品编码缩写</summary>
        public String UPC
        {
            get { return _UPC; }
            set { _UPC = value; }
        }

        private List<Service> _serviceList;
        /// <summary>服务项目</summary>
        [XmlArray("serviceList")]
        [XmlArrayItem("service")]
        public List<Service> serviceList
        {
            get { return _serviceList; }
            set { _serviceList = value; }
        }

        private List<Device> _deviceList;
        /// <summary>设备 仅当根设备带有嵌入式设备时要求</summary>
        [XmlArray("deviceList")]
        [XmlArrayItem("device")]
        public List<Device> deviceList
        {
            get { return _deviceList; }
            set { _deviceList = value; }
        }
        #endregion

        #region 方法
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            //return String.Format("{0} {1}", friendlyName, manufacturer);
            return friendlyName;
        }
        #endregion

        #region 设备/服务
        /// <summary>获取指定设备指定类型的服务</summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public Service GetService(String serviceType) { return GetService(this, serviceType); }

        static Service GetService(Device device, String serviceType)
        {
            if (device == null || device.serviceList == null || device.serviceList.Count < 1) return null;

            foreach (var item in device.serviceList)
            {
                if (item.serviceType.EqualIgnoreCase(serviceType)) return item;
            }

            if (device.deviceList == null || device.deviceList.Count < 1) return null;

            foreach (var item in device.deviceList)
            {
                Service service = GetService(item, serviceType);
                if (service != null) return service;
            }

            return null;
        }

        /// <summary>取得广域网IP连接设备</summary>
        /// <returns></returns>
        public Service GetWANIPService()
        {
            return GetService("urn:schemas-upnp-org:service:WANIPConnection:1");
        }
        #endregion
    }
}