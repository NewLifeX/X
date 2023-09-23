using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace NewLife.Net;

/// <summary>Tcp连接信息</summary>
public class TcpConnectionInformation2 : TcpConnectionInformation
{
    /// <summary>本地结点</summary>
    public override IPEndPoint LocalEndPoint { get; }

    /// <summary>远程结点</summary>
    public override IPEndPoint RemoteEndPoint { get; }

    /// <summary>Tcp状态</summary>
    public override TcpState State { get; }

    /// <summary>进程标识</summary>
    public Int32 ProcessId { get; set; }

    /// <summary>实例化Tcp连接信息</summary>
    /// <param name="local"></param>
    /// <param name="remote"></param>
    /// <param name="state"></param>
    /// <param name="processId"></param>
    public TcpConnectionInformation2(IPEndPoint local, IPEndPoint remote, TcpState state, Int32 processId)
    {
        LocalEndPoint = local;
        RemoteEndPoint = remote;
        State = state;
        ProcessId = processId;
    }

    private TcpConnectionInformation2(MIB_TCPROW_OWNER_PID row)
    {
        State = (TcpState)row.state;
        var port = (row.localPort1 << 8) | row.localPort2;
        var port2 = (State != TcpState.Listen) ? ((row.remotePort1 << 8) | row.remotePort2) : 0;
        LocalEndPoint = new IPEndPoint(row.localAddr, port);
        RemoteEndPoint = new IPEndPoint(row.remoteAddr, port2);
        ProcessId = row.owningPid;
    }

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => $"{LocalEndPoint}<=>{RemoteEndPoint} {State} {ProcessId}";

    #region Windows连接信息
    private enum TCP_TABLE_CLASS : Int32
    {
        TCP_TABLE_BASIC_LISTENER,
        TCP_TABLE_BASIC_CONNECTIONS,
        TCP_TABLE_BASIC_ALL,
        TCP_TABLE_OWNER_PID_LISTENER,
        TCP_TABLE_OWNER_PID_CONNECTIONS,
        TCP_TABLE_OWNER_PID_ALL,
        TCP_TABLE_OWNER_MODULE_LISTENER,
        TCP_TABLE_OWNER_MODULE_CONNECTIONS,
        TCP_TABLE_OWNER_MODULE_ALL
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_TCPROW_OWNER_PID
    {
        public UInt32 state;
        public UInt32 localAddr;
        public Byte localPort1;
        public Byte localPort2;
        public Byte localPort3;
        public Byte localPort4;
        public UInt32 remoteAddr;
        public Byte remotePort1;
        public Byte remotePort2;
        public Byte remotePort3;
        public Byte remotePort4;
        public Int32 owningPid;

        public UInt16 LocalPort => BitConverter.ToUInt16(new Byte[2] { localPort2, localPort1 }, 0);

        public UInt16 RemotePort => BitConverter.ToUInt16(new Byte[2] { remotePort2, remotePort1 }, 0);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_TCPTABLE_OWNER_PID
    {
        public UInt32 dwNumEntries;
        private MIB_TCPROW_OWNER_PID table;
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern UInt32 GetExtendedTcpTable(IntPtr pTcpTable,
        ref Int32 dwOutBufLen,
        Boolean sort,
        Int32 ipVersion,
        TCP_TABLE_CLASS tblClass,
        Int32 reserved);

    /// <summary>获取所有Tcp连接</summary>
    /// <returns></returns>
    [Obsolete("=>GetWindowsTcpConnections")]
    public static TcpConnectionInformation2[] GetAllTcpConnections() => GetWindowsTcpConnections();

    /// <summary>获取所有Tcp连接</summary>
    /// <returns></returns>
    public static TcpConnectionInformation2[] GetWindowsTcpConnections()
    {
        //MIB_TCPROW_OWNER_PID[] tTable;
        var AF_INET = 2;    // IP_v4
        var buffSize = 0;

        // how much memory do we need?
        var ret = GetExtendedTcpTable(IntPtr.Zero,
            ref buffSize,
            true,
            AF_INET,
            TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL,
            0);
        if (ret is not 0 and not 122) // 122 insufficient buffer size
            throw new Exception("bad ret on check " + ret);
        var buffTable = Marshal.AllocHGlobal(buffSize);

        var list = new List<TcpConnectionInformation2>();
        try
        {
            ret = GetExtendedTcpTable(buffTable,
                ref buffSize,
                true,
                AF_INET,
                TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL,
                0);
            if (ret != 0)
                throw new Exception("bad ret " + ret);

            // get the number of entries in the table
            var tab =
                (MIB_TCPTABLE_OWNER_PID)Marshal.PtrToStructure(
                    buffTable,
                    typeof(MIB_TCPTABLE_OWNER_PID))!;
            var rowPtr = (IntPtr)((Int64)buffTable +
                Marshal.SizeOf(tab.dwNumEntries));
            //tTable = new MIB_TCPROW_OWNER_PID[tab.dwNumEntries];

            for (var i = 0; i < tab.dwNumEntries; i++)
            {
                var tcpRow = (MIB_TCPROW_OWNER_PID)Marshal
                    .PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_PID))!;
                //tTable[i] = tcpRow;
                list.Add(new TcpConnectionInformation2(tcpRow));

                // next entry
                rowPtr = (IntPtr)((Int64)rowPtr + Marshal.SizeOf(tcpRow));
            }
        }
        finally
        {
            // Free the Memory
            Marshal.FreeHGlobal(buffTable);
        }
        //return tTable;
        return list.ToArray();
    }
    #endregion

