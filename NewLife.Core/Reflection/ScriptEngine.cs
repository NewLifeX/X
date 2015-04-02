using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using NewLife.Collections;
using NewLife.Log;

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
        public String FinalCode { get { if (_FinalCode == null && !String.IsNullOrEmpty(Code)) GenerateCode(); return _FinalCode; } private set { _FinalCode = value; } }

        private MethodInfo _Method;
        /// <summary>根据代码编译出来可供直接调用的方法</summary>
        public MethodInfo Method { get { return _Method; } private set { _Method = value; } }

        private StringCollection _NameSpaces = new StringCollection{
            "System",
            "System.Collections",
            "System.Diagnostics",
            "System.Reflection",
            "System.Text",
            "System.Linq",
            "System.IO"};
        /// <summary>命名空间集合</summary>
        public StringCollection NameSpaces { get { return _NameSpaces; } set { _NameSpaces = value; } }

        private StringCollection _ReferencedAssemblies = new StringCollection();
        /// <summary>引用程序集集合</summary>
        public StringCollection ReferencedAssemblies { get { return _ReferencedAssemblies; } set { _ReferencedAssemblies = value; } }

        private ILog _Log;
        /// <summary>日志</summary>
        public ILog Log { get { return _Log; } set { _Log = value; } }
        #endregion

        #region 创建
        static ScriptEngine()
        {
            // 考虑到某些要引用的程序集在别的目录
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

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
                for (var i = 0; i < names.Length; i++)
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
            var types = ps.GetTypeArray();

            var dic = se.Parameters;
            for (int i = 0; i < names.Length; i++)
            {
                dic.Add(names[i], types[i]);
            }

            return se.Invoke(ps);
        }

        /// <summary>执行表达式，返回结果。参数名默认为p0/p1/p2/pn</summary>
        /// <param name="code"></param>
        /// <param name="parameters">参数数组</param>
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
            var types = parameters.GetTypeArray();

            var dic = se.Parameters;
            for (int i = 0; i < names.Length; i++)
            {
                dic.Add(names[i], types[i]);
            }

            return se.Invoke(parameters);
        }
        #endregion

        #region 动态编译
        /// <summary>生成代码。根据<see cref="Code"/>完善得到最终代码<see cref="FinalCode"/></summary>
        public void GenerateCode() { FinalCode = GetFullCode(); }

        /// <summary>获取完整源代码</summary>
        /// <returns></returns>
        public String GetFullCode()
        {
            if (String.IsNullOrEmpty(Code)) throw new ArgumentNullException("Code");

            // 预处理代码
            var code = Code.Trim();
            // 把命名空间提取出来
            code = ParseNameSpace(code);

            // 表达式需要构造一个语句
            if (IsExpression)
            {
                // 如果代码不含有reutrn关键字，则在最前面加上，因为可能是简单表达式
                if (!code.Contains("return ")) code = "return " + code;
                if (!code.EndsWith(";")) code += ";";

                var sb = new StringBuilder(64 + code.Length);
                sb.Append("\t\tpublic static Object Eval(");
                // 参数
                var isfirst = false;
                foreach (var item in Parameters)
                {
                    if (isfirst)
                        sb.Append(", ");
                    else
                        isfirst = true;

                    sb.AppendFormat("{0} {1}", item.Value.FullName, item.Key);
                }
                sb.AppendLine(")");
                sb.AppendLine("\t\t{");
                sb.Append("\t\t\t");
                sb.AppendLine(code);
                sb.AppendLine("\t\t}");

                code = sb.ToString();
            }
            //else if (!code.Contains("static void Main("))
            // 这里也许用正则判断会更好一些
            else if (!code.Contains(" Main("))
            {
                // 如果不是;和}结尾，则增加分号
                var last = code[code.Length - 1];
                if (last != ';' && last != '}') code += ";";
                code = String.Format("\t\tstatic void Main()\r\n\t\t{{\r\n\t\t\t{0}\r\n\t\t}}", code);
            }

            // 没有命名空间，包含一个
            if (!code.Contains("namespace "))
            {
                // 没有类名，包含一个
                if (!code.Contains("class "))
                {
                    code = String.Format("\tpublic class {0}\r\n\t{{\r\n{1}\r\n\t}}", this.GetType().Name, code);
                }

                code = String.Format("namespace {0}\r\n{{\r\n{1}\r\n}}", this.GetType().Namespace, code);
            }

            // 命名空间
            if (NameSpaces.Count > 0)
            {
                var sb = new StringBuilder(code.Length + NameSpaces.Count * 20);
                foreach (var item in NameSpaces)
                {
                    sb.AppendFormat("using {0};\r\n", item);
                }
                sb.AppendLine();
                sb.Append(code);

                code = sb.ToString();
            }

            return code;
        }

        /// <summary>编译</summary>
        public void Compile()
        {
            if (Method != null) return;
            lock (Parameters)
            {
                if (Method != null) return;

                if (FinalCode == null) GenerateCode();

                var rs = Compile(FinalCode, null);
                if (rs.Errors == null || !rs.Errors.HasErrors)
                {
                    // 加载外部程序集
                    foreach (var item in ReferencedAssemblies)
                    {
                        try
                        {
                            //Assembly.LoadFrom(item);
                            // 先加载到内存，再加载程序集，避免文件被锁定
                            Assembly.Load(File.ReadAllBytes(item));
                            WriteLog("加载外部程序集：{0}", item);
                        }
                        catch { }
                    }

                    try
                    {
                        var type = rs.CompiledAssembly.GetTypes()[0];
                        var name = IsExpression ? "Eval" : "Main";
                        Method = type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        if (ex.LoaderExceptions != null && ex.LoaderExceptions.Length > 0)
                            throw ex.LoaderExceptions[0];
                        else
                            throw;
                    }
                }
                else
                {
                    var err = rs.Errors[0];

                    // 异常中输出错误代码行
                    var code = "";
                    var ss = FinalCode.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    if (err.Line > 0 && err.Line <= ss.Length) code = ss[err.Line - 1].Trim();

                    throw new XException("{0} {1} {2}({3},{4}) {5}", err.ErrorNumber, err.ErrorText, err.FileName, err.Line, err.Column, code);
                }
            }
        }

        /// <summary>编译</summary>
        /// <param name="classCode"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public CompilerResults Compile(String classCode, CompilerParameters options)
        {
            if (options == null)
            {
                options = new CompilerParameters();
                options.GenerateInMemory = true;
                options.GenerateExecutable = !IsExpression;
            }

            var hs = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            // 同名程序集只引入一个
            var fs = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            // 优先考虑外部引入的程序集
            foreach (var item in ReferencedAssemblies)
            {
                if (String.IsNullOrEmpty(item)) continue;
                if (hs.Contains(item)) continue;
                var name = Path.GetFileName(item);
                if (fs.Contains(name)) continue;

                hs.Add(item);
                fs.Add(name);

                if (!options.ReferencedAssemblies.Contains(item)) options.ReferencedAssemblies.Add(item);
            }
            foreach (var item in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (item is AssemblyBuilder) continue;

                // 三趾树獭  303409914 发现重复加载同一个DLL，表现为Web站点Bin目录有一个，系统缓存有一个
                if (hs.Contains(item.FullName)) continue;
                hs.Add(item.FullName);

                String name = null;
                try
                {
                    name = item.Location;
                }
                catch { }
                if (String.IsNullOrEmpty(name)) continue;

                var fname = Path.GetFileName(name);
                if (fs.Contains(fname)) continue;
                fs.Add(fname);

                if (!options.ReferencedAssemblies.Contains(name)) options.ReferencedAssemblies.Add(name);
            }

            var provider = CodeDomProvider.CreateProvider("CSharp");
            //var arg = (String)provider.CreateGenerator().Invoke("CmdArgsFromParameters", options);
            //XTrace.WriteLine(arg);
#if !NET4
            Check35(provider);
#endif
            return provider.CompileAssemblyFromSource(options, classCode);
        }

        void Check35(CodeDomProvider provider)
        {
            // 如果是2.0，为了使用扩展方法，直接调用3.5编译器
            if (XTrace.Debug) XTrace.WriteLine("当前环境是2.0，为了使用扩展方法等，准备调用3.5编译器");
            // 先找到2.0路径，隔壁就是3.5，如果不存在，则下载并解压
            var dir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            var fdir = dir.AsDirectory().Parent.FullName;
            dir = fdir.CombinePath("v3.5");
            var file = dir.CombinePath("csc.exe");
            if (!Directory.Exists(dir) || !File.Exists(file))
            {
                var url = "http://www.newlifex.com/showtopic-1348.aspx";
                XTrace.WriteLine(".Net 3.5未安装，准备下载绿色版 " + url);
                var client = new Web.WebClientX(true, true);
                try
                {
                    var zip = client.DownloadLink(url, "csc_v3.5", dir.EnsureDirectory());
                    if (File.Exists(zip))
                    {
                        NewLife.Compression.ZipFile.Extract(zip, dir);
                    }
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
            }
            if (File.Exists(file))
            {
                Environment.SetEnvironmentVariable("COMPLUS_InstallRoot", fdir);
                Environment.SetEnvironmentVariable("COMPLUS_Version", "v3.5");
                //Environment.SetEnvironmentVariable("COMPLUS_Version", "v4.0.30319");

                //var type = TypeX.GetType("RedistVersionInfo");
                //var dic = new Dictionary<String, String>();
                //dic.Add("CompilerVersion", "v3.5");
                //var path = (String)type.Invoke("GetCompilerPath", dic, "");
                //XTrace.WriteLine(path);

                //var gen = provider.CreateGenerator();
                var gen = provider.GetValue("generator") as ICodeGenerator;
                var provOptions = gen.GetValue("provOptions", false) as IDictionary<string, string>;
                if (provOptions != null) provOptions["CompilerVersion"] = "v3.5";
            }
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
            // Main函数可能含有参数string[] args
            if (parameters == null || parameters.Length == 0)
            {
                var ms = Method.GetParameters();
                if (Method.Name.EqualIgnoreCase("Main") && ms.Length == 1 && ms[0].ParameterType == typeof(String[]))
                {
                    parameters = new Object[] { new String[] { "" } };
                }
            }
            return "".Invoke(Method, parameters);
        }
        #endregion

        #region 辅助
        /// <summary>分析命名空间</summary>
        /// <param name="code"></param>
        /// <returns></returns>
        String ParseNameSpace(String code)
        {
            var sb = new StringBuilder();

            var ss = code.Split(new String[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (var item in ss)
            {
                // 提取命名空间
                if (!String.IsNullOrEmpty(item))
                {
                    var line = item.Trim();
                    if (line.StartsWith("using ") && line.EndsWith(";"))
                    {
                        var len = "using ".Length;
                        line = line.Substring(len, line.Length - len - 1);
                        if (!NameSpaces.Contains(line)) NameSpaces.Add(line);
                        continue;
                    }
                }

                sb.AppendLine(item);
            }

            return sb.ToString().Trim();
        }

        void WriteLog(String format, params Object[] args)
        {
            if (Log != null) Log.Info(format, args);
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = args.Name;
            if (String.IsNullOrEmpty(name)) return null;

            // 遍历现有程序集
            foreach (var item in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (item.FullName == name) return item;
            }

            return null;
        }
        #endregion
    }
}