using System;
using System.IO;
using System.Reflection;
using System.Net;
using NewLife.Reflection;

namespace NewLife.IO
{
    /// <summary>
    /// 二进制协议读取器
    /// </summary>
    public class BinaryReaderX : BinaryReader
    {
        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="stream"></param>
        public BinaryReaderX(Stream stream) : base(stream) { }

        /// <summary>
        /// 以压缩格式读取32位整数
        /// </summary>
        /// <returns></returns>
        public Int32 ReadEncodedInt32()
        {
            return Read7BitEncodedInt();

            //byte num3;
            //int num = 0;
            //int num2 = 0;
            //do
            //{
            //    if (num2 == 0x23)
            //    {
            //        //throw new FormatException(Environment.GetResourceString("Format_Bad7BitInt32"));
            //        throw new FormatException("Format_Bad7BitInt32");
            //    }
            //    num3 = this.ReadByte();
            //    num |= (num3 & 0x7f) << num2;
            //    num2 += 7;
            //}
            //while ((num3 & 0x80) != 0);
            //return num;
        }

        /// <summary>
        /// 以压缩格式读取64位整数
        /// </summary>
        /// <returns></returns>
        public Int64 ReadEncodedInt64()
        {
            Byte b;
            Int64 rs = 0;
            Int32 n = 0;
            while (true)
            {
                b = ReadByte();
                // 必须转为Int64，否则可能溢出
                rs += (Int64)(b & 0x7f) << n;
                if ((b & 0x80) == 0) break;

                n += 7;
                if (n >= 64) throw new FormatException("数字值过大，无法使用压缩格式读取！");
            }
            return rs;
        }

        #region 读取值类型
        /// <summary>
        /// 读取值类型数据
        /// </summary>
        /// <param name="type"></param>
        /// <param name="encodeInt"></param>
        /// <returns></returns>
        public Object ReadValue(Type type, Boolean encodeInt)
        {
            Object value;
            return TryReadValue(type, encodeInt, out value) ? value : null;
        }

        /// <summary>
        /// 尝试读取值类型数据，返回是否读取成功
        /// </summary>
        /// <param name="type"></param>
        /// <param name="encodeInt"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean TryReadValue(Type type, Boolean encodeInt, out Object value)
        {
            TypeCode code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Boolean:
                    value = ReadBoolean();
                    return true;
                case TypeCode.Byte:
                    value = ReadByte();
                    return true;
                case TypeCode.Char:
                    value = ReadChar();
                    return true;
                case TypeCode.DBNull:
                    //value=DBNull.Value;
                    value = ReadByte();
                    return true;
                case TypeCode.DateTime:
                    //value=new DateTime(ReadInt64());
                    if (!TryReadValue(typeof(Int64), encodeInt, out value)) return false;
                    value = new DateTime((Int64)value);
                    //value = new DateTime(2000, 1, 1).AddTicks((Int64)value);
                    return true;
                case TypeCode.Decimal:
                    value = ReadDecimal();
                    return true;
                case TypeCode.Double:
                    value = ReadDouble();
                    return true;
                case TypeCode.Empty:
                    //value=null; ;
                    value = ReadByte();
                    return true;
                case TypeCode.Int16:
                    value = ReadInt16();
                    return true;
                case TypeCode.Int32:
                    if (!encodeInt)
                        value = ReadInt32();
                    else
                        value = ReadEncodedInt32();
                    return true;
                case TypeCode.Int64:
                    if (!encodeInt)
                        value = ReadInt64();
                    else
                        value = ReadEncodedInt64();
                    return true;
                case TypeCode.Object:
                    break;
                case TypeCode.SByte:
                    value = ReadSByte();
                    return true;
                case TypeCode.Single:
                    value = ReadSingle();
                    return true;
                case TypeCode.String:
                    value = ReadString();
                    return true;
                case TypeCode.UInt16:
                    value = ReadUInt16();
                    return true;
                case TypeCode.UInt32:
                    if (!encodeInt)
                        value = ReadUInt32();
                    else
                        value = ReadEncodedInt32();
                    return true;
                case TypeCode.UInt64:
                    if (!encodeInt)
                        value = ReadUInt64();
                    else
                        value = ReadEncodedInt64();
                    return true;
                default:
                    break;
            }

            //if (type == typeof(Guid))
            //{
            //    value = new Guid(ReadBytes(16));
            //    return true;
            //}

            if (ReadX(type, out value)) return true;

            value = null;
            return false;
        }
        #endregion

        #region 扩展
        /// <summary>
        /// 扩展读取，反射查找合适的读取方法
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private Boolean ReadX(Type type, out Object value)
        {
            value = null;
            MethodInfo method = this.GetType().GetMethod("Read" + type.Name, new Type[0]);
            if (method == null) return false;

            value = MethodInfoX.Create(method).Invoke(this, new Object[0]);
            return true;
        }

        /// <summary>
        /// 读取Guid
        /// </summary>
        /// <returns></returns>
        public Guid ReadGuid()
        {
            return new Guid(ReadBytes(16));
        }

        /// <summary>
        /// 读取IPAddress
        /// </summary>
        /// <returns></returns>
        public IPAddress ReadIPAddress()
        {
            Int32 p = 0;
            p = ReadEncodedInt32();
            Byte[] buffer = ReadBytes(p);

            return new IPAddress(buffer);
        }

        /// <summary>
        /// 读取IPEndPoint
        /// </summary>
        /// <returns></returns>
        public IPEndPoint ReadIPEndPoint()
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            ep.Address = ReadIPAddress();
            //// 端口实际只占2字节
            //ep.Port = ReadUInt16();
            ep.Port = ReadEncodedInt32();
            return ep;
        }
        #endregion
    }
}