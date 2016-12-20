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

        /// <summary>过滤器</summary>
        public IActionFilter[] Filters { get; }

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

            Filters = GetAllFilters(method);
        }

        private IActionFilter[] GetAllFilters(MethodInfo method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            var fs = new List<IActionFilter>();
            var atts = method.GetCustomAttributes<ActionFilterAttribute>(true);
            if (atts != null) fs.AddRange(atts);
            atts = method.DeclaringType.GetCustomAttributes<ActionFilterAttribute>(true);
            if (atts != null) fs.AddRange(atts);

            fs.AddRange(GlobalFilters.Filters);

            // 排序
            var arr = fs.OrderBy(e => (e as ActionFilterAttribute)?.Order ?? 0).ToArray();

            return arr;
        }
    }
}