    #region Linux连接信息
    /// <summary>获取指定进程的Tcp连接</summary>
    /// <param name="processId">目标进程。默认-1未指定，获取所有进程的Tcp连接</param>
    /// <returns></returns>
    public static TcpConnectionInformation2[] GetLinuxTcpConnections(Int32 processId = -1)
    {
        var list = new List<TcpConnectionInformation2>();

        if (processId > 0)
        {
            var rs = ParseTcps(File.ReadAllText($"/proc/{processId}/net/tcp"));
            if (rs != null && rs.Count > 0) list.AddRange(rs);

            var rs2 = ParseTcps(File.ReadAllText($"/proc/{processId}/net/tcp6"));
            if (rs2 != null && rs2.Count > 0) list.AddRange(rs2);

            foreach (var item in list)
            {
                item.ProcessId = processId;
            }
        }
        else
        {
            var rs = ParseTcps(File.ReadAllText("/proc/net/tcp"));
            if (rs != null && rs.Count > 0) list.AddRange(rs);

            var rs2 = ParseTcps(File.ReadAllText("/proc/net/tcp6"));
            if (rs2 != null && rs2.Count > 0) list.AddRange(rs2);
        }

        return list.ToArray();
    }

    /// <summary>分析Tcp连接信息</summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static IList<TcpConnectionInformation2> ParseTcps(String text)
    {
        var list = new List<TcpConnectionInformation2>();

        if (text.IsNullOrEmpty()) return list;

        // 逐行读取TCP连接信息
        foreach (var line in text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4 || parts[1].IndexOf(':') < 0) continue;

            //// 提取连接信息
            //var ps1 = parts[1].Split(':');
            //var ps2 = parts[2].Split(':');
            //if (ps1.Length < 2 || ps2.Length < 2) continue;

            var state = GetState(parts[3]);

            //// IPv4 和 IPv6 解析方式不同
            //if (ps1[0].Length <= 8)
            //{
            //    var localAddress = new IPAddress(ps1[0].ToHex().Reverse().ToArray());
            //    var local = new IPEndPoint(localAddress, Int32.Parse(ps1[1], NumberStyles.HexNumber));
            //    var remoteAddress = new IPAddress(ps2[0].ToHex().Reverse().ToArray());
            //    var remote = new IPEndPoint(remoteAddress, Int32.Parse(ps2[1], NumberStyles.HexNumber));
            //    var info = new TcpConnectionInformation2(local, remote, state, 0);

            //    list.Add(info);
            //}
            //else
            //{
            //    var localAddress = GetIPv6(ps1[0]);
            //    var local = new IPEndPoint(localAddress, Int32.Parse(ps1[1], NumberStyles.HexNumber));

            //    var remoteAddress = GetIPv6(ps2[0]);
            //    var remote = new IPEndPoint(remoteAddress, Int32.Parse(ps2[1], NumberStyles.HexNumber));

            //    var info = new TcpConnectionInformation2(local, remote, state, 0);

            //    list.Add(info);
            //}

            var local = ParseAddressAndPort(parts[1]);
            var remote = ParseAddressAndPort(parts[2]);
            var info = new TcpConnectionInformation2(local, remote, state, 0);
            list.Add(info);
        }

