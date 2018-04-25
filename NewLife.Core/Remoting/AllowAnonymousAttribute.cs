using System;

namespace NewLife.Remoting
{
    /// <summary>Api是否可以匿名使用</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class AllowAnonymousAttribute : Attribute { }
}