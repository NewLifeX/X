using System;
using System.Collections.Generic;
using System.Text;

namespace System.Runtime.CompilerServices
{
    /// <summary>支持使用扩展方法的特性</summary>
    /// <remarks>
    /// 为了能在vs2010+.Net 2.0中使用扩展方法，添加该特性。
    /// 在vs2010+.Net4.0中引用当前程序集，会爆一个预定义类型多次定义的警告，不影响使用。
    /// 2011-11-15
    /// @补丁 建议独立NewLife.Linq.dll以规避.Net 4.0中冲突的问题。
    /// 但仔细想想，不管分两个DLL还是何在一起，还是会冲突的，除非所有组件都编译两份，其中一份For .Net 4.0不需要这个扩展方法的。
    /// 在@Aimeast和@小董 的帮助下，决定枚举器扩展的命名空间从System.Linq改为NewLife.Linq。
    /// </remarks>
    public sealed class ExtensionAttribute : Attribute { }
}