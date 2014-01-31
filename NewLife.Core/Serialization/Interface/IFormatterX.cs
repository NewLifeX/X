using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>序列化接口</summary>
    public interface IFormatterX
    {
        #region 方法
        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        Boolean Write(Object value, Type type = null);

        /// <summary>读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        Object Read(Type type);

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Boolean TryRead(Type type, ref Object value);
        #endregion
    }

    ///// <summary>序列化接口</summary>
    //public abstract class FormatterBase : IFormatterX
    //{

    //}
}