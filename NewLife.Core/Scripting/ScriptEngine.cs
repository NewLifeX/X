using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp;

namespace NewLife.Scripting
{
    /// <summary>脚本引擎</summary>
    public class ScriptEngine
    {
        #region 属性
        private String _Expression;
        /// <summary>源码脚本</summary>
        public String Expression { get { return _Expression; } private set { _Expression = value; } }

        private static String _TypeName = "NewLife.Core.DynamicScriptEngine";

        /// <summary></summary>
        public Object DynamicInstance = null;
        #endregion

        #region 创建
        #endregion

        #region 动态编译
        /// <summary>动态编译</summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private static object DynamicCompile(String expression)
        {
            //创建Provider
            var provider = new CSharpCodeProvider();
            //创建编译参数
            var ps = new CompilerParameters();
            ps.ReferencedAssemblies.Add("System.dll");
            //objCompilerParameters.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            ps.GenerateInMemory = true;
            //创建编译器返回结果
            var cr = provider.CompileAssemblyFromSource(ps, expression);
            //检测是否存在编译错误
            if (cr.Errors.HasErrors)
            {
                string strErrorMsg = cr.Errors.Count.ToString() + " Errors:";
                for (int x = 0; x < cr.Errors.Count; x++)
                {
                    strErrorMsg = strErrorMsg + Environment.NewLine + "Line: " +
                                 cr.Errors[x].Line.ToString() + " - " +
                                 cr.Errors[x].ErrorText;
                }
                throw new Exception(strErrorMsg);
            }
            //获取编译结果的程序集
            var objAssembly = cr.CompiledAssembly;

            _TypeName = GetNameSpacesInSourceCode(expression) + "." + GetClassInSourceCode(expression);

            object objClass = objAssembly.CreateInstance(_TypeName);
            return objClass;
        }
        #endregion

        #region 提取命名空间及类名
        static List<string> GetNameSpacesInSourceCode(string code)
        {
            return GetMatchStrings(code, @"using\s+(.+?)\s*;", false);
        }

        static List<string> GetClassInSourceCode(string code)
        {
            return GetMatchStrings(code, @"class\s+(.+?)\s*;", false);
        }

        static List<string> GetMatchStrings(String text, String regx, bool ignoreCase)
        {
            var output = new List<string>();

            Regex reg;

            int index = 0;
            int begin = 0;
            index = regx.IndexOf("(.+)");
            if (index < 0)
            {
                index = regx.IndexOf("(.+?)");
                if (index >= 0)
                {
                    begin = index + 5;
                }
            }
            else
            {
                begin = index + 4;
            }

            if (index >= 0)
            {
                String endText = regx.Substring(begin);

                if (GetMatch(text, endText, ignoreCase) == "")
                {
                    return output;
                }
            }

            if (ignoreCase)
            {
                reg = new Regex(regx, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }
            else
            {
                reg = new Regex(regx, RegexOptions.Singleline);
            }

            MatchCollection m = reg.Matches(text);

            if (m.Count == 0) return output;

            for (int j = 0; j < m.Count; j++)
            {
                int count = m[j].Groups.Count;

                for (int i = 1; i < count; i++)
                {
                    output.Add(m[j].Groups[i].Value.Trim());
                }
            }

            return output;

        }

        static String GetMatch(String text, String regx, bool ignoreCase)
        {
            Regex reg;

            int index = 0;
            int begin = 0;
            index = regx.IndexOf("(.+)");
            if (index < 0)
            {
                index = regx.IndexOf("(.+?)");
                if (index >= 0)
                {
                    begin = index + 5;
                }
            }
            else
            {
                begin = index + 4;
            }

            if (index >= 0)
            {
                String endText = regx.Substring(begin);

                if (endText != "")
                {
                    if (GetMatch(text, endText, ignoreCase) == "")
                    {
                        return "";
                    }
                }
            }

            if (ignoreCase)
            {
                reg = new Regex(regx, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }
            else
            {
                reg = new Regex(regx, RegexOptions.Singleline);
            }

            String ret = "";
            Match m = reg.Match(text);

            if (m.Groups.Count > 0)
            {
                ret = m.Groups[m.Groups.Count - 1].Value;
            }

            return ret;
        }
        #endregion

        #region 传入表达式或代码
        /// <summary>从字符串创建脚本</summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public ScriptEngine CreateScriptSourceFromString(String expression)
        {
            _Expression = expression;
            object objClass = DynamicCompile(expression);

            DynamicInstance = objClass;
            return this;
        }

        /// <summary>从文件创建脚本</summary>
        /// <param name="file"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public ScriptEngine CreateScriptSourceFromFile(String file, Encoding encoding = null)
        {
            String code = String.Empty;
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                Byte[] buffer = new Byte[fs.Length];
                fs.Read(buffer, 0, buffer.Length);
                code = encoding.GetString(buffer);
            }

            CreateScriptSourceFromString(code);
            return this;
        }
        #endregion

        #region 执行方法
        /// <summary>执行</summary>
        /// <returns></returns>
        public Object Execute()
        {
            return null;
        }
        #endregion

        #region 快速静态方法
        /// <summary>执行表达式，返回结果</summary>
        /// <param name="expression"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static Object Execute(String expression, params Object[] parameters)
        {
            var instance = DynamicCompile(expression);
            return null;
        }

        /// <summary>执行代码，返回结果</summary>
        /// <param name="code"></param>
        /// <param name="parameterTypes"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static Object Execute(String code, Type[] parameterTypes, params Object[] parameters)
        {
            return null;
        }
        #endregion
    }
}