using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NewLife.Security
{
    /// <summary>抽象语法标记。ASN.1是一种 ISO/ITU-T 标准，描述了一种对数据进行表示、编码、传输和解码的数据格式。</summary>
    public class Asn1
    {
        #region 属性
        /// <summary>标签</summary>
        public Asn1Tags Tag { get; set; }

        /// <summary>长度</summary>
        public Int32 Length { get; set; }

        /// <summary>数值</summary>
        public Object Value { get; set; }
        #endregion

        #region 构造
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            switch (Tag)
            {
                case Asn1Tags.Boolean:
                    break;
                case Asn1Tags.Integer: return "#" + (Value as Byte[]).ToHex(0, 32);
                case Asn1Tags.BitString:
                case Asn1Tags.OctetString: return (Value as Byte[]).ToHex(0, 32);
                case Asn1Tags.Null: return "Null";
                case Asn1Tags.ObjectIdentifier:
                    if (Value is Oid oid) return oid.FriendlyName + " " + oid.Value;
                    break;
                case Asn1Tags.Sequence:
                    if (Value is Asn1[] arr) return arr.Join();
                    break;
            }

            return $"{Tag} {Value}";
        }
        #endregion

        #region 方法
        /// <summary>获取OID</summary>
        /// <returns></returns>
        public Oid[] GetOids()
        {
            if (Value is Oid oid) return new[] { oid };

            var list = new List<Oid>();
            if (Value is Asn1[] arr)
            {
                foreach (var item in arr)
                {
                    var ds = item.GetOids();
                    if (ds != null && ds.Length > 0) list.AddRange(ds);
                }
            }

            return list.ToArray();
        }
        #endregion

        #region 读取
        /// <summary>读取</summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Asn1 Read(Byte[] data) => Read(new BinaryReader(new MemoryStream(data)));

        /// <summary>读取</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static Asn1 Read(Stream stream) => Read(new BinaryReader(stream));

        /// <summary>读取对象</summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static Asn1 Read(BinaryReader reader)
        {
            var len = ReadTLV(reader, out var tag);
            if (len < 0) return null;

            var asn = new Asn1 { Length = len };

            var tagNo = tag & 0x1F;
            //if (tagNo == 0x1F) tagNo = reader.BaseStream.ReadEncodedInt();

            // isConstructed
            asn.Tag = (Asn1Tags)tagNo;
            if ((tag & (Byte)Asn1Tags.Constructed) != 0)
            {
                switch (asn.Tag)
                {
                    case Asn1Tags.OctetString:
                        break;
                    case Asn1Tags.External:
                        break;
                    case Asn1Tags.Sequence:
                        var reader2 = new BinaryReader(new MemoryStream(reader.ReadBytes(len)));
                        var list = new List<Asn1>();
                        while (true)
                        {
                            var obj = Read(reader2);
                            if (obj == null) break;

                            list.Add(obj);
                        }
                        asn.Value = list.ToArray();
                        return asn;
                    case Asn1Tags.Set:
                        break;
                }
            }

            // 基础类型
            var buf = reader.ReadBytes(len);
            asn.Value = buf;
            switch (asn.Tag)
            {
                case Asn1Tags.Boolean:
                    break;
                case Asn1Tags.Integer:
                    asn.Value = buf;
                    break;
                case Asn1Tags.BitString:
                    if (buf.Length > 0 && buf[0] == 0) buf = buf.ReadBytes(1);
                    asn.Value = buf;
                    break;
                case Asn1Tags.OctetString:
                    asn.Value = buf;
                    break;
                case Asn1Tags.Null:
                    break;
                case Asn1Tags.ObjectIdentifier:
                    //asn.Value = reader.ReadBytes(len);
                    asn.Value = new Oid(MakeOidStringFromBytes(buf));
                    break;
                case Asn1Tags.External:
                    break;
                case Asn1Tags.Enumerated:
                    break;
                //case Asn1Tags.Sequence:
                //    break;
                //case Asn1Tags.SequenceOf:
                //    break;
                case Asn1Tags.Set:
                    break;
                //case Asn1Tags.SetOf:
                //    break;
                case Asn1Tags.NumericString:
                    break;
                case Asn1Tags.PrintableString:
                    break;
                case Asn1Tags.T61String:
                    break;
                case Asn1Tags.VideotexString:
                    break;
                case Asn1Tags.IA5String:
                    break;
                case Asn1Tags.UtcTime:
                    break;
                case Asn1Tags.GeneralizedTime:
                    break;
                case Asn1Tags.GraphicString:
                    break;
                case Asn1Tags.VisibleString:
                    break;
                case Asn1Tags.GeneralString:
                    break;
                case Asn1Tags.UniversalString:
                    break;
                case Asn1Tags.BmpString:
                    break;
                case Asn1Tags.Utf8String:
                    break;
                case Asn1Tags.Constructed:
                    break;
                case Asn1Tags.Application:
                    break;
                case Asn1Tags.Tagged:
                    break;
                default:
                    break;
            }

            return asn;
        }

        /// <summary>获取字节数组</summary>
        /// <param name="trimZero"></param>
        /// <returns></returns>
        public Byte[] GetByteArray(Boolean trimZero = false)
        {
            var buf = Value as Byte[];
            if (buf != null && trimZero && buf[0] == 0) buf = buf.ReadBytes(1);

            return buf;
        }
        #endregion

        #region 辅助
        /// <summary>读取TLV，Tag+Length+Value</summary>
        /// <param name="reader">读取器</param>
        /// <param name="tag"></param>
        /// <returns>返回长度，数据流指针移到Value第一个字节</returns>
        private static Int32 ReadTLV(BinaryReader reader, out Byte tag)
        {
            tag = 0;

            var v = reader.BaseStream.ReadByte();
            if (v < 0) return v;

            tag = (Byte)v;

            var len = (Int32)reader.ReadByte();
            if (len == 0x81)
                len = reader.ReadByte();
            else if (len == 0x82)
                len = reader.ReadBytes(2).ToUInt16(0, false);
            else if (len == 0x84)
                len = (Int32)reader.ReadBytes(4).ToUInt32(0, false);

            return len;
        }

        /// <summary>读取TLV，Tag+Length+Value</summary>
        /// <param name="reader">读取器</param>
        /// <param name="trimZero">是否剔除头部的0x00</param>
        /// <returns></returns>
        private static Byte[] ReadTLV(BinaryReader reader, Boolean trimZero = true)
        {
            var len = ReadTLV(reader, out var tag);
            //Debug.Assert(tag == 0x02);

            //if (offset > 0) reader.BaseStream.Seek(1, SeekOrigin.Current);
            if (trimZero && reader.PeekChar() == 0) { reader.ReadByte(); len--; }

            return reader.ReadBytes(len);
        }

        private const Int64 LONG_LIMIT = (Int64.MaxValue >> 7) - 0x7f;
        private static String MakeOidStringFromBytes(Byte[] bytes)
        {
            var objId = new StringBuilder();
            Int64 value = 0;
            //BigInteger bigValue;
            var first = true;

            for (var i = 0; i != bytes.Length; i++)
            {
                Int32 b = bytes[i];

                if (value <= LONG_LIMIT)
                {
                    value += (b & 0x7f);
                    if ((b & 0x80) == 0)             // end of number reached
                    {
                        if (first)
                        {
                            if (value < 40)
                            {
                                objId.Append('0');
                            }
                            else if (value < 80)
                            {
                                objId.Append('1');
                                value -= 40;
                            }
                            else
                            {
                                objId.Append('2');
                                value -= 80;
                            }
                            first = false;
                        }

                        objId.Append('.');
                        objId.Append(value);
                        value = 0;
                    }
                    else
                    {
                        value <<= 7;
                    }
                }
                //else
                //{
                //    if (bigValue == null)
                //    {
                //        bigValue = BigInteger.ValueOf(value);
                //    }
                //    bigValue = bigValue.Or(BigInteger.ValueOf(b & 0x7f));
                //    if ((b & 0x80) == 0)
                //    {
                //        if (first)
                //        {
                //            objId.Append('2');
                //            bigValue = bigValue.Subtract(BigInteger.ValueOf(80));
                //            first = false;
                //        }

                //        objId.Append('.');
                //        objId.Append(bigValue);
                //        bigValue = null;
                //        value = 0;
                //    }
                //    else
                //    {
                //        bigValue = bigValue.ShiftLeft(7);
                //    }
                //}
            }

            return objId.ToString();
        }
        #endregion
    }
}