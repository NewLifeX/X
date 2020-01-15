using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewLife.Log;
using XCode.DataAccessLayer;

namespace XCode.Code
{
    /// <summary>类代码生成器</summary>
    public class ClassBuilder
    {
        #region 属性
        /// <summary>写入器</summary>
        public TextWriter Writer { get; set; }

        /// <summary>数据表</summary>
        public IDataTable Table { get; set; }

        /// <summary>命名空间</summary>
        public String Namespace { get; set; }

        /// <summary>引用命名空间</summary>
        public HashSet<String> Usings { get; } = new HashSet<String>(StringComparer.OrdinalIgnoreCase);

        /// <summary>纯净类</summary>
        public Boolean Pure { get; set; }

        /// <summary>生成接口</summary>
        public Boolean Interface { get; set; }

        /// <summary>基类</summary>
        public String BaseClass { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public ClassBuilder()
        {
            Namespace = GetType().Namespace;

            Usings.Add("System");
            Usings.Add("System.Collections.Generic");
            Usings.Add("System.ComponentModel");
        }
        #endregion

        #region 主方法
        /// <summary>执行生成</summary>
        public virtual void Execute()
        {
            WriteLog("生成 {0} {1}", Table.Name, Table.DisplayName);

            Clear();
            if (Writer == null) Writer = new StringWriter();

            OnExecuting();

            BuildItems();

            OnExecuted();
        }

        /// <summary>生成头部</summary>
        protected virtual void OnExecuting()
        {
            // 引用命名空间
            var us = Usings.OrderBy(e => e.StartsWith("System") ? 0 : 1).ThenBy(e => e).ToArray();
            foreach (var item in us)
            {
                WriteLine("using {0};", item);
            }
            WriteLine();

            var ns = Namespace;
            if (!ns.IsNullOrEmpty())
            {
                WriteLine("namespace {0}", ns);
                WriteLine("{");
            }

            BuildClassHeader();
        }

        /// <summary>实体类头部</summary>
        protected virtual void BuildClassHeader()
        {
            // 头部
            BuildAttribute();

            // 类名和基类
            var cn = GetClassName();
            var bc = GetBaseClass();
            if (!bc.IsNullOrEmpty()) bc = " : " + bc;

            // 类接口
            if (Interface)
                WriteLine("public interface {0}{1}", cn, bc);
            else
                WriteLine("public partial class {0}{1}", cn, bc);
            WriteLine("{");
        }

        /// <summary>获取类名</summary>
        /// <returns></returns>
        protected virtual String GetClassName()
        {
            var name = Table.Name;
            if (Interface) name = "I" + name;

            return name;
        }

        /// <summary>获取基类</summary>
        /// <returns></returns>
        protected virtual String GetBaseClass() { return BaseClass; }

        /// <summary>实体类头部</summary>
        protected virtual void BuildAttribute()
        {
            // 注释
            var des = Table.Description;
            WriteLine("/// <summary>{0}</summary>", des);

            if (!Pure)
            {
                WriteLine("[Serializable]");
                WriteLine("[DataObject]");

                if (!des.IsNullOrEmpty()) WriteLine("[Description(\"{0}\")]", des);
            }
        }

        /// <summary>生成尾部</summary>
        protected virtual void OnExecuted()
        {
            // 类接口
            WriteLine("}");

            var ns = Namespace;
            if (!ns.IsNullOrEmpty())
            {
                Writer.Write("}");
            }
        }

        /// <summary>生成主体</summary>
        protected virtual void BuildItems()
        {
            WriteLine("#region 属性");
            for (var i = 0; i < Table.Columns.Count; i++)
            {
                if (i > 0) WriteLine();
                BuildItem(Table.Columns[i]);
            }
            WriteLine("#endregion");
        }

        /// <summary>生成每一项</summary>
        protected virtual void BuildItem(IDataColumn column)
        {
            var dc = column;
            //BuildItemAttribute(column);
            // 注释
            var des = dc.Description;
            WriteLine("/// <summary>{0}</summary>", des);

            if (!Pure)
            {
                if (!des.IsNullOrEmpty()) WriteLine("[Description(\"{0}\")]", des);

                var dis = dc.DisplayName;
                if (!dis.IsNullOrEmpty()) WriteLine("[DisplayName(\"{0}\")]", dis);
            }

            var type = dc.Properties["Type"];
            if (type.IsNullOrEmpty()) type = dc.DataType?.Name;

            if (Interface)
                WriteLine("{0} {1} {{ get; set; }}", type, dc.Name);
            else
                WriteLine("public {0} {1} {{ get; set; }}", type, dc.Name);
        }

        ///// <summary>属性头部特性</summary>
        //protected virtual void BuildItemAttribute(IDataColumn column)
        //{
        //    // 注释
        //    var des = column.Description;
        //    WriteLine("/// <summary>{0}</summary>", des);
        //    if (!des.IsNullOrEmpty()) WriteLine("[Description(\"{0}\")]", des);

        //    WriteLine("[Serializable]");
        //    WriteLine("[DataObject]");
        //}
        #endregion

        #region 写入缩进方法
        private String _Indent;

        /// <summary>设置缩进</summary>
        /// <param name="add"></param>
        protected virtual void SetIndent(Boolean add)
        {
            if (add)
                _Indent += "    ";
            else if (!_Indent.IsNullOrEmpty())
                _Indent = _Indent.Substring(0, _Indent.Length - 4);
        }

        /// <summary>写入</summary>
        /// <param name="value"></param>
        protected virtual void WriteLine(String value = null)
        {
            if (value.IsNullOrEmpty())
            {
                Writer.WriteLine();
                return;
            }

            if (value[0] == '}') SetIndent(false);

            var v = value;
            if (!_Indent.IsNullOrEmpty()) v = _Indent + v;

            Writer.WriteLine(v);

            if (value == "{") SetIndent(true);
        }

        /// <summary>写入</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected virtual void WriteLine(String format, params Object[] args)
        {
            if (!_Indent.IsNullOrEmpty()) format = _Indent + format;

            Writer.WriteLine(format, args);
        }

        /// <summary>清空，重新生成</summary>
        public void Clear()
        {
            _Indent = null;

            if (Writer is StringWriter sw)
            {
                sw.GetStringBuilder().Clear();
            }
        }

        /// <summary>输出结果</summary>
        /// <returns></returns>
        public override String ToString()
        {
            return Writer.ToString();
        }
        #endregion

        #region 保存
        /// <summary>输出目录</summary>
        public String Output { get; set; }

        /// <summary>保存文件，返回文件路径</summary>
        public virtual String Save(String ext = null, Boolean overwrite = true, Boolean chineseFileName = true)
        {
            var p = Output;
            //if (Table.Properties.ContainsKey("Output")) p = p.CombinePath(Table.Properties["Output"]);
            //if (Table.Properties.ContainsKey("分类")) p = p.CombinePath(Table.Properties["分类"]);

            if (ext.IsNullOrEmpty()) ext = ".cs";

            if (Interface)
                p = p.CombinePath("I" + Table.Name + ext);
            else if (chineseFileName && !Table.DisplayName.IsNullOrEmpty())
                p = p.CombinePath(Table.DisplayName + ext);
            else
                p = p.CombinePath(Table.Name + ext);

            p = p.GetBasePath();

            if (!File.Exists(p) || overwrite) File.WriteAllText(p.EnsureDirectory(true), ToString());

            return p;
        }
        #endregion

        #region 辅助
        /// <summary>C#版本</summary>
        public Version CSharp { get; set; }

        /// <summary>nameof</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected String NameOf(String name)
        {
            var v = CSharp;
            if (v == null || v.Major == 0 || v.Major > 5) return "nameof({0})".F(name);

            return "\"" + name + "\"";
        }

        /// <summary>是否调试</summary>
        public static Boolean Debug { get; set; }

        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            Log?.Info(format, args);
        }
        #endregion
    }
}