using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>字段大小特性。可以通过Size指定字符串或数组的固有大小，为0表示自动计算；也可以通过指定参考字段ReferenceName，然后从其中获取大小</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class FieldSizeAttribute : Attribute
    {
        private Int32 _Size;
        /// <summary>大小。0表示自动计算大小</summary>
        public Int32 Size { get { return _Size; } set { _Size = value; } }

        private String _ReferenceName;
        /// <summary>参考大小字段名</summary>
        public String ReferenceName { get { return _ReferenceName; } set { _ReferenceName = value; } }

        /// <summary>通过Size指定字符串或数组的固有大小，为0表示自动计算</summary>
        /// <param name="size"></param>
        public FieldSizeAttribute(Int32 size) { Size = size; }

        /// <summary>指定参考字段ReferenceName，然后从其中获取大小</summary>
        /// <param name="referenceName"></param>
        public FieldSizeAttribute(String referenceName) { ReferenceName = referenceName; }
    }
}