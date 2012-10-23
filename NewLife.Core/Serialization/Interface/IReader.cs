using System;
using System.Net;

namespace NewLife.Serialization
{
    /// <summary>读取器接口</summary>
    /// <remarks>
    /// 序列化框架的核心思想：基本类型直接读取，自定义类型反射得到成员，逐层递归读取！详见<see cref="IReaderWriter"/>
    /// 
    /// 反序列化对象时只能调用<see cref="ReadObject(Type)" />方法，其它所有方法（包括所有Read重载）仅用于内部读取或者自定义序列化时使用。
    /// </remarks>
    public interface IReader : IReaderWriter
    {
        #region 方法
        /// <summary>主要入口方法。从数据流中读取指定类型的对象</summary>
        /// <param name="type">类型</param>
        /// <returns>对象</returns>
        Object ReadObject(Type type);

        /// <summary>主要入口方法。从数据流中读取指定类型的对象</summary>
        /// <typeparam name="T">类型</typeparam>
        /// <returns>对象</returns>
        T ReadObject<T>();

        /// <summary>主要入口方法。尝试按照指定类型读取目标对象</summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        Boolean ReadObject(Type type, ref Object value, ReadObjectCallback callback);

        /// <summary>读取对象成员</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="member">成员</param>
        /// <param name="index">成员索引</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        Boolean ReadMember(Type type, ref Object value, IObjectMemberInfo member, Int32 index, ReadObjectCallback callback);
        #endregion

        #region 事件
        /// <summary>读对象前触发。</summary>
        event EventHandler<ReadObjectEventArgs> OnObjectReading;

        /// <summary>读对象后触发。</summary>
        event EventHandler<ReadObjectEventArgs> OnObjectReaded;

        /// <summary>读成员前触发。</summary>
        event EventHandler<ReadMemberEventArgs> OnMemberReading;

        /// <summary>读成员后触发。</summary>
        event EventHandler<ReadMemberEventArgs> OnMemberReaded;
        #endregion
    }

    /// <summary>数据读取方法</summary>
    /// <param name="reader">读取器</param>
    /// <param name="type">要读取的对象类型</param>
    /// <param name="value">要读取的对象</param>
    /// <param name="callback">处理成员的方法</param>
    /// <returns>是否读取成功</returns>
    public delegate Boolean ReadObjectCallback(IReader reader, Type type, ref Object value, ReadObjectCallback callback);
}