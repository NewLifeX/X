using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace NewLife.Net
{
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
        public Int32 ProcessId { get; }

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
        public static TcpConnectionInformation2[] GetAllTcpConnections()
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
            if (ret != 0 && ret != 122) // 122 insufficient buffer size
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
                        typeof(MIB_TCPTABLE_OWNER_PID));
                var rowPtr = (IntPtr)((Int64)buffTable +
                    Marshal.SizeOf(tab.dwNumEntries));
                //tTable = new MIB_TCPROW_OWNER_PID[tab.dwNumEntries];

                for (var i = 0; i < tab.dwNumEntries; i++)
                {
                    var tcpRow = (MIB_TCPROW_OWNER_PID)Marshal
                        .PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_PID));
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
    }
}