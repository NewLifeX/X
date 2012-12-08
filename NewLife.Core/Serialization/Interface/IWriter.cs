using System;

namespace NewLife.Serialization
{
    /// <summary>写入器接口</summary>
    /// <remarks>
    /// 序列化框架的核心思想：基本类型直接写入，自定义类型反射得到成员，逐层递归写入！详见<see cref="IReaderWriter" />
    /// 
    /// 序列化对象时只能调用<see cref="WriteObject(Object)" />方法，其它所有方法（包括所有Write重载）仅用于内部写入或者自定义序列化时使用。
    /// </remarks>
    public interface IWriter : IReaderWriter
    {
        #region 方法
        /// <summary>主要入口方法。把对象写入数据流</summary>
        /// <param name="value">对象</param>
        /// <returns>是否写入成功</returns>
        Boolean WriteObject(Object value);

        /// <summary>主要入口方法。写入对象。具体读写器可以重载该方法以修改写入对象前后的行为。</summary>
        /// <param name="value">对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        Boolean WriteObject(Object value, Type type, WriteObjectCallback callback);

        /// <summary>写入对象成员</summary>
        /// <param name="name">成员名字</param>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的成员类型</param>
        /// <param name="index">成员索引</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        Boolean WriteMember(String name, Object value, Type type, Int32 index, WriteObjectCallback callback);

        /// <summary>刷新缓存中的数据</summary>
        void Flush();
        #endregion

        #region 事件
        /// <summary>写对象前触发。</summary>
        event EventHandler<WriteObjectEventArgs> OnObjectWriting;

        /// <summary>写对象后触发。</summary>
        event EventHandler<WriteObjectEventArgs> OnObjectWrited;

        /// <summary>写成员前触发。</summary>
        event EventHandler<WriteMemberEventArgs> OnMemberWriting;

        /// <summary>写成员后触发。</summary>
        event EventHandler<WriteMemberEventArgs> OnMemberWrited;
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