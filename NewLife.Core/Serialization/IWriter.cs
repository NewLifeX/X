using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Reflection;

namespace NewLife.Serialization
{
    /// <summary>
    /// 写入器接口
    /// </summary>
    [CLSCompliant(false)]
    public interface IWriter : IReaderWriter
    {
        #region 事件
        /// <summary>
        /// 写成员前触发。参数是成员信息和是否取消写入该成员。
        /// 事件处理器中可以自定义写入成员，然后把第二参数设为false请求写入器不要再写入该成员。
        /// </summary>
        event EventHandler<EventArgs<MemberInfo, Boolean>> OnMemberWriting;

        /// <summary>
        /// 写成员后触发。
        /// </summary>
        event EventHandler<EventArgs<MemberInfo, Boolean>> OnMemberWrited;
        #endregion

        #region 写入基础元数据
        #region 字节
        /// <summary>
        /// 将一个无符号字节写入
        /// </summary>
        /// <param name="value">要写入的无符号字节。</param>
        void Write(Byte value);

        /// <summary>
        /// 将字节数组写入
        /// </summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        void Write(byte[] buffer);

        /// <summary>
        /// 将一个有符号字节写入当前流，并将流的位置提升 1 个字节。
        /// </summary>
        /// <param name="value">要写入的有符号字节。</param>
        void Write(sbyte value);

        /// <summary>
        /// 将字节数组部分写入当前流。
        /// </summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        /// <param name="index">buffer 中开始写入的起始点。</param>
        /// <param name="count">要写入的字节数。</param>
        void Write(byte[] buffer, int index, int count);
        #endregion

        #region 有符号整数
        /// <summary>
        /// 将 2 字节有符号整数写入当前流，并将流的位置提升 2 个字节。
        /// </summary>
        /// <param name="value">要写入的 2 字节有符号整数。</param>
        void Write(short value);

        /// <summary>
        /// 将 4 字节有符号整数写入当前流，并将流的位置提升 4 个字节。
        /// </summary>
        /// <param name="value">要写入的 4 字节有符号整数。</param>
        void Write(int value);

        /// <summary>
        /// 将 8 字节有符号整数写入当前流，并将流的位置提升 8 个字节。
        /// </summary>
        /// <param name="value">要写入的 8 字节有符号整数。</param>
        void Write(long value);
        #endregion

        #region 无符号整数
        /// <summary>
        /// 将 2 字节无符号整数写入当前流，并将流的位置提升 2 个字节。
        /// </summary>
        /// <param name="value">要写入的 2 字节无符号整数。</param>
        void Write(ushort value);

        /// <summary>
        /// 将 4 字节无符号整数写入当前流，并将流的位置提升 4 个字节。
        /// </summary>
        /// <param name="value">要写入的 4 字节无符号整数。</param>
        void Write(uint value);

        /// <summary>
        /// 将 8 字节无符号整数写入当前流，并将流的位置提升 8 个字节。
        /// </summary>
        /// <param name="value">要写入的 8 字节无符号整数。</param>
        void Write(ulong value);
        #endregion

        #region 浮点数
        /// <summary>
        /// 将 4 字节浮点值写入当前流，并将流的位置提升 4 个字节。
        /// </summary>
        /// <param name="value">要写入的 4 字节浮点值。</param>
        void Write(float value);

        /// <summary>
        /// 将 8 字节浮点值写入当前流，并将流的位置提升 8 个字节。
        /// </summary>
        /// <param name="value">要写入的 8 字节浮点值。</param>
        void Write(double value);
        #endregion

        #region 字符串
        /// <summary>
        /// 将 Unicode 字符写入当前流，并根据所使用的 Encoding 和向流中写入的特定字符，提升流的当前位置。
        /// </summary>
        /// <param name="ch">要写入的非代理项 Unicode 字符。</param>
        void Write(char ch);

        /// <summary>
        /// 将字符数组写入当前流，并根据所使用的 Encoding 和向流中写入的特定字符，提升流的当前位置。
        /// </summary>
        /// <param name="chars">包含要写入的数据的字符数组。</param>
        void Write(char[] chars);

        /// <summary>
        /// 将字符数组部分写入当前流，并根据所使用的 Encoding（可能还根据向流中写入的特定字符），提升流的当前位置。
        /// </summary>
        /// <param name="chars">包含要写入的数据的字符数组。</param>
        /// <param name="index">chars 中开始写入的起始点。</param>
        /// <param name="count">要写入的字符数。</param>
        void Write(char[] chars, int index, int count);

        /// <summary>
        /// 写入字符串
        /// </summary>
        /// <param name="value">要写入的值。</param>
        void Write(string value);
        #endregion

        #region 其它
        /// <summary>
        /// 将单字节 Boolean 值写入
        /// </summary>
        /// <param name="value">要写入的 Boolean 值</param>
        void Write(Boolean value);

        /// <summary>
        /// 将一个十进制值写入当前流，并将流位置提升十六个字节。
        /// </summary>
        /// <param name="value">要写入的十进制值。</param>
        void Write(decimal value);
        #endregion
        #endregion

        #region 7位压缩编码整数
        ///// <summary>
        ///// 以7位压缩格式写入16位整数，小于7位用1个字节，小于14位用2个字节。
        ///// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        ///// </summary>
        ///// <param name="value">16位整数</param>
        ///// <returns>实际写入字节数</returns>
        //Int32 WriteEncoded(Int16 value);

        ///// <summary>
        ///// 以7位压缩格式写入32位整数，小于7位用1个字节，小于14位用2个字节。
        ///// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        ///// </summary>
        ///// <param name="value">32位整数</param>
        ///// <returns>实际写入字节数</returns>
        //Int32 WriteEncoded(Int32 value);

        ///// <summary>
        ///// 以7位压缩格式写入64位整数，小于7位用1个字节，小于14位用2个字节。
        ///// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        ///// </summary>
        ///// <param name="value">64位整数</param>
        ///// <returns>实际写入字节数</returns>
        //Int32 WriteEncoded(Int64 value);
        #endregion

        #region 写入对象
        /// <summary>
        /// 把对象写入数据流，空对象写入0，所有子孙成员编码整数、允许空、写入字段。
        /// </summary>
        /// <param name="value">对象</param>
        /// <returns>是否写入成功</returns>
        Boolean WriteObject(Object value);
        #endregion

        #region 枚举
        /// <summary>
        /// 写入枚举类型数据
        /// </summary>
        /// <param name="value">枚举数据</param>
        /// <returns>是否写入成功</returns>
        Boolean Write(IEnumerable value);
        #endregion
    }
}