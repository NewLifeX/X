using System;

namespace NewLife.Remoting
{
    /// <summary>标识Api</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class ApiAttribute : Attribute
    {
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>是否在会话上复用控制器。复用控制器可确保同一个会话多次请求路由到同一个控制器对象实例</summary>
        public Boolean IsReusable { get; set; }

        /// <summary>实例化</summary>
        /// <param name="name"></param>
        public ApiAttribute(String name) { Name = name; }

        /// <summary>实例化</summary>
        /// <param name="name"></param>
        /// <param name="reusable"></param>
        public ApiAttribute(String name, Boolean reusable) { Name = name; IsReusable = reusable; }
    }
}