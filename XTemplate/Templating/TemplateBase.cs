using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NewLife;
using NewLife.Reflection;

namespace XTemplate.Templating
{
    /// <summary>模版基类，所有模版继承自该类</summary>
    /// <remarks>模版的原理其实就是生成一个继承自该类的模版类，并重载Render方法</remarks>
    [Serializable]
    public abstract class TemplateBase : DisposeBase
    {
        #region 构造和释放
        ///// <summary>释放</summary>
        ///// <param name="disposing"></param>
        //protected override void OnDispose(bool disposing)
        //{
        //    base.OnDispose(disposing);

        //    if (Output != null) Output = null;
        //}
        #endregion

        #region 缩进
        /// <summary>当前缩进</summary>
        public String CurrentIndent { get; private set; } = "";

        /// <summary>缩进长度集合</summary>
        private List<Int32> indentLengths { get; set; } = new List<Int32>();

        /// <summary>清除缩进</summary>
        public void ClearIndent()
        {
            indentLengths.Clear();
            CurrentIndent = "";
        }

        /// <summary>弹出缩进</summary>
        /// <returns></returns>
        public String RemoveIndent()
        {
            var str = "";
            if (indentLengths.Count > 0)
            {
                var num = indentLengths[indentLengths.Count - 1];
                indentLengths.RemoveAt(indentLengths.Count - 1);
                if (num > 0)
                {
                    str = CurrentIndent.Substring(CurrentIndent.Length - num);
                    CurrentIndent = CurrentIndent.Remove(CurrentIndent.Length - num);
                }
            }
            return str;
        }

        /// <summary>压入缩进</summary>
        /// <param name="indent"></param>
        public void AddIndent(String indent)
        {
            if (indent == null) throw new ArgumentNullException("indent");

            CurrentIndent = CurrentIndent + indent;
            indentLengths.Add(indent.Length);
        }
        #endregion

        #region 输出
        private Boolean endsWithNewline;
        /// <summary>写入文本</summary>
        /// <param name="str"></param>
        public void Write(String str)
        {
            if (String.IsNullOrEmpty(str)) return;

            if ((Output.Length == 0) || endsWithNewline)
            {
                Output.Append(CurrentIndent);
                endsWithNewline = false;
            }
            if (str.EndsWithIgnoreCase(Environment.NewLine)) endsWithNewline = true;
            if (CurrentIndent.Length == 0)
            {
                Output.Append(str);
            }
            else
            {
                str = str.Replace(Environment.NewLine, Environment.NewLine + CurrentIndent);
                if (endsWithNewline)
                    Output.Append(str, 0, str.Length - CurrentIndent.Length);
                else
                    Output.Append(str);
            }
        }

        /// <summary>写入文本</summary>
        /// <param name="obj"></param>
        public void Write(Object obj)
        {
            if (obj == null) return;

            Write(obj.ToString());
        }

        /// <summary>写入文本</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Write(String format, params Object[] args)
        {
            if (String.IsNullOrEmpty(format)) return;

            if (args != null && args.Length > 0)
                Write(String.Format(CultureInfo.CurrentCulture, format, args));
            else
                Write(format);
        }

        /// <summary>写入文本</summary>
        /// <param name="str"></param>
        public void WriteLine(String str)
        {
            Write(str);

            Output.AppendLine();
            endsWithNewline = true;
        }

        /// <summary>写入文本</summary>
        /// <param name="obj"></param>
        public void WriteLine(Object obj)
        {
            if (obj != null) Write(obj.ToString());

            Output.AppendLine();
            endsWithNewline = true;
        }

        /// <summary>写入行</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLine(String format, params Object[] args)
        {
            if (!String.IsNullOrEmpty(format))
            {
                if (args != null && args.Length > 0)
                    Write(String.Format(CultureInfo.CurrentCulture, format, args));
                else
                    Write(format);
            }

            Output.AppendLine();
            endsWithNewline = true;
        }

        /// <summary>写入一个换行</summary>
        public void WriteLine()
        {
            Output.AppendLine();
            endsWithNewline = true;
        }
        #endregion

        #region 属性
        /// <summary>模版引擎实例</summary>
        public Template Template { get; set; }

        /// <summary>模版项实例</summary>
        public TemplateItem TemplateItem { get; set; }
        #endregion

        #region 生成
        /// <summary>初始化</summary>
        public virtual void Initialize() { }

        /// <summary>转换文本</summary>
        /// <returns></returns>
        public virtual String Render() { return Output.ToString(); }

        /// <summary>输出</summary>
        protected StringBuilder Output { get; set; } = new StringBuilder();
        #endregion

        #region 数据属性
        /// <summary>数据</summary>
        public IDictionary<String, Object> Data { get; set; } = new Dictionary<String, Object>();

        /// <summary>获取数据，主要处理数据字典中不存在的元素</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        protected Object GetData(String name)
        {
            Object obj = null;
            return Data.TryGetValue(name, out obj) ? obj : null;
        }

        /// <summary>获取数据，主要处理类型转换</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">名称</param>
        /// <returns></returns>
        protected T GetData<T>(String name)
        {
            var obj = GetData(name);
            if (obj == null) return default(T);

            return obj.ChangeType<T>();
        }
        #endregion

        #region 模版变量
        /// <summary>模版变量集合</summary>
        public IDictionary<String, Type> Vars { get; set; } = new Dictionary<String, Type>();
        #endregion
    }
}