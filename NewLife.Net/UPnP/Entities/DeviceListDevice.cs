using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace NewLife.Net.UPnP
{
    /// <summary>
    /// DeviceListDevice
    /// </summary>
    public class DeviceListDevice
    {
        private String _deviceType;
		/// <summary>设备类型</summary>

		public String deviceType 
		{ 
			get{return _deviceType;} 
			set{_deviceType=value;} 
		}

        private String _friendlyName;
		/// <summary>对于用户的简短描述</summary>
		public String friendlyName 
		{ 
			get{return _friendlyName;} 
			set{_friendlyName=value;} 
		}

        private String _manufacturer;
		/// <summary>制造商名称</summary>
		public String manufacturer 
		{ 
			get{return _manufacturer;} 
			set{_manufacturer=value;} 
		}

        private String _manufacturerURL;
		/// <summary>制造商网址</summary>
		public String manufacturerURL 
		{ 
			get{return _manufacturerURL;} 
			set{_manufacturerURL=value;} 
		}

        private String _modelDescription;
		/// <summary>对于用户的长篇描述</summary>
		public String modelDescription 
		{ 
			get{return _modelDescription;} 
			set{_modelDescription=value;} 
		}

        private String _modelName;
		/// <summary>型号名称</summary>
		public String modelName 
		{ 
			get{return _modelName;} 
			set{_modelName=value;} 
		}

        private String _modelNumber;
		/// <summary>型号</summary>
		public String modelNumber 
		{ 
			get{return _modelNumber;} 
			set{_modelNumber=value;} 
		}

        private String _modelURL;
		/// <summary>型号网址</summary>
		public String modelURL 
		{ 
			get{return _modelURL;} 
			set{_modelURL=value;} 
		}

        private String _serialNumber;
		/// <summary>序列号</summary>
		public String serialNumber 
		{ 
			get{return _serialNumber;} 
			set{_serialNumber=value;} 
		}

        private String _UDN;
		/// <summary>唯一设备名称</summary>
		public String UDN 
		{ 
			get{return _UDN;} 
			set{_UDN=value;} 
		}

        private String _UPC;
		/// <summary>通用产品编码缩写</summary>
		public String UPC 
		{ 
			get{return _UPC;} 
			set{_UPC=value;} 
		}

        private List<Service> _serviceList;
        /// <summary>服务列表</summary>
        [XmlArray("serviceList")]
        [XmlArrayItem("service")]
        public List<Service> serviceList
        {
            get { return _serviceList; }
            set { _serviceList = value; }
        }

        private List<DeviceListDevice> _deviceList;
        /// <summary>设备列表</summary>
        [XmlArray("deviceList")]
        [XmlArrayItem("device")]
        public List<DeviceListDevice> deviceList
        {
            get { return _deviceList; }
            set { _deviceList = value; }
        }

    }
}
