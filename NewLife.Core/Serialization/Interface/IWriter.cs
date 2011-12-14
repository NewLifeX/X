using System;
using System.Collections;
using System.Net;

namespace NewLife.Serialization
{
    /// <summary>写入器接口</summary>
    /// <remarks>序列化框架的处理顺序为：IAccessor接口 => OnObjectWriting事件 => 扩展类型 => 基础类型 => 字典 => 枚举 => 序列化接口 => 自定义对象 => 未知类型 => OnObjectWrited事件</remarks>
    public interface IWriter : IReaderWriter
    {
        #region 基元类型
        #region 字节
        /// <summary>
        /// 将一个无符号字节写入
        /// </summary>
        /// <param name="value">要写入的无符号字节。</param>
        void Write(Byte value);

        /// <summary>
        /// 将字节数组写入，如果设置了UseSize，则先写入数组长度。
        /// </summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        void Write(byte[] buffer);

        /// <summary>
        /// 将一个有符号字节写入当前流，并将流的位置提升 1 个字节。
        /// </summary>
        /// <param name="value">要写入的有符号字节。</param>
        void Write(sbyte value);

        /// <summary>
        /// 将字节数组部分写入当前流，不写入数组长度。
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

        /// <summary>
        /// 将一个时间日期写入
        /// </summary>
        /// <param name="value"></param>
        void Write(DateTime value);
        #endregion
        #endregion

        #region 扩展类型
        /// <summary>
        /// 写入Guid
        /// </summary>
        /// <param name="value"></param>
        void Write(Guid value);

        /// <summary>
        /// 写入IPAddress
        /// </summary>
        /// <param name="value"></param>
        void Write(IPAddress value);

        /// <summary>
        /// 写入IPEndPoint
        /// </summary>
        /// <param name="value"></param>
        void Write(IPEndPoint value);

        /// <summary>
        /// 写入Type
        /// </summary>
        /// <param name="value"></param>
        void Write(Type value);
        #endregion

        #region 复杂对象
        /// <summary>把对象写入数据流</summary>
        /// <param name="value">对象</param>
        /// <returns>是否写入成功</returns>
        Boolean WriteObject(Object value);

        /// <summary>
        /// 写入对象。具体读写器可以重载该方法以修改写入对象前后的行为。
        /// </summary>
        /// <param name="value">对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        Boolean WriteObject(Object value, Type type, WriteObjectCallback callback);

        //Boolean WriteStart(Object value, Type type);

        //Boolean WriteBody();

        //Boolean WriteEnd(Object value, Type type);

        /// <summary>
        /// 写入对象引用。
        /// </summary>
        /// <param name="value">对象</param>
        /// <returns>是否写入成功</returns>
        Boolean WriteObjRef(Object value);
        #endregion

        #region 字典
        /// <summary>
        /// 写入字典类型数据
        /// </summary>
        /// <param name="value">字典数据</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        Boolean WriteDictionary(IDictionary value, Type type, WriteObjectCallback callback);
        #endregion

        #region 枚举
        /// <summary>
        /// 写入枚举类型数据
        /// </summary>
        /// <param name="value">枚举数据</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        Boolean WriteEnumerable(IEnumerable value, Type type, WriteObjectCallback callback);
        #endregion

        #region 序列化接口
        /// <summary>
        /// 写入实现了可序列化接口的对象
        /// </summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        Boolean WriteSerializable(Object value, Type type, WriteObjectCallback callback);
        #endregion

        #region 未知对象
        /// <summary>
        /// 写入未知对象（其它所有方法都无法识别的对象），采用BinaryFormatter或者XmlSerialization
        /// </summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        Boolean WriteUnKnown(Object value, Type type, WriteObjectCallback callback);
        #endregion

        #region 事件
        /// <summary>
        /// 写对象前触发。
        /// </summary>
        event EventHandler<WriteObjectEventArgs> OnObjectWriting;

        /// <summary>
        /// 写对象后触发。
        /// </summary>
        event EventHandler<WriteObjectEventArgs> OnObjectWrited;

        /// <summary>
        /// 写成员前触发。
        /// </summary>
        event EventHandler<WriteMemberEventArgs> OnMemberWriting;

        /// <summary>
        /// 写成员后触发。
        /// </summary>
        event EventHandler<WriteMemberEventArgs> OnMemberWrited;

        /// <summary>
        /// 写字典项前触发。
        /// </summary>
        event EventHandler<WriteDictionaryEventArgs> OnDictionaryWriting;

        /// <summary>
        /// 写字典项后触发。
        /// </summary>
        event EventHandler<WriteDictionaryEventArgs> OnDictionaryWrited;

        /// <summary>
        /// 写枚举项前触发。
        /// </summary>
        event EventHandler<WriteItemEventArgs> OnItemWriting;

        /// <summary>
        /// 写枚举项后触发。
        /// </summary>
        event EventHandler<WriteItemEventArgs> OnItemWrited;
        #endregion

        #region 方法
        /// <summary>写入大小</summary>
        /// <param name="size"></param>
        void WriteSize(Int32 size);

        /// <summary>
        /// 刷新缓存中的数据
        /// </summary>
        void Flush();

        /// <summary>
        /// 输出数据转为字节数组
        /// </summary>
        /// <returns></returns>
        Byte[] ToArray();
        #endregion
    }

    /// <summary>数据写入方法</summary>
    /// <param name="writer">写入器</param>
    /// <param name="value">要写入的对象</param>
    /// <param name="type">要写入的对象类型</param>
    /// <param name="callback">处理成员的方法</param>
    /// <returns>是否写入成功</returns>
    public delegate Boolean WriteObjectCallback(IWriter writer, Object value, Type type, WriteObjectCallback callback);
}