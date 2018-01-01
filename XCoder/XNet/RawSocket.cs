using System;
using System.Net;
using System.Net.Sockets;
using NewLife.Data;

namespace XCoder.XNet
{
    /// <summary>原始数据Socket。用于抓包</summary>
    public class RawSocket
    {
        private Socket _socket;
        private IPAddress _address;

        public Action<TcpPacket> OnTcpReceive;

        public Action<Byte[], Int32> OnReceive;

        public RawSocket(IPAddress address)
        {
            _address = address;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
            _socket.Bind(new IPEndPoint(address, 0));
        }

        public Boolean Capture()
        {
            try
            {
                _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, 1);
                var inBytes = new Byte[] { 1, 0, 0, 0 };
                var outBytes = new Byte[] { 0, 0, 0, 0 };
                _socket.IOControl(IOControlCode.ReceiveAll, inBytes, outBytes);
                if (0 != outBytes[0] + outBytes[1] + outBytes[2] + outBytes[3]) return false;
            }
            catch (SocketException)
            {
                return false;
            }

            while (_socket != null)
            {
                var buffer = new Byte[1500];

                var count = _socket.Receive(buffer, SocketFlags.None);
                var pk = new Packet(buffer, 0, count);

                OnReceive?.Invoke(buffer, count);

                if (OnTcpReceive != null)
                {
                    var ip = new IPPacket(pk);
                    if (ip.Protocol == ProtocolType.Tcp)
                    {
                        var tcp = new TcpPacket(ip);
                        OnTcpReceive(tcp);
                    }
                }
            }

            return true;
        }

        public void Stop()
        {
            var s = _socket;
            if (s != null)
            {
                _socket = null;
                s.Shutdown(SocketShutdown.Both);
                s.Close();
            }
        }
    }
}