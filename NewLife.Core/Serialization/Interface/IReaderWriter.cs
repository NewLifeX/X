using System;
using System.IO;
using System.Collections;

namespace NewLife.Serialization
{
    /// <summary>读写器接口</summary>
    /// <remarks>
    /// 序列化框架的核心思想：基本类型直接写入，自定义类型反射得到成员，逐层递归写入！
    /// 
    /// <see cref="Stream"/>作为序列化操作的根本，读写都是对数据流进行操作；
    /// <see cref="Settings"/>是序列化时的一些设置；
    /// <see cref="Depth"/>表示当前序列化的层次；
    /// <see cref="GetMembers"/>方法和<see cref="OnGotMembers"/>事件用于获取/修改自定义类型需要序列化的成员，默认反射实现。
    /// </remarks>
    public interface IReaderWriter
    {
        #region 属性
        /// <summary>读写器名称</summary>
        String Name { get; }

        /// <summary>数据流</summary>
        Stream Stream { get; set; }

        /// <summary>序列化设置</summary>
        ReaderWriterSetting Settings { get; set; }

        /// <summary>层次深度。</summary>
        Int32 Depth { get; set; }

        /// <summary>当前对象</summary>
        Object CurrentObject { get; set; }

        /// <summary>当前成员</summary>
        IObjectMemberInfo CurrentMember { get; set; }

        /// <summary>用于存放使用者的上下文数据</summary>
        IDictionary Items { get; set; }
        #endregion

        #region 方法
        /// <summary>重置</summary>
        void Reset();

        /// <summary>获取需要序列化的成员</summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <returns>需要序列化的成员</returns>
        IObjectMemberInfo[] GetMembers(Type type, Object value);
        #endregion

        #region 事件
        /// <summary>获取指定类型中需要序列化的成员时触发。使用者可以修改、排序要序列化的成员。</summary>
        event EventHandler<EventArgs<Type, Object, IObjectMemberInfo[]>> OnGotMembers;
        #endregion

        #region 跟踪日志
        /// <summary>是否调试</summary>
        Boolean Debug { get; set; }

        /// <summary>使用跟踪流。实际上是重新包装一次Stream，必须在设置Stream，使用之前</summary>
        void EnableTraceStream();

        /// <summary>调试输出</summary>
        /// <param name="action">操作</param>
        /// <param name="args">参数</param>
        void WriteLog(String action, params Object[] args);
        #endregion
    }
}