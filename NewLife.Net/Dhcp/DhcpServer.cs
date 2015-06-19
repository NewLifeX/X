using System.Net.Sockets;
using NewLife.Net.Sockets;

namespace NewLife.Net.Dhcp
{
    /// <summary>DHCP服务器</summary>
    public class DhcpServer : NetServer<DhcpSession>
    {
        /// <summary>实例化DHCP服务器</summary>
        public DhcpServer()
        {
            ProtocolType = ProtocolType.Udp;
            Port = 67;
        }

        /// <summary>确保创建服务器</summary>
        public override void EnsureCreateServer()
        {
            var count = Servers.Count;

            base.EnsureCreateServer();

            // 附加多一个端口
            if (count == 0)
            {
                var list = CreateServer(Local.Address, 68, Local.ProtocolType, AddressFamily);
                foreach (var item in list)
                {
                    AttachServer(item);
                }
            }
        }
    }
}