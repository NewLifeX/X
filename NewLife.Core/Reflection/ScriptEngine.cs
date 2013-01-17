using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using NewLife.Collections;
using NewLife.Exceptions;

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

        //static readonly String Refs =
        //    "using System;\r\n" +
        //    "using System.Collections;\r\n" +
        //    "using System.Diagnostics;\r\n" +
        //    "using System.Reflection;\r\n" +
        //    "using System.Text;\r\n" +
        //    "using System.IO;\r\n" +
        //    "" +
        //    "";

        private StringCollection _NameSpaces = new StringCollection{
            "System",
            "System.Collections",
            "System.Diagnostics",
            "System.Reflection",
            "System.Text",
            "System.IO"};
        /// <summary>命名空间集合</summary>
        public StringCollection NameSpaces { get { return _NameSpaces; } set { _NameSpaces = value; } }

        private StringCollection _ReferencedAssemblies = new StringCollection();
        /// <summary>引用程序集集合</summary>
        public StringCollection ReferencedAssemblies { get { return _ReferencedAssemblies; } set { _ReferencedAssemblies = value; } }
        #endregion

        #region 自定义添加命名空间属性和方法
        //private HashSet<String> CusNameSpaceList = new HashSet<String>();
        ///// <summary>添加命名空间</summary>
        ///// <param name="nameSpace"></param>
        //public void AddNameSpace(String nameSpace)
        //{
        //    // 对于字符串来说，哈希集合的Contains更快
        //    if (!String.IsNullOrEmpty(nameSpace) && !CusNameSpaceList.Contains(nameSpace))
        //        CusNameSpaceList.Add(nameSpace);
        //}

        ////private String _cusNameSpaceStr = null;
        ///// <summary>获取自定义命名空间</summary>
        ///// <returns></returns>
        //protected String GetCusNameSpaceStr()
        //{
        //    if (CusNameSpaceList.Count == 0) return String.Empty;
        //    //if (_cusNameSpaceStr != null) return _cusNameSpaceStr;

        //    //_cusNameSpaceStr = String.Empty;
        //    //CusNameSpaceList.ForEach(names =>
        //    //{
        //    //    if (names[names.Length - 1] != ';')
        //    //        names += ";";
        //    //    if (!names.StartsWith("using"))
        //    //        names = "using " + names;
        //    //    _cusNameSpaceStr += names + "\r\n";
        //    //});
        //    //return _cusNameSpaceStr;

        //    // 字符串相加有一定损耗
        //    var sb = new StringBuilder(20 * CusNameSpaceList.Count);
        //    foreach (var item in CusNameSpaceList)
        //    {
        //        if (!item.StartsWith("using")) sb.Append("using ");
        //        sb.Append(item);
        //        if (item[item.Length - 1] != ';') sb.Append(";");

        //        sb.AppendLine();
        //    }

        //    return sb.ToString();
        //}
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
        /// <summary>生成代码。根据<see cref="Code"/>完善得到最终代码<see cref="FinalCode"/></summary>
        public void GenerateCode()
        {
            if (String.IsNullOrEmpty(Code)) throw new ArgumentNullException("Code");

            // 预处理代码
            var code = Code;
            // 把命名空间提取出来
            code = ParseNameSpace(code);

            // 表达式需要构造一个语句
            if (IsExpression)
            {
                // 如果代码不含有reutrn关键字，则在最前面加上，因为可能是简单表达式
                if (!code.Contains("return ")) code = "return " + code;
                if (!code.EndsWith(";")) code += ";";

                var sb = new StringBuilder(64 + code.Length);
                sb.Append("public static Object Main(");
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
                sb.AppendLine("{");
                sb.AppendLine(code);
                sb.AppendLine("}");

                code = sb.ToString();
            }
            //else if (!code.Contains("static void Main("))
            // 这里也许用正则判断会更好一些
            else if (!code.Contains(" Main("))
            {
                code = String.Format("static void Main() {{\r\n\t{0}\r\n}}", code);
            }

            // 没有命名空间，包含一个
            if (!code.Contains("namespace "))
            {
                // 没有类名，包含一个
                if (!code.Contains("class "))
                {
                    code = String.Format("public class {0}{{\r\n{1}\r\n}}", this.GetType().Name, code);
                }

                code = String.Format("namespace {0}{{\r\n{1}\r\n}}", this.GetType().Namespace, code);
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

            FinalCode = code;
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
                    var type = rs.CompiledAssembly.GetTypes()[0];
                    //Method = MethodInfoX.Create(type, "Execute");
                    Method = type.GetMethod("Main", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                }
                else
                {
                    var err = rs.Errors[0];
                    throw new XException("{0} {1} {2}({3},{4})", err.ErrorNumber, err.ErrorText, err.FileName, err.Line, err.Column);
                }
            }
        }

        /// <summary>编译</summary>
        /// <param name="classCode"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        CompilerResults Compile(String classCode, CompilerParameters options)
        {
            if (options == null)
            {
                options = new CompilerParameters();
                options.GenerateInMemory = true;
            }

            var hs = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
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

                if (!options.ReferencedAssemblies.Contains(name)) options.ReferencedAssemblies.Add(name);
            }
            foreach (var item in ReferencedAssemblies)
            {
                if (hs.Contains(item)) continue;
                hs.Add(item);

                var name = item;
                if (String.IsNullOrEmpty(name)) continue;

                if (!options.ReferencedAssemblies.Contains(name)) options.ReferencedAssemblies.Add(name);
            }

            var provider = CodeDomProvider.CreateProvider("CSharp");
            //var provider = CreateProvider();

            return provider.CompileAssemblyFromSource(options, classCode);

            //var tf = XTrace.TempPath.CombinePath(Path.GetRandomFileName() + ".cs");
            //File.WriteAllText(tf, classCode);
            //try
            //{
            //    return CompileAssemblyFromSource(options, tf);
            //}
            //finally
            //{
            //    File.Delete(tf);
            //}
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

            return sb.ToString();
        }
        #endregion

        #region 辅助方法
        ///// <summary>不知道是否有线程冲突</summary>
        ////[ThreadStatic]
        //private static CodeDomProvider _provider;
        ///// <summary>建立代码编译提供者。尝试采用最新版本的编译器</summary>
        ///// <returns></returns>
        //public static CodeDomProvider CreateProvider()
        //{
        //    if (_provider != null) return _provider;

        //    try
        //    {
        //        var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\MSBuild\ToolsVersions");
        //        var names = reg.GetSubKeyNames();
        //        for (int i = 0; i < names.Length; i++)
        //        //for (int i = names.Length - 1; i >= 0; i--)
        //        {
        //            var ver = names[i];
        //            reg = reg.OpenSubKey(ver);

        //            return _provider = new CSharpCodeProvider(new Dictionary<String, String>() { { "CompilerVersion", "v" + ver } });
        //        }

        //        return _provider = CodeDomProvider.CreateProvider("CSharp");
        //    }
        //    catch
        //    {
        //        return _provider = CodeDomProvider.CreateProvider("CSharp");
        //    }
        //}

        //static String GetBuildToolsPath()
        //{
        //    try
        //    {
        //        var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\MSBuild\ToolsVersions");
        //        var names = reg.GetSubKeyNames();
        //        for (int i = 0; i < names.Length; i++)
        //        //for (int i = names.Length - 1; i >= 0; i--)
        //        {
        //            var ver = names[i];
        //            reg = reg.OpenSubKey(ver);

        //            var path = reg.GetValue("MSBuildToolsPath") as String;
        //            if (!String.IsNullOrEmpty(path) && Directory.Exists(path)) return path;
        //        }

        //        return RuntimeEnvironment.GetRuntimeDirectory();
        //    }
        //    catch
        //    {
        //        return RuntimeEnvironment.GetRuntimeDirectory();
        //    }
        //}

        //private static CompilerResults CompileAssemblyFromSource(CompilerParameters options, String fileName)
        //{
        //    if (options == null) throw new ArgumentNullException("options");

        //    int nativeReturnValue = 0;
        //    var results = new CompilerResults(options.TempFiles);

        //    if (options.OutputAssembly == null || options.OutputAssembly.Length == 0)
        //    {
        //        var ext = options.GenerateExecutable ? "exe" : "dll";
        //        options.OutputAssembly = results.TempFiles.AddExtension(ext, !options.GenerateInMemory);
        //        new FileStream(options.OutputAssembly, FileMode.Create, FileAccess.ReadWrite).Close();
        //    }
        //    results.TempFiles.AddExtension("pdb");

        //    var cmdArgs = CmdArgsFromParameters(options) + " \"" + fileName + "\"";
        //    nativeReturnValue = Compile(options, cmdArgs);
        //    results.NativeCompilerReturnValue = nativeReturnValue;

        //    if (!results.Errors.HasErrors && options.GenerateInMemory)
        //    {
        //        var buffer = File.ReadAllBytes(options.OutputAssembly);
        //        results.CompiledAssembly = Assembly.Load(buffer, null, options.Evidence);
        //        return results;
        //    }
        //    results.PathToAssembly = options.OutputAssembly;
        //    return results;
        //}

        //static Int32 Compile(CompilerParameters options, String arguments)
        //{
        //    String errorName = null;
        //    var outputFile = options.TempFiles.AddExtension("out");

        //    var path = Path.Combine(GetBuildToolsPath(), "csc.exe");

        //    //var pis = options.GetType().GetProperty("SafeUserToken", BindingFlags.NonPublic | BindingFlags.Instance);
        //    //var ip = (IntPtr)pis.GetValue(options, null);

        //    return Executor.ExecWaitWithCapture(IntPtr.Zero, "\"" + path + "\" " + arguments, Environment.CurrentDirectory, options.TempFiles, ref outputFile, ref errorName);
        //}

        //private static String CmdArgsFromParameters(CompilerParameters options)
        //{
        //    var sb = new StringBuilder(128);
        //    if (options.GenerateExecutable)
        //    {
        //        sb.Append("/t:exe ");
        //        if (options.MainClass != null && options.MainClass.Length > 0)
        //        {
        //            sb.Append("/main:");
        //            sb.Append(options.MainClass);
        //            sb.Append(" ");
        //        }
        //    }
        //    else
        //        sb.Append("/t:library ");
        //    sb.Append("/utf8output ");
        //    foreach (string str in options.ReferencedAssemblies)
        //    {
        //        sb.Append("/R:");
        //        sb.Append("\"");
        //        sb.Append(str);
        //        sb.Append("\"");
        //        sb.Append(" ");
        //    }
        //    sb.Append("/out:");
        //    sb.Append("\"");
        //    sb.Append(options.OutputAssembly);
        //    sb.Append("\"");
        //    sb.Append(" ");
        //    if (options.IncludeDebugInformation)
        //    {
        //        sb.Append("/D:DEBUG ");
        //        sb.Append("/debug+ ");
        //        sb.Append("/optimize- ");
        //    }
        //    else
        //    {
        //        sb.Append("/debug- ");
        //        sb.Append("/optimize+ ");
        //    }
        //    if (options.Win32Resource != null) sb.Append("/win32res:\"" + options.Win32Resource + "\" ");
        //    foreach (string str2 in options.EmbeddedResources)
        //    {
        //        sb.Append("/res:\"");
        //        sb.Append(str2);
        //        sb.Append("\" ");
        //    }
        //    foreach (string str3 in options.LinkedResources)
        //    {
        //        sb.Append("/linkres:\"");
        //        sb.Append(str3);
        //        sb.Append("\" ");
        //    }
        //    if (options.TreatWarningsAsErrors) sb.Append("/warnaserror ");
        //    if (options.WarningLevel >= 0) sb.Append("/w:" + options.WarningLevel + " ");
        //    if (options.CompilerOptions != null) sb.Append(options.CompilerOptions + " ");
        //    return sb.ToString();
        //}
        #endregion
    }
}