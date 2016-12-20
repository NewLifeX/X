using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NewLife.Remoting
{
    /// <summary>Api动作</summary>
    public class ApiAction
    {
        /// <summary>动作名称</summary>
        public string Name { get; set; }

        /// <summary>方法</summary>
        public MethodInfo Method { get; set; }

        /// <summary>动作过滤器</summary>
        public IActionFilter[] ActionFilters { get; }

        /// <summary>异常过滤器</summary>
        public IExceptionFilter[] ExceptionFilters { get; }

        /// <summary>实例化</summary>
        public ApiAction(MethodInfo method)
        {
            if (method.DeclaringType != null)
            {
                var name = method.DeclaringType.Name.TrimEnd("Controller");
                var miName = method.Name;

                Name = "{0}/{1}".F(name, miName);
            }
            Method = method;

            ActionFilters = GetAllFilters(method);
            ExceptionFilters = GetAllExceptionFilters(method);
        }

        private IActionFilter[] GetAllFilters(MethodInfo method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            var fs = new List<IActionFilter>();
            var atts = method.GetCustomAttributes<ActionFilterAttribute>(true);
            if (atts != null) fs.AddRange(atts);
            atts = method.DeclaringType.GetCustomAttributes<ActionFilterAttribute>(true);
            if (atts != null) fs.AddRange(atts);

            fs.AddRange(GlobalFilters.ActionFilters);

            // 排序
            var arr = fs.OrderBy(e => (e as FilterAttribute)?.Order ?? 0).ToArray();

            return arr;
        }

        private IExceptionFilter[] GetAllExceptionFilters(MethodInfo method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            var fs = new List<IExceptionFilter>();
            var atts = method.GetCustomAttributes<HandleErrorAttribute>(true);
            if (atts != null) fs.AddRange(atts);
            atts = method.DeclaringType.GetCustomAttributes<HandleErrorAttribute>(true);
            if (atts != null) fs.AddRange(atts);

            fs.AddRange(GlobalFilters.ExceptionFilters);

            // 排序
            var arr = fs.OrderBy(e => (e as FilterAttribute)?.Order ?? 0).ToArray();

            return arr;
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString()
        {
            return "{0}\t{1}".F(Name, Method);
        }
    }
}