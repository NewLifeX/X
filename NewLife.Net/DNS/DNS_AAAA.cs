
namespace NewLife.Net.Protocols.DNS
{
    /// <summary>A记录</summary>
    public class DNS_AAAA : DNS_A
    {
        #region 构造
        /// <summary>构造一个AAAA记录实例</summary>
        public DNS_AAAA()
        {
            Type = DNSQueryType.AAAA;
            Class = DNSQueryClass.IN;
        }
        #endregion
    }
}