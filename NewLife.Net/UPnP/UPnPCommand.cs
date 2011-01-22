using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Services.Protocols;
using System.Web.Services;

namespace NewLife.Net.UPnP
{
    [WebServiceBinding("ipc", Namespace = "urn:schemas-upnp-org:service:WANIPConnection:1")]
    public class UPnPCommand : SoapHttpClientProtocol
    {
        [SoapDocumentMethod("urn:schemas-upnp-org:service:WANIPConnection:1#GetGenericPortMappingEntry", RequestNamespace = "urn:schemas-upnp-org:service:WANIPConnection:1", ParameterStyle = SoapParameterStyle.Bare)]
        public PortMappingEntry GetGenericPortMappingEntry(PortMappingEntryRequest entry)
        {
            return Invoke("GetGenericPortMappingEntry", new Object[] { entry })[0] as PortMappingEntry;
        }
    }
}
