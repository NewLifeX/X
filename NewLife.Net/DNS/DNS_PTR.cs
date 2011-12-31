using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using NewLife.Serialization;

namespace NewLife.Net.Protocols.DNS
{
    /// <summary>PTR记录</summary>
    /// <remarks>
    /// 查询的时候只需要设置<see cref="Address"/>，返回的数据里面，<see cref="DomainName"/>最有价值。
    /// </remarks>
    public class DNS_PTR : DNSBase<DNS_PTR>
    {
        #region 属性
        const String _suffix = ".in-addr.arpa";

        /// <summary>IP地址</summary>
        public IPAddress Address
        {
            get
            {
                var name = Name;
                if (String.IsNullOrEmpty(name)) return null;

                if (name.EndsWith(_suffix, StringComparison.OrdinalIgnoreCase)) name = name.Substring(0, name.Length - _suffix.Length);

                IPAddress addr;
                if (!IPAddress.TryParse(name, out addr)) return null;
                return addr;
            }
            set
            {
                if (value != null)
                {
                    var bts = value.GetAddressBytes();
                    // 倒序
                    Array.Reverse(bts);
                    // 重新变成地址
                    var addr = new IPAddress(bts);
                    Name = addr + _suffix;
                }
                else
                    Name = null;
            }
        }

        /// <summary>域名</summary>
        public String DomainName { get { return DataString; } set { DataString = value; } }
        #endregion

        #region 构造
        /// <summary>构造一个A记录实例</summary>
        public DNS_PTR()
        {
            Type = DNSQueryType.PTR;
            Class = DNSQueryClass.IN;
        }
        #endregion
    }
}