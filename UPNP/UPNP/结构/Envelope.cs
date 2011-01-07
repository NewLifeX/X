using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace NewLife.UPNP
{
    /// <summary>
    /// 端口映射结构
    /// </summary>
    [Serializable, XmlRoot("Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class Envelope
    {

        private List<PortMappingEntry> _Body;
        /// <summary>属性说明</summary>
        [XmlArray("Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        [XmlArrayItem("GetGenericPortMappingEntryResponse",Namespace="urn:schemas-upnp-org:service:WANIPConnection:1")]
        public List<PortMappingEntry> Body
        {
            get { return _Body; }
            set { _Body = value; }
        }
        
    }
}
