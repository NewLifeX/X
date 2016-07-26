using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace NewLife.iOS.Net
{
    /// <summary>
    /// iOS设备的IP定位器
    /// </summary>
    public class iOSIpResolver : NetHelper.IIpResolver
    {
        /// <summary>
        /// 获取本机IP
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IPAddress> GetIPs()
        {
            foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (netInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                    netInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (var addrInfo in netInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (addrInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            yield return addrInfo.Address;
                        }
                    }
                }
            }
        }
    }
}
