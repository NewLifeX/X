using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace XCode
{
    /// <summary>
    /// 模型检查模式
    /// </summary>
    public enum ModelCheckModes
    {
        /// <summary>
        /// 初始化时检查所有表
        /// </summary>
        CheckAllTablesWhenInit,

        /// <summary>
        /// 第一次使用时检查表
        /// </summary>
        CheckTableWhenFirstUse
    }

    /// <summary>
    /// 模型检查模式
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ModelCheckModeAttribute : Attribute
    {
        private ModelCheckModes _Mode;
        /// <summary>模式</summary>
        public ModelCheckModes Mode
        {
            get { return _Mode; }
            set { _Mode = value; }
        }

        /// <summary>
        /// 指定实体类的模型检查模式
        /// </summary>
        /// <param name="mode"></param>
        public ModelCheckModeAttribute(ModelCheckModes mode) { Mode = mode; }

        /// <summary>
        /// 检索应用于类型成员的自定义属性。
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static ModelCheckModeAttribute GetCustomAttribute(MemberInfo element)
        {
            return GetCustomAttribute(element, typeof(ModelCheckModeAttribute)) as ModelCheckModeAttribute;
        }
    }
}