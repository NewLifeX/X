using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom.Compiler;
using System.Reflection;

namespace Test
{
    /**/
    /// <summary>
    /// 动态求值
    /// </summary>
    public class Evaluator
    {
        /**/
        /// <summary>
        /// 计算结果,如果表达式出错则抛出异常
        /// </summary>
        /// <param name="statement">表达式,如"1+2+3+4"</param>
        /// <returns>结果</returns>
        public static object Eval(string statement)
        {
            try
            {
                return _evaluatorType.InvokeMember(
                            "Eval",
                            BindingFlags.InvokeMethod,
                            null,
                            _evaluator,
                            new object[] { statement }
                         );
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null) throw ex.InnerException;
                throw;
            }
        }
        /**/
        /// <summary>
        /// 
        /// </summary>
        static Evaluator()
        {
            //构造JScript的编译驱动代码
            CodeDomProvider provider = CodeDomProvider.CreateProvider("JScript");

            CompilerParameters parameters;
            parameters = new CompilerParameters();
            parameters.GenerateInMemory = true;

            parameters.ReferencedAssemblies.Add("System");

            CompilerResults results;
            results = provider.CompileAssemblyFromSource(parameters, _jscriptSource);

            Assembly assembly = results.CompiledAssembly;
            _evaluatorType = assembly.GetType("Evaluator");

            _evaluator = Activator.CreateInstance(_evaluatorType);
        }

        private static object _evaluator = null;
        private static Type _evaluatorType = null;
        /**/
        /// <summary>
        /// JScript代码
        /// </summary>
        private static readonly string _jscriptSource =

            @"class Evaluator
              {
                  public function Eval(expr : String) : String 
                  { 
                     return eval(expr); 
                  }
              }";
    }
}
