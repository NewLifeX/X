using System;
using NewLife.Data;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Caching
{
    ///// <summary>Redis编码器</summary>
    //public interface IRedisEncoder
    //{
    //    /// <summary>数值转字节数组</summary>
    //    /// <param name="value"></param>
    //    /// <returns></returns>
    //    Packet Encode(Object value);

    //    /// <summary>字节数组转对象</summary>
    //    /// <param name="pk"></param>
    //    /// <param name="type"></param>
    //    /// <returns></returns>
    //    Object Decode(Packet pk, Type type);
    //}

    /// <summary>Redis编码器</summary>
    public class RedisJsonEncoder : IPacketEncoder
    {
        #region 属性
        /// <summary>解码出错时抛出异常。默认false不抛出异常，仅返回默认值</summary>
        public Boolean ThrowOnError { get; set; }
        #endregion

        /// <summary>数值转数据包</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual Packet Encode(Object value)
        {
            if (value == null) return Array.Empty<Byte>();

            if (value is Packet pk) return pk;
            if (value is Byte[] buf) return buf;
            if (value is IAccessor acc) return acc.ToPacket();

            var type = value.GetType();
            return (type.GetTypeCode()) switch
            {
                TypeCode.Object => value.ToJson().GetBytes(),
                TypeCode.String => (value as String).GetBytes(),
                TypeCode.DateTime => ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff").GetBytes(),
                _ => (value + "").GetBytes(),
            };
        }

        /// <summary>数据包转对象</summary>
        /// <param name="pk"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public virtual Object Decode(Packet pk, Type type)
        {
            //if (pk == null) return null;

            try
            {
                if (type == typeof(Packet)) return pk;
                if (type == typeof(Byte[])) return pk.ReadBytes();
                if (type.As<IAccessor>()) return type.AccessorRead(pk);

                //var str = pk.ToStr().Trim('\"');
                var str = pk.ToStr();
                if (type.GetTypeCode() == TypeCode.String) return str;
                //if (type.GetTypeCode() != TypeCode.Object) return str.ChangeType(type);
                if (type.GetTypeCode() != TypeCode.Object)
                {
                    if (type == typeof(Boolean) && str == "OK") return true;

                    return Convert.ChangeType(str, type);
                }

                return str.ToJsonEntity(type);
            }
            catch
            {
                if (ThrowOnError) throw;

                return null;
            }
        }
    }
}