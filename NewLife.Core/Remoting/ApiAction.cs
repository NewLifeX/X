using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NewLife.Reflection;

namespace NewLife.Remoting
{
    /// <summary>Api动作</summary>
    public class ApiAction
    {
        /// <summary>动作名称</summary>
        public String Name { get; set; }

        /// <summary>动作所在类型</summary>
        public Type Type { get; set; }

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
        public ApiAction(MethodInfo method, Type type)
        {
            if (type == null) type = method.DeclaringType;
            if (type != null)
            {
                var typeName = type.Name.TrimEnd("Controller");
                var att = type.GetCustomAttribute<ApiAttribute>(true);
                if (att != null) typeName = att.Name;

                var miName = method.Name;
                att = method.GetCustomAttribute<ApiAttribute>();
                if (att != null) miName = att.Name;

                if (typeName.IsNullOrEmpty())
                    Name = miName;
                else
                    Name = "{0}/{1}".F(typeName, miName);
            }
            // 必须同时记录类型和方法，因为有些方法位于继承的不同层次，那样会导致实例化的对象不一致
            Type = type;
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

            var type = Method.ReturnType;
            var rtype = type.Name;
            if (type.As<Task>())
            {
                if (type.IsGenericType)
                    rtype = "Void";
                else
                {
                    type = type.GetGenericArguments()[0];
                    rtype = type.Name;
                }
            }

            var sb = new StringBuilder();
            sb.AppendFormat("{0} {1}", rtype, Name);
            sb.Append("(");

            var pis = Method.GetParameters();
            for (var i = 0; i < pis.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.AppendFormat("{0} {1}", pis[i].ParameterType.Name, pis[i].Name);
            }

            sb.Append(")");

            return sb.ToString();
        }
    }
}