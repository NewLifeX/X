using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using NewLife.Reflection;

namespace NewLife.IO
{
    /// <summary>
    /// 二进制协议写入器
    /// </summary>
    public class BinaryWriterX : BinaryWriter
    {
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="stream"></param>
        public BinaryWriterX(Stream stream) : base(stream) { }

        /// <summary>
        /// 以压缩格式写入32位整数
        /// </summary>
        /// <param name="value"></param>
        public void WriteEncoded(Int32 value)
        {
            //Write7BitEncodedInt(value);

            uint num = (uint)value;
            while (num >= 0x80)
            {
                this.Write((byte)(num | 0x80));
                num = num >> 7;
            }
            this.Write((byte)num);
        }

        /// <summary>
        /// 以压缩格式写入64位整数
        /// </summary>
        /// <param name="value"></param>
        public void WriteEncoded(Int64 value)
        {
            UInt64 num = (UInt64)value;
            while (num >= 0x80)
            {
                this.Write((byte)(num | 0x80));
                num = num >> 7;
            }
            this.Write((byte)num);
        }

        #region 写入值类型
        /// <summary>
        /// 写入值类型，只能识别基础类型，对于不能识别的类型，方法返回false
        /// </summary>
        /// <param name="value"></param>
        /// <param name="encodeInt">是否编码整数</param>
        /// <returns>是否成功写入</returns>
        public Boolean WriteValue(Object value, Boolean encodeInt)
        {
            //if (value == null)
            //{
            //    Write((Byte)0);
            //    return true;
            //}

            // 值类型不会有空，写入器不知道该如何处理空，由外部决定吧
            if (value == null) return false;

            TypeCode code = Type.GetTypeCode(value.GetType());
            switch (code)
            {
                case TypeCode.Boolean:
                    Write(Convert.ToBoolean(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.Byte:
                    Write(Convert.ToByte(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.Char:
                    Write(Convert.ToChar(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.DBNull:
                    Write((Byte)0);
                    return true;
                case TypeCode.DateTime:
                    //Write(Convert.ToDateTime(value, CultureInfo.InvariantCulture).Ticks);
                    //return true;
                    return WriteValue(Convert.ToDateTime(value, CultureInfo.InvariantCulture).Ticks, encodeInt);
                //DateTime dt = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
                //return WriteValue((dt - new DateTime(2000, 1, 1)).Ticks, encodeInt);
                case TypeCode.Decimal:
                    Write(Convert.ToDecimal(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.Double:
                    Write(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.Empty:
                    Write((Byte)0);
                    return true;
                case TypeCode.Int16:
                    Write(Convert.ToInt16(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.Int32:
                    if (!encodeInt)
                        Write(Convert.ToInt32(value, CultureInfo.InvariantCulture));
                    else
                        WriteEncoded(Convert.ToInt32(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.Int64:
                    if (!encodeInt)
                        Write(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                    else
                        WriteEncoded(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.Object:
                    break;
                case TypeCode.SByte:
                    Write(Convert.ToSByte(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.Single:
                    Write(Convert.ToSingle(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.String:
                    Write(Convert.ToString(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.UInt16:
                    Write(Convert.ToUInt16(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.UInt32:
                    if (!encodeInt)
                        Write(Convert.ToUInt32(value, CultureInfo.InvariantCulture));
                    else
                        WriteEncoded(Convert.ToInt32(value, CultureInfo.InvariantCulture));
                    return true;
                case TypeCode.UInt64:
                    if (!encodeInt)
                        Write(Convert.ToUInt64(value, CultureInfo.InvariantCulture));
                    else
                        WriteEncoded(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                    return true;
                default:
                    break;
            }

            //Type type = value.GetType();
            //if (type == typeof(Guid))
            //{
            //    Write((Guid)value);
            //    return true;
            //}

            //if (type == typeof(IPAddress))
            //{
            //    Byte[] buffer = (value as IPAddress).GetAddressBytes();
            //    WriteEncoded(buffer.Length);
            //    Write(buffer);
            //    return true;
            //}
            if (WriteX(value)) return true;

            return false;
        }
        #endregion

        #region 扩展
        /// <summary>
        /// 扩展写入，反射查找合适的写入方法
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private Boolean WriteX(Object value)
        {
            Type type = value.GetType();

            MethodInfo method = this.GetType().GetMethod("Write", new Type[] { type });
            if (method == null) return false;

            MethodInfoX.Create(method).Invoke(this, new Object[] { value });
            return true;
        }

        /// <summary>
        /// 写入Guid
        /// </summary>
        /// <param name="value"></param>
        public void Write(Guid value)
        {
            Write(((Guid)value).ToByteArray());
        }

        /// <summary>
        /// 写入IPAddress
        /// </summary>
        /// <param name="value"></param>
        public void Write(IPAddress value)
        {
            Byte[] buffer = (value as IPAddress).GetAddressBytes();
            WriteEncoded(buffer.Length);
            Write(buffer);
        }

        /// <summary>
        /// 写入IPEndPoint
        /// </summary>
        /// <param name="value"></param>
        public void Write(IPEndPoint value)
        {
            Write(value.Address);
            //// 端口实际只占2字节
            //Write((UInt16)value.Port);
            WriteEncoded(value.Port);
        }
        #endregion
    }
}