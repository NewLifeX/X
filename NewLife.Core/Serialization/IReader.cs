using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace NewLife.Serialization
{
    /// <summary>
    /// 读取器接口
    /// </summary>
    [CLSCompliant(false)]
    public interface IReader : IReaderWriter
    {
        #region 事件
        /// <summary>
        /// 读成员前触发。参数是成员信息和是否取消读取该成员。
        /// 事件处理器中可以自定义读取成员，然后把第二参数设为false请求读取器不要再读取该成员。
        /// </summary>
        event EventHandler<EventArgs<MemberInfo, Boolean>> OnMemberReading;

        /// <summary>
        /// 读成员后触发。
        /// </summary>
        event EventHandler<EventArgs<MemberInfo>> OnMemberReaded;
        #endregion

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

        /// <summary>
        /// 从此流中读取一个有符号字节，并使流的当前位置提升 1 个字节。
        /// </summary>
        /// <returns></returns>
        sbyte ReadSByte();
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
        /// <summary>
        /// 使用 Little-Endian 编码从当前流中读取 2 字节无符号整数，并将流的位置提升 2 个字节。
        /// </summary>
        /// <returns></returns>
        ushort ReadUInt16();

        /// <summary>
        /// 从当前流中读取 4 字节无符号整数并使流的当前位置提升 4 个字节。
        /// </summary>
        /// <returns></returns>
        uint ReadUInt32();

        /// <summary>
        /// 从当前流中读取 8 字节无符号整数并使流的当前位置提升 8 个字节。
        /// </summary>
        /// <returns></returns>
        ulong ReadUInt64();
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
        #endregion
        #endregion

        #region 7位压缩编码整数
        ///// <summary>
        ///// 以压缩格式读取16位整数
        ///// </summary>
        ///// <returns></returns>
        //Int16 ReadEncodedInt16();

        ///// <summary>
        ///// 以压缩格式读取32位整数
        ///// </summary>
        ///// <returns></returns>
        //Int32 ReadEncodedInt32();

        ///// <summary>
        ///// 以压缩格式读取64位整数
        ///// </summary>
        ///// <returns></returns>
        //Int64 ReadEncodedInt64();
        #endregion

        #region 读取对象
        /// <summary>
        /// 从数据流中读取指定类型的对象
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns>对象</returns>
        Object ReadObject(Type type);

        /// <summary>
        /// 尝试读取目标对象指定成员的值
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <returns>是否读取成功</returns>
        Boolean TryReadObject(Type type, ref Object value);

        ///// <summary>
        ///// 尝试读取目标对象指定成员的值，通过委托方法递归处理成员
        ///// </summary>
        ///// <param name="target">目标对象</param>
        ///// <param name="member">成员</param>
        ///// <param name="type">成员类型，以哪一种类型读取</param>
        ///// <param name="encodeInt">是否编码整数</param>
        ///// <param name="allowNull">是否允许空</param>
        ///// <param name="isProperty">是否处理属性</param>
        ///// <param name="value">成员值</param>
        ///// <returns>是否读取成功</returns>
        //Boolean TryReadObject(Object target, MemberInfoX member, Type type, Boolean encodeInt, Boolean allowNull, Boolean isProperty, out Object value);

        ///// <summary>
        ///// 尝试读取目标对象指定成员的值，处理基础类型、特殊类型、基础类型数组、特殊类型数组，通过委托方法处理成员
        ///// </summary>
        ///// <remarks>
        ///// 简单类型在value中返回，复杂类型直接填充target；
        ///// </remarks>
        ///// <param name="target">目标对象</param>
        ///// <param name="member">成员</param>
        ///// <param name="type">成员类型，以哪一种类型读取</param>
        ///// <param name="encodeInt">是否编码整数</param>
        ///// <param name="allowNull">是否允许空</param>
        ///// <param name="isProperty">是否处理属性</param>
        ///// <param name="value">成员值</param>
        ///// <param name="callback">处理成员的方法</param>
        ///// <returns>是否读取成功</returns>
        //Boolean TryReadObject(Object target, MemberInfoX member, Type type, Boolean encodeInt, Boolean allowNull, Boolean isProperty, out Object value, ReadCallback callback);
        #endregion

        #region 枚举
        /// <summary>
        /// 尝试读取目标对象指定成员的值
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <returns>是否读取成功</returns>
        Boolean TryReadEnumerable(Type type, ref Object value);
        #endregion
    }
}