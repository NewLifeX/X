using System;
using System.Net;
using NewLife.Serialization;

namespace NewLife.Net.DNS
{
    /// <summary>A记录</summary>
    /// <remarks>
    /// 查询的时候只需要设置<see cref="DNSEntity.Name"/>，返回的数据里面，<see cref="Address"/>和<see cref="DNSRecord.TTL"/>最有价值。
    /// </remarks>
    public class DNS_A : DNSRecord
    {
        #region 属性
        [FieldSize("_Length")]
        private IPAddress _Address;
        /// <summary>IP地址</summary>
        public IPAddress Address { get { return _Address; } set { _Address = value; } }

        /// <summary>文本信息</summary>
        public override String Text { get { return Address + ""; } set { Address = String.IsNullOrEmpty(value) ? null : IPAddress.Parse(value); } }
        #endregion

        #region 构造
        /// <summary>构造一个A记录实例</summary>
        public DNS_A()
        {
            Type = DNSQueryType.A;
            Class = DNSQueryClass.IN;
        }
        #endregion

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            // Address为空，可能是请求
            if (Address == null)
                return String.Format("{0} {1}", Type, Name);
            else
                return String.Format("{0} {1}", Type, Address);
        }
    }
}