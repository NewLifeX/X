using System;

namespace NewLife.Net.Sdp
{
    /// <summary>SDP Origin。RFC 4566 5.2</summary>
    public class SdpOrigin
    {
        #region 属性
        private String _UserName;
        /// <summary>用户名</summary>
        public String UserName { get { return _UserName; } set { _UserName = value; } }

        private Int64 _SessionID;
        /// <summary>会话编号</summary>
        public Int64 SessionID { get { return _SessionID; } set { _SessionID = value; } }

        private Int64 _SessionVersion;
        /// <summary>会话版本。每次会话数据被修改时都递增该值</summary>
        public Int64 SessionVersion { get { return _SessionVersion; } set { _SessionVersion = value; } }

        private String _NetType;
        /// <summary>网络类型。IN</summary>
        public String NetType { get { return _NetType; } set { _NetType = value; } }

        private String _AddressType;
        /// <summary>地址类型。IP4，IP6</summary>
        public String AddressType { get { return _AddressType; } set { _AddressType = value; } }

        private String _UnicastAddress;
        /// <summary>地址</summary>
        public String UnicastAddress { get { return _UnicastAddress; } set { _UnicastAddress = value; } }
        #endregion

        #region 方法
        /// <summary>分析</summary>
        /// <param name="value">Origin value.</param>
        /// <returns>Returns parsed SDP Origin.</returns>
        public static SdpOrigin Parse(string value)
        {
            if (value == null) throw new ArgumentNullException("value");

            value = value.Trim();

            /* o=<username> <sess-id> <sess-version> <nettype> <addrtype> <unicast-address>
            */

            if (!value.ToLower().StartsWith("o=")) throw new NetException("Invalid SDP Origin('o=') value '" + value + "'.");

            value = value.Substring(2);

            string[] values = value.Split(' ');
            if (values.Length != 6) throw new NetException("Invalid SDP Origin('o=') value '" + value + "'.");

            var so = new SdpOrigin();
            so.UserName = values[0];
            so.SessionID = Convert.ToInt64(values[1]);
            so.SessionVersion = Convert.ToInt64(values[2]);
            so.NetType = values[3];
            so.AddressType = values[4];
            so.UnicastAddress = values[5];
            return so;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "o=" + UserName + " " + SessionID + " " + SessionVersion + " " + NetType + " " + AddressType + " " + UnicastAddress + "\r\n";
        }
        #endregion
    }
}