        return list;
    }

    //private static IPAddress GetIPv6(String hex)
    //{
    //    var buf = hex.ToHex();
    //    var numbers = new List<UInt32>();
    //    // 拆分为4个切片，保持字节顺序不变
    //    for (var i = 0; i < buf.Length; i += 4)
    //    {
    //        numbers.Add(buf.ToUInt32(i, false));
    //    }
    //    // 重新打包，每个切片都转换为本机字节顺序
    //    for (var i = 0; i < numbers.Count; i++)
    //    {
    //        buf.Write(i * 4, numbers[i].GetBytes(true));
    //    }
    //    var address = new IPAddress(buf);
    //    if (address.IsIPv4MappedToIPv6)
    //        address = address.MapToIPv4();
    //    //else if (buf[8] == 0xFF && buf[9] == 0xFF)
    //    //    address = new IPAddress(new[] { buf[15], buf[14], buf[13], buf[12] });

    //    return address;
    //}

    private static IPEndPoint ParseAddressAndPort(String colonSeparatedAddress)
    {
        var num = colonSeparatedAddress.IndexOf(':');
        if (num == -1) throw new NetworkInformationException();

        var address = ParseHexIPAddress(colonSeparatedAddress.Substring(0, num));
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        var s = colonSeparatedAddress.Substring(num + 1);
        return !Int32.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result)
            ? throw new NetworkInformationException()
            : new IPEndPoint(address, result);
    }

    internal static IPAddress ParseHexIPAddress(String remoteAddressString)
    {
        if (remoteAddressString.Length <= 8) return ParseIPv4HexString(remoteAddressString);

        if (remoteAddressString.Length == 32) return ParseIPv6HexString(remoteAddressString);

        throw new NetworkInformationException();
    }

    private static IPAddress ParseIPv4HexString(String hexAddress)
    {
        return !Int64.TryParse(hexAddress, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result)
            ? throw new NetworkInformationException()
            : new IPAddress(result);
    }

    private static IPAddress ParseIPv6HexString(String hexAddress, Boolean isNetworkOrder = false)
    {
        var span = hexAddress.ToHex();
        if (!isNetworkOrder && BitConverter.IsLittleEndian)
        {
            for (var j = 0; j < 4; j++)
            {
                Array.Reverse(span, j * 4, 4);
            }
        }

        return new IPAddress(span);
    }

    private static TcpState GetState(String hexState)
    {
        return hexState switch
        {
            "01" => TcpState.Established,
            "02" => TcpState.SynSent,
            "03" => TcpState.SynReceived,
            "04" => TcpState.FinWait1,
            "05" => TcpState.FinWait2,
            "06" => TcpState.TimeWait,
            "07" => TcpState.Closed,
            "08" => TcpState.CloseWait,
            "09" => TcpState.LastAck,
            "0A" => TcpState.Listen,
            "0B" => TcpState.Closing,
            _ => TcpState.Unknown,
        };
    }
    #endregion
}