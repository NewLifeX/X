using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using NewLife.Collections;
using NewLife.Exceptions;
using System.Reflection;

namespace NewLife.Reflection
{
    /// <summary>脚本引擎</summary>
    /// <remarks>
    /// 三大用法：
    /// 1，单个表达式，根据参数计算表达式结果并返回
    /// 2，多个语句，最后有返回语句
    /// 3，多个方法，有一个名为Execute的静态方法作为入口方法
    /// 
    /// 脚本引擎禁止实例化，必须通过<see cref="Create"/>方法创建，以代码为键进行缓存，避免重复创建反复编译形成泄漏。
    /// 其中<see cref="Create"/>方法的第二个参数为true表示前两种用法，为false表示第三种用法。
    /// </remarks>
    /// <example>
    /// 最简单而完整的用法：
    /// <code>
    /// // 根据代码创建脚本实例，相同代码只编译一次
    /// var se = ScriptEngine.Create("a+b");
    /// // 如果Method为空说明未编译，可设置参数
    /// if (se.Method == null)
    /// {
    ///     se.Parameters.Add("a", typeof(Int32));
    ///     se.Parameters.Add("b", typeof(Int32));
    /// }
    /// // 脚本固定返回Object类型，需要自己转换
    /// var n = (Int32)se.Invoke(2, 3);
    /// Console.WriteLine("2+3={0}", n);
    /// </code>
    /// 
    /// 无参数快速调用：
    /// <code>
    /// var n = (Int32)ScriptEngine.Execute("2*3");
    /// </code>
    /// 
    /// 约定参数快速调用：
    /// <code>
    /// var n = (Int32)ScriptEngine.Execute("p0*p1", new Object[] { 2, 3 });
    /// Console.WriteLine("2*3={0}", n);
    /// </code>
    /// </example>
    public class ScriptEngine
    {
        #region 属性
        private String _Code;
        /// <summary>代码</summary>
        public String Code { get { return _Code; } private set { _Code = value; } }

        private Boolean _IsExpression;
        /// <summary>是否表达式</summary>
        public Boolean IsExpression { get { return _IsExpression; } set { _IsExpression = value; } }

        private IDictionary<String, Type> _Parameters;
        /// <summary>参数集合。编译后就不可修改。</summary>
        public IDictionary<String, Type> Parameters { get { return _Parameters ?? (_Parameters = new Dictionary<String, Type>()); } }

        private String _FinalCode;
        /// <summary>最终代码</summary>
        public String FinalCode { get { return _FinalCode; } private set { _FinalCode = value; } }

        private MethodInfo _Method;
        /// <summary>根据代码编译出来可供直接调用的方法</summary>
        public MethodInfo Method { get { return _Method; } private set { _Method = value; } }

        private MethodInfoX _Mix;
        /// <summary>快速反射</summary>
        public MethodInfoX Mix { get { if (_Mix == null && Method != null)_Mix = MethodInfoX.Create(Method); return _Mix; } }

        static readonly String Refs =
            "using System;\r\n" +
            "using System.Collections;\r\n" +
            "using System.Diagnostics;\r\n" +
            "using System.Reflection;\r\n" +
            "using System.Text;\r\n" +
            "using System.IO;\r\n" +
            "" +
            "";
        #endregion

        #region 创建
        /// <summary>构造函数私有，禁止外部越过Create方法直接创建实例</summary>
        /// <param name="code">代码片段</param>
        /// <param name="isExpression">是否表达式，表达式将编译成为一个Main方法</param>
        private ScriptEngine(String code, Boolean isExpression)
        {
            Code = code;
            IsExpression = isExpression;
        }

        static DictionaryCache<String, ScriptEngine> _cache = new DictionaryCache<String, ScriptEngine>(StringComparer.OrdinalIgnoreCase);
        /// <summary>为指定代码片段创建脚本引擎实例。采用缓存，避免同一脚本重复创建引擎。</summary>
        /// <param name="code">代码片段</param>
        /// <param name="isExpression">是否表达式，表达式将编译成为一个Main方法</param>
        /// <returns></returns>
        public static ScriptEngine Create(String code, Boolean isExpression = true)
        {
            if (String.IsNullOrEmpty(code)) throw new ArgumentNullException("code");

            var key = code + isExpression;
            return _cache.GetItem<String, Boolean>(key, code, isExpression, (k, c, b) => new ScriptEngine(c, b));
        }
        #endregion

        #region 快速静态方法
        /// <summary>执行表达式，返回结果</summary>
        /// <param name="code">代码片段</param>
        /// <returns></returns>
        public static Object Execute(String code) { return Create(code).Invoke(); }

        /// <summary>执行表达式，返回结果</summary>
        /// <param name="code">代码片段</param>
        /// <param name="names">参数名称</param>
        /// <param name="parameterTypes">参数类型</param>
        /// <param name="parameters">参数值</param>
        /// <returns></returns>
        public static Object Execute(String code, String[] names, Type[] parameterTypes, Object[] parameters)
        {
            var se = Create(code);
            if (se != null && se.Method != null) return se.Invoke(parameters);

            if (names != null)
            {
                var dic = se.Parameters;
                for (int i = 0; i < names.Length; i++)
                {
                    dic.Add(names[i], parameterTypes[i]);
                }
            }

            return se.Invoke(parameters);
        }

        /// <summary>执行表达式，返回结果</summary>
        /// <param name="code">代码片段</param>
        /// <param name="parameters">参数名值对</param>
        /// <returns></returns>
        public static Object Execute(String code, IDictionary<String, Object> parameters)
        {
            if (parameters == null || parameters.Count < 1) return Execute(code);

            var ps = new Object[parameters.Count];
            parameters.Values.CopyTo(ps, 0);

            var se = Create(code);
            if (se != null && se.Method != null) return se.Invoke(ps);

            var names = new String[parameters.Count];
            parameters.Keys.CopyTo(names, 0);
            var types = TypeX.GetTypeArray(ps);

            var dic = se.Parameters;
            for (int i = 0; i < names.Length; i++)
            {
                dic.Add(names[i], types[i]);
            }

            return se.Invoke(ps);
        }

        /// <summary>执行表达式，返回结果。参数名默认为p0/p1/p2/pn</summary>
        /// <param name="code"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static Object Execute(String code, Object[] parameters)
        {
            if (parameters == null || parameters.Length < 1) return Execute(code);

            var se = Create(code);
            if (se != null && se.Method != null) return se.Invoke(parameters);

            var names = new String[parameters.Length];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = "p" + i;
            }
            var types = TypeX.GetTypeArray(parameters);

            var dic = se.Parameters;
            for (int i = 0; i < names.Length; i++)
            {
                dic.Add(names[i], types[i]);
            }

            return se.Invoke(parameters);
        }
        #endregion

        #region 动态编译
        /// <summary>编译</summary>
        public void Compile()
        {
            if (Method != null) return;
            lock (Parameters)
            {
                if (Method != null) return;

                // 预处理代码
                var code = Code;
                // 表达式需要构造一个语句
                if (IsExpression)
                {
                    // 如果代码不含有reutrn关键字，则在最前面加上，因为可能是简单表达式
                    if (!code.Contains("return ")) code = "return " + code;
                    if (!code.EndsWith(";")) code += ";";

                    var sb = new StringBuilder();
                    sb.Append("public static Object Execute(");
                    // 参数
                    Boolean isfirst = false;
                    foreach (var item in Parameters)
                    {
                        if (isfirst)
                            sb.Append(", ");
                        else
                            isfirst = true;

                        sb.AppendFormat("{0} {1}", item.Value.FullName, item.Key);
                    }
                    sb.AppendLine(")");
                    sb.AppendLine("{");
                    sb.AppendLine(code);
                    sb.AppendLine("}");

                    code = sb.ToString();
                }

                // 没有命名空间，包含一个
                if (!code.Contains("namespace "))
                {
                    // 没有类名，包含一个
                    if (!code.Contains("class ")) code = String.Format("public class {0}{{\r\n{1}\r\n}}", this.GetType().Name, code);

                    code = String.Format("namespace {0}{{\r\n{1}\r\n}}", this.GetType().Namespace, code);
                }

                // 加上默认引用
                code = Refs + Environment.NewLine + code;

                FinalCode = code;

                var rs = Compile(code, null);
                if (rs.Errors == null || !rs.Errors.HasErrors)
                {
                    var type = rs.CompiledAssembly.GetTypes()[0];
                    //Method = MethodInfoX.Create(type, "Execute");
                    Method = type.GetMethod("Execute");
                }
                else
                {
                    var err = rs.Errors[0];
                    var msg = String.Format("{0} {1} {2}({3},{4})", err.ErrorNumber, err.ErrorText, err.FileName, err.Line, err.Column);
                    throw new XException(msg);
                }
            }
        }

        /// <summary>编译</summary>
        /// <param name="classCode"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        static CompilerResults Compile(String classCode, CompilerParameters options)
        {
            if (options == null)
            {
                options = new CompilerParameters();
                options.GenerateInMemory = true;
            }

            foreach (var item in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (item is AssemblyBuilder) continue;

                String name = null;
                try
                {
                    name = item.Location;
                }
                catch { }
                if (String.IsNullOrEmpty(name)) continue;

                if (!options.ReferencedAssemblies.Contains(name)) options.ReferencedAssemblies.Add(name);
            }

            var provider = CodeDomProvider.CreateProvider("CSharp");

            return provider.CompileAssemblyFromSource(options, classCode);
        }
        #endregion

        #region 执行方法
        /// <summary>按照传入参数执行代码</summary>
        /// <param name="parameters">参数</param>
        /// <returns>结果</returns>
        public Object Invoke(params Object[] parameters)
        {
            if (Method == null) Compile();
            if (Method == null) throw new XException("脚本引擎未编译表达式！");

            // 这里反射调用方法，为了提高性能，我们采用快速反射
            //Method.Invoke(null, parameters);

            return Mix.Invoke(null, parameters);
        }
        #endregion
    }
}