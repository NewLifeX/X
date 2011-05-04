using System;
using System.Net;

namespace NewLife.Serialization
{
    /// <summary>
    /// 读取器接口
    /// </summary>
    public interface IReader : IReaderWriter
    {
        #region 读取基础元数据
        #region 字节
        /// <summary>
        /// 从当前流中读取下一个字节，并使流的当前位置提升 1 个字节。
        /// </summary>
        /// <returns></returns>
        byte ReadByte();

        /// <summary>
        /// 从当前流中将 count 个字节读入字节数组，并使当前位置提升 count 个字节。
        /// </summary>
        /// <param name="count">要读取的字节数。</param>
        /// <returns></returns>
        byte[] ReadBytes(int count);

        ///// <summary>
        ///// 从此流中读取一个有符号字节，并使流的当前位置提升 1 个字节。
        ///// </summary>
        ///// <returns></returns>
        //sbyte ReadSByte();
        #endregion

        #region 有符号整数
        /// <summary>
        /// 从当前流中读取 2 字节有符号整数，并使流的当前位置提升 2 个字节。
        /// </summary>
        /// <returns></returns>
        short ReadInt16();

        /// <summary>
        /// 从当前流中读取 4 字节有符号整数，并使流的当前位置提升 4 个字节。
        /// </summary>
        /// <returns></returns>
        int ReadInt32();

        /// <summary>
        /// 从当前流中读取 8 字节有符号整数，并使流的当前位置向前移动 8 个字节。
        /// </summary>
        /// <returns></returns>
        long ReadInt64();
        #endregion

        #region 无符号整数
        ///// <summary>
        ///// 使用 Little-Endian 编码从当前流中读取 2 字节无符号整数，并将流的位置提升 2 个字节。
        ///// </summary>
        ///// <returns></returns>
        //ushort ReadUInt16();

        ///// <summary>
        ///// 从当前流中读取 4 字节无符号整数并使流的当前位置提升 4 个字节。
        ///// </summary>
        ///// <returns></returns>
        //uint ReadUInt32();

        ///// <summary>
        ///// 从当前流中读取 8 字节无符号整数并使流的当前位置提升 8 个字节。
        ///// </summary>
        ///// <returns></returns>
        //ulong ReadUInt64();
        #endregion

        #region 浮点数
        /// <summary>
        /// 从当前流中读取 4 字节浮点值，并使流的当前位置提升 4 个字节。
        /// </summary>
        /// <returns></returns>
        float ReadSingle();

        /// <summary>
        /// 从当前流中读取 8 字节浮点值，并使流的当前位置提升 8 个字节。
        /// </summary>
        /// <returns></returns>
        double ReadDouble();
        #endregion

        #region 字符串
        /// <summary>
        /// 从当前流中读取下一个字符，并根据所使用的 Encoding 和从流中读取的特定字符，提升流的当前位置。
        /// </summary>
        /// <returns></returns>
        char ReadChar();

        /// <summary>
        /// 从当前流中读取 count 个字符，以字符数组的形式返回数据，并根据所使用的 Encoding 和从流中读取的特定字符，提升当前位置。
        /// </summary>
        /// <param name="count">要读取的字符数。</param>
        /// <returns></returns>
        char[] ReadChars(int count);

        /// <summary>
        /// 从当前流中读取一个字符串。字符串有长度前缀，一次 7 位地被编码为整数。
        /// </summary>
        /// <returns></returns>
        string ReadString();
        #endregion

        #region 其它
        /// <summary>
        /// 从当前流中读取 Boolean 值，并使该流的当前位置提升 1 个字节。
        /// </summary>
        /// <returns></returns>
        bool ReadBoolean();

        /// <summary>
        /// 从当前流中读取十进制数值，并将该流的当前位置提升十六个字节。
        /// </summary>
        /// <returns></returns>
        decimal ReadDecimal();

        /// <summary>
        /// 读取一个时间日期
        /// </summary>
        /// <returns></returns>
        DateTime ReadDateTime();
        #endregion
        #endregion

        #region 扩展类型
        /// <summary>
        /// 读取Guid
        /// </summary>
        /// <returns></returns>
        Guid ReadGuid();

        /// <summary>
        /// 读取IPAddress
        /// </summary>
        /// <returns></returns>
        IPAddress ReadIPAddress();

        /// <summary>
        /// 读取IPEndPoint
        /// </summary>
        /// <returns></returns>
        IPEndPoint ReadIPEndPoint();

        /// <summary>
        /// 读取Type
        /// </summary>
        /// <returns></returns>
        Type ReadType();
        #endregion

        #region 读取对象
        /// <summary>
        /// 尝试按照指定类型读取目标对象
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        Boolean ReadObject(Type type, ref Object value, ReadObjectCallback callback);

        /// <summary>
        /// 读取对象引用。
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <param name="index">引用计数</param>
        /// <returns>是否读取成功</returns>
        Boolean ReadObjRef(Type type, ref Object value, out Int32 index);
        #endregion

        #region 字典
        /// <summary>
        /// 尝试读取字典类型对象
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        Boolean ReadDictionary(Type type, ref Object value, ReadObjectCallback callback);
        #endregion

        #region 枚举
        /// <summary>
        /// 尝试读取枚举类型对象
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        Boolean ReadEnumerable(Type type, ref Object value, ReadObjectCallback callback);
        #endregion

        #region 序列化接口
        /// <summary>
        /// 读取实现了可序列化接口的对象
        /// </summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        Boolean ReadSerializable(Type type, ref Object value, ReadObjectCallback callback);
        #endregion

        #region 未知对象
        /// <summary>
        /// 读取未知对象（其它所有方法都无法识别的对象），采用BinaryFormatter或者XmlSerialization
        /// </summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        Boolean ReadUnKnown(Type type, ref Object value, ReadObjectCallback callback);
        #endregion

        #region 事件
        /// <summary>
        /// 读对象前触发。
        /// </summary>
        event EventHandler<ReadObjectEventArgs> OnObjectReading;

        /// <summary>
        /// 读对象后触发。
        /// </summary>
        event EventHandler<ReadObjectEventArgs> OnObjectReaded;

        /// <summary>
        /// 读成员前触发。
        /// </summary>
        event EventHandler<ReadMemberEventArgs> OnMemberReading;

        /// <summary>
        /// 读成员后触发。
        /// </summary>
        event EventHandler<ReadMemberEventArgs> OnMemberReaded;

        /// <summary>
        /// 读字典项前触发。
        /// </summary>
        event EventHandler<ReadDictionaryEventArgs> OnDictionaryReading;

        /// <summary>
        /// 读字典项后触发。
        /// </summary>
        event EventHandler<ReadDictionaryEventArgs> OnDictionaryReaded;

        /// <summary>
        /// 读枚举项前触发。
        /// </summary>
        event EventHandler<ReadItemEventArgs> OnItemReading;

        /// <summary>
        /// 读枚举项后触发。
        /// </summary>
        event EventHandler<ReadItemEventArgs> OnItemReaded;
        #endregion
    }

    /// <summary>
    /// 数据读取方法
    /// </summary>
    /// <param name="reader">读取器</param>
    /// <param name="type">要读取的对象类型</param>
    /// <param name="value">要读取的对象</param>
    /// <param name="callback">处理成员的方法</param>
    /// <returns>是否读取成功</returns>
    public delegate Boolean ReadObjectCallback(IReader reader, Type type, ref Object value, ReadObjectCallback callback);
}