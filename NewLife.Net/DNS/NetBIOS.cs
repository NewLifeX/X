using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NewLife.Net.DNS
{
    /// <summary>NetBIOS名称</summary>
    public class NetBIOS
    {
        /// <summary>查询名称</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public IPAddress QueryName(string name)
        {
            //DnsRequest request = new DnsRequest(new Question(EncodeName(name), DnsType.NB, DnsClass.IN));
            var request = new DNSEntity();
            request.Name = EncodeName(name);
            request.Header.RecursionDesired = true;
            request.Header.Broadcast = true;

            var res = Invoke(request);

            if (res == null || res.Answers.Length == 0) return null;

            var nb = res.Answers[0] as DNS_A;

            if (nb == null) return null;

            return nb.Address;
        }

        /// <summary>注册</summary>
        /// <param name="name">名称</param>
        /// <param name="address"></param>
        /// <returns></returns>
        public bool Register(string name, IPAddress address)
        {
            //DnsRequest request = new DnsRequest(new Question(EncodeName(name), DnsType.NB, DnsClass.IN));
            var request = new DNSEntity();
            request.Name = EncodeName(name);
            request.Header.Opcode = DNSOpcodeType.Registration;
            request.Header.RecursionAvailable = false;
            request.Header.RecursionDesired = false;
            request.Header.Broadcast = false;


            var add = new DNS_NB();
            add.Name = EncodeName(name);
            add.TTL = new TimeSpan(0, 0, 30);
            add.Address = address;

            request.Additionals = new DNSRecord[] { add };

            var res = Invoke(request, false);

            return true;
        }

        static string EncodeName(string domain)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in domain + "                ".Substring(0, 16 - domain.Length))
            {
                byte b = (byte)c;
                char x = (char)((byte)'A' + (((byte)c & 0xF0) >> 4));

                sb.Append(x);

                x = (char)((byte)'A' + ((byte)c & 0x0F));

                sb.Append(x);
            }

            return sb.ToString();
        }

        DNSEntity Invoke(DNSEntity request) { return Invoke(request, true); }

        private static readonly int _maxRetryAttemps = 2;
        internal DNSEntity Invoke(DNSEntity request, bool isQuery)
        {
            int attempts = 0;

            while (attempts <= _maxRetryAttemps)
            {
                byte[] bytes = request.GetStream().ReadBytes();

                if (bytes.Length > 512)
                    throw new ArgumentException("RFC 1035 2.3.4 states that the maximum size of a UDP datagram is 512 octets (bytes).");

                Socket socket = null;

                try
                {
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                    socket.ReceiveTimeout = 300;
                    //socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 300);

                    socket.SendTo(bytes, bytes.Length, SocketFlags.None, new IPEndPoint(IPAddress.Parse("192.168.178.255"), 137));

                    if (!isQuery) return null;

                    // Messages carried by UDP are restricted to 512 bytes (not counting the IP
                    // or UDP headers).  Longer messages are truncated and the TC bit is set in
                    // the header. (RFC 1035 4.2.1)
                    byte[] responseMessage = new byte[512];

                    //int numBytes = socket.Receive(responseMessage);

                    EndPoint ep = (EndPoint)new IPEndPoint(new IPAddress(4294967295), 137);
                    int numBytes = socket.ReceiveFrom(responseMessage, ref ep);

                    if (numBytes == 0 || numBytes > 512)
                        throw new Exception("RFC 1035 2.3.4 states that the maximum size of a UDP datagram is 512 octets (bytes).");

                    //DnsReader br = new DnsReader(responseMessage);
                    //DnsResponse res = new DnsResponse(br);
                    var rs = DNSEntity.Read(responseMessage);

                    if (request.Header.ID == rs.Header.ID) return rs;

                    attempts++;
                }
                catch
                {
                    attempts++;
                }
                finally
                {
                    socket.Close();
                    socket = null;
                }
            }

            throw new Exception("Could not resolve the query (" + attempts + " attempts).");
        }
    }
}