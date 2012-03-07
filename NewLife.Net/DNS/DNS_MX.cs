using System;
using System.Net;
using NewLife.Serialization;
using System.IO;

namespace NewLife.Net.DNS
{
    /// <summary>MX记录</summary>
    public class DNS_MX : DNSRecord
    {
        #region 属性
        private Int16 _Preference;
        /// <summary>引用</summary>
        public Int16 Preference { get { return _Preference; } set { _Preference = value; } }

        private String _Host;
        /// <summary>主机</summary>
        public String Host { get { return _Host; } set { _Host = value; } }
        #endregion

        #region 构造
        /// <summary>构造一个MX记录实例</summary>
        public DNS_MX()
        {
            Type = DNSQueryType.MX;
            Class = DNSQueryClass.IN;
        }
        #endregion

        #region 方法
        //internal override void OnReadDataString(IReader reader, Int64 position)
        //{
        //    //base.OnReadDataString(reader);

        //    if (Data == null || Data.Length < 2) return;

        //    var data = new Byte[2];
        //    Array.Copy(Data, 0, data, 0, data.Length);
        //    Array.Reverse(data);
        //    Preference = BitConverter.ToInt16(data, 0);

        //    // 当前指针在数据流后面
        //    DataString = GetNameAccessor(reader).Read(new MemoryStream(Data, 2, Data.Length - 2), position);
        //}

        //internal override void OnWriteDataString(IWriter writer, Stream ms)
        //{
        //    //base.OnWriteDataString(writer, ms);

        //    var data = BitConverter.GetBytes(Preference);
        //    Array.Reverse(data);
        //    ms.WriteByte(data[0]);
        //    ms.WriteByte(data[1]);

        //    // 传入当前流偏移，加2是因为待会要先写2个字节的长度
        //    GetNameAccessor(writer).Write(ms, DataString, writer.Stream.Position + 2 + 2);
        //}
        #endregion

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return base.ToString() + " " + Preference + " " + Host;
        }
    }
}