using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace NewLife.Serialization
{
    /// <summary>
    /// 读写器接口
    /// </summary>
    public interface IReaderWriter
    {
        #region 属性
        /// <summary>字符串编码。</summary>
        Encoding Encoding { get; set; }
        #endregion

        //#region 方法
        ///// <summary>
        ///// 获取指定类型中需要序列化的成员（属性或字段）
        ///// </summary>
        ///// <param name="type">指定类型</param>
        ///// <returns>需要序列化的成员</returns>
        //IObjectMemberInfo[] GetMembers(Type type);
        //#endregion

        //#region 事件
        ///// <summary>
        ///// 获取指定类型中需要序列化的成员时触发。使用者可以修改、排序要序列化的成员。
        ///// </summary>
        //event EventHandler<EventArgs<Type, IObjectMemberInfo[]>> OnGotMembers;
        //#endregion
    }
}