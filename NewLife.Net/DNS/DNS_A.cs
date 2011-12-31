using System;
using System.Net;

namespace NewLife.Net.DNS
{
    /// <summary>A记录</summary>
    /// <remarks>
    /// 查询的时候只需要设置<see cref="DNSEntity.Name"/>，返回的数据里面，<see cref="Address"/>和<see cref="DNSEntity.TTL"/>最有价值。
    /// </remarks>
    public class DNS_A : DNSBase<DNS_A>
    {
        #region 属性
        /// <summary>IP地址</summary>
        public IPAddress Address
        {
            get { return !String.IsNullOrEmpty(DataString) ? IPAddress.Parse(DataString) : null; }
            set { DataString = value.ToString(); }
        }
        #endregion

        #region 构造
        /// <summary>构造一个A记录实例</summary>
        public DNS_A()
        {
            Type = DNSQueryType.A;
            Class = DNSQueryClass.IN;
        }
        #endregion
    }
}