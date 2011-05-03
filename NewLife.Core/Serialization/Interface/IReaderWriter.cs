using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;

namespace NewLife.Serialization
{
    /// <summary>
    /// 读写器接口
    /// </summary>
    public interface IReaderWriter
    {
        #region 属性
        /// <summary>
        /// 数据流
        /// </summary>
        Stream Stream { get; set; }

        /// <summary>
        /// 序列化设置
        /// </summary>
        ReaderWriterSetting Settings { get; set; }

        /// <summary>层次深度。</summary>
        Int32 Depth { get; set; }
        #endregion

        #region 方法
        /// <summary>
        /// 重置
        /// </summary>
        void Reset();

        /// <summary>
        /// 获取需要序列化的成员
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <returns>需要序列化的成员</returns>
        IObjectMemberInfo[] GetMembers(Type type, Object value);
        #endregion

        #region 事件
        /// <summary>
        /// 获取指定类型中需要序列化的成员时触发。使用者可以修改、排序要序列化的成员。
        /// </summary>
        event EventHandler<EventArgs<Type, Object, IObjectMemberInfo[]>> OnGotMembers;
        #endregion
    }
}