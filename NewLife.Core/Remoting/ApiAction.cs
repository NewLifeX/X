using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NewLife.Remoting
{
    /// <summary>Api动作</summary>
    public class ApiAction
    {
        /// <summary>动作名称</summary>
        public string Name { get; set; }

        /// <summary>方法</summary>
        public MethodInfo Method { get; set; }

        /// <summary>控制器对象</summary>
        /// <remarks>如果指定控制器对象，则每次调用前不再实例化对象</remarks>
        public Object Controller { get; set; }

        /// <summary>动作过滤器</summary>
        public IActionFilter[] ActionFilters { get; }

        /// <summary>异常过滤器</summary>
        public IExceptionFilter[] ExceptionFilters { get; }

        /// <summary>实例化</summary>
        public ApiAction(MethodInfo method)
        {
            if (method.DeclaringType != null)
            {
                var typeName = method.DeclaringType.Name.TrimEnd("Controller");
                var att = method.DeclaringType.GetCustomAttribute<ApiAttribute>();
                if (att != null) typeName = att.Name;

                var miName = method.Name;
                att = method.GetCustomAttribute<ApiAttribute>();
                if (att != null) miName = att.Name;

                if (typeName.IsNullOrEmpty())
                    Name = miName;
                else
                    Name = "{0}/{1}".F(typeName, miName);
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
            //return "{0}\t{1}".F(Method.GetDisplayName() ?? Name, Method);

            var sb = new StringBuilder();
            sb.AppendFormat("{0} {1}", Method.ReturnType.Name, Name);
            sb.Append("(");

            var pis = Method.GetParameters();
            for (int i = 0; i < pis.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.AppendFormat("{0} {1}", pis[i].ParameterType.Name, pis[i].Name);
            }

            sb.Append(")");

            return sb.ToString();
        }
    }
}