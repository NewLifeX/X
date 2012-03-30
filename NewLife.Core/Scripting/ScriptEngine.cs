using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Scripting
{
    /// <summary>脚本引擎</summary>
    public class ScriptEngine
    {
        #region 属性
        #endregion

        #region 传入表达式或代码
        /// <summary>从字符串创建脚本</summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public ScriptEngine CreateScriptSourceFromString(String expression)
        {
            return this;
        }

        /// <summary>从文件创建脚本</summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public ScriptEngine CreateScriptSourceFromFile(String file)
        {
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