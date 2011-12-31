using System;
using System.Net;

namespace NewLife.Net.DNS
{
    /// <summary>MX记录</summary>
    public class DNS_MX : DNSBase<DNS_MX>
    {
        #region 属性
        /// <summary>引用</summary>
        public Int16 Preference
        {
            get
            {
                var aw = GetAnswer();
                if (aw == null || aw.Data == null || aw.Data.Length < 2) return 0;

                var data = new Byte[2];
                aw.Data.CopyTo(data, 0);
                return BitConverter.ToInt16(data, 0);
            }
            set
            {
                var aw = GetAnswer(true);
                var data = BitConverter.GetBytes(value);
                if (aw.Data == null || aw.Data.Length <= 2)
                    aw.Data = data;
                else
                {
                    aw.Data[0] = data[0];
                    aw.Data[1] = data[1];
                }
            }
        }

        /// <summary>主机</summary>
        public String Host
        {
            get
            {
                var aw = GetAnswer();
                if (aw == null || aw.Data == null || aw.Data.Length < 2) return null;

                throw new NetException("这里还需要解析主机！用到再做！");
                //return aw != null ? aw.DataString : null;
            }
            set { GetAnswer(true).DataString = value; }
        }
        #endregion

        #region 构造
        /// <summary>构造一个MX记录实例</summary>
        public DNS_MX()
        {
            Type = DNSQueryType.MX;
            Class = DNSQueryClass.IN;
        }
        #endregion
    }
}