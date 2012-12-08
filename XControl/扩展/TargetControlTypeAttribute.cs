using System;

namespace XControl
{
    /// <summary>目标控件类型</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TargetControlTypeAttribute : Attribute
    {
        private Type _Type;
        /// <summary>目标控件类型</summary>
        public Type Type
        {
            get { return _Type; }
            set { _Type = value; }
        }

        /// <summary>指定目标控件类型</summary>
        /// <param name="type"></param>
        public TargetControlTypeAttribute(Type type)
        {
            Type = type;
        }
    }
}