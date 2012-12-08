using System;

namespace NewLife.Net.Sdp
{
    /// <summary>SDP Connection</summary>
    public class SdpConnection
    {
        #region 属性
        private String _NetType;
        /// <summary>网络类型</summary>
        public String NetType { get { return _NetType; } set { _NetType = value; } }

        private String _AddressType;
        /// <summary>地址类型</summary>
        public String AddressType { get { return _AddressType; } set { _AddressType = value; } }

        private String _Address;
        /// <summary>地址</summary>
        public String Address { get { return _Address; } set { _Address = value; } }
        #endregion

        #region 方法
        /// <summary>分析</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static SdpConnection Parse(String value)
        {
            if (String.IsNullOrEmpty(value)) throw new ArgumentNullException("value");

            value = value.Trim();

            if (!value.ToLower().StartsWith("c=")) throw new NetException("Invalid SDP Connection('c=') value '" + value + "'.");

            value = value.Substring(2);

            string[] values = value.Split(' ');
            if (values.Length != 3) throw new NetException("Invalid SDP Connection('c=') value '" + value + "'.");

            var entity = new SdpConnection();
            entity.NetType = values[0];
            entity.AddressType = values[1];
            entity.Address = values[2];

            return entity;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "c=" + NetType + " " + AddressType + " " + Address + "\r\n";
        }
        #endregion
    }
}
