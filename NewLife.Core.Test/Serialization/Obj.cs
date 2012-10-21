using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using NewLife.Serialization;

namespace NewLife.Core.Test.Serialization
{
    abstract class Obj
    {
        #region 随机数
        [NonSerialized]
        private Random _Rnd;
        /// <summary>属性说明</summary>
        [XmlIgnore]
        public Random Rnd { get { return _Rnd ?? (_Rnd = new Random((Int32)DateTime.Now.Ticks)); } }
        #endregion

        #region 二进制输出
        public Stream GetStream(BinarySettings set)
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            Write(writer, set);
            ms.Position = 0;
            return ms;
        }

        public abstract void Write(BinaryWriter writer, BinarySettings set);

        //private BinaryWriter _Writer;
        ///// <summary>属性说明</summary>
        //public BinaryWriter Writer { get { return _Writer; } set { _Writer = value; } }

        //protected void Write(Int64 value)
        //{
        //    if (EncodeInt)
        //        WriteEncoded(value);
        //    else
        //        Writer.Write(value);
        //}
        #endregion

        #region 7位压缩编码整数
        //private Boolean _EncodeInt;
        ///// <summary>是否编码整数</summary>
        //public Boolean EncodeInt { get { return _EncodeInt; } set { _EncodeInt = value; } }

        public static Byte[] WriteEncoded(Int16 value) { return WriteEncoded((UInt16)value); }
        public static Byte[] WriteEncoded(UInt16 value) { return WriteEncoded((UInt64)value); }
        public static Byte[] WriteEncoded(Int32 value) { return WriteEncoded((UInt32)value); }
        public static Byte[] WriteEncoded(UInt32 value) { return WriteEncoded((UInt64)value); }
        public static Byte[] WriteEncoded(Int64 value) { return WriteEncoded((UInt64)value); }

        /// <summary>
        /// 以7位压缩格式写入64位整数，小于7位用1个字节，小于14位用2个字节。
        /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        /// </summary>
        /// <param name="value"></param>
        /// <returns>实际写入字节数</returns>
        public static Byte[] WriteEncoded(UInt64 value)
        {
            List<Byte> list = new List<Byte>();

            Int32 count = 1;
            UInt64 num = (UInt64)value;
            while (num >= 0x80)
            {
                list.Add((byte)(num | 0x80));
                num = num >> 7;

                count++;
            }
            list.Add((byte)num);

            return list.ToArray();
        }

        /// <summary>获取整数编码后所占字节数</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Int32 GetEncodedIntSize(Int64 value)
        {
            Int32 count = 1;
            UInt64 num = (UInt64)value;
            while (num >= 0x80)
            {
                num = num >> 7;

                count++;
            }

            return count;
        }
        #endregion
    }
}
