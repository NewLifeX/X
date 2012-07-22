using System;

namespace XCode
{
    /// <summary>模型字段排序模式</summary>
    public enum ModelSortModes
    {
        /// <summary>基类优先。默认值。一般用于扩展某个实体类增加若干数据字段。</summary>
        BaseFirst,

        /// <summary>派生类优先。一般用于具有某些公共数据字段的基类。</summary>
        DerivedFirst
    }

    /// <summary>模型字段排序模式。其实不是很重要，仅仅影响数据字段在数据表中的先后顺序而已</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ModelSortModeAttribute : Attribute
    {
        private ModelSortModes _Mode;
        /// <summary>模式</summary>
        public ModelSortModes Mode { get { return _Mode; } set { _Mode = value; } }

        /// <summary>指定实体类的模型字段排序模式</summary>
        /// <param name="mode"></param>
        public ModelSortModeAttribute(ModelSortModes mode) { Mode = mode; }
    }
}