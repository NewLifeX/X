using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewLife;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Reflection;
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

        /// <summary>类名。默认Table.Name</summary>
        public String ClassName { get; set; }

        /// <summary>生成器选项</summary>
        public BuilderOption Option { get; set; } = new BuilderOption();
        #endregion

        #region 静态快速
        /// <summary>加载模型文件</summary>
        /// <param name="xmlFile">Xml模型文件</param>
        /// <param name="option">生成可选项</param>
        /// <param name="atts">扩展属性字典</param>
        /// <returns></returns>
        public static IList<IDataTable> LoadModels(String xmlFile, BuilderOption option, out IDictionary<String, String> atts)
        {
            if (xmlFile.IsNullOrEmpty())
            {
                var di = ".".GetBasePath().AsDirectory();
                //XTrace.WriteLine("未指定模型文件，准备从目录中查找第一个xml文件 {0}", di.FullName);
                // 选当前目录第一个
                xmlFile = di.GetFiles("*.xml", SearchOption.TopDirectoryOnly).FirstOrDefault()?.FullName;
            }

            if (xmlFile.IsNullOrEmpty()) throw new Exception("找不到任何模型文件！");

            xmlFile = xmlFile.GetBasePath();
            if (!File.Exists(xmlFile)) throw new FileNotFoundException("指定模型文件不存在！", xmlFile);

            // 导入模型
            var xmlContent = File.ReadAllText(xmlFile);
            atts = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase)
            {
                ["xmlns"] = "http://www.newlifex.com/Model2020.xsd",
                ["xmlns:xs"] = "http://www.w3.org/2001/XMLSchema-instance",
                ["xs:schemaLocation"] = "http://www.newlifex.com http://www.newlifex.com/Model2020.xsd"
            };

            // 导入模型
            var tables = ModelHelper.FromXml(xmlContent, DAL.CreateTable, atts);

            if (option != null)
            {
                option.Output = atts["Output"] ?? Path.GetDirectoryName(xmlFile);
                option.Namespace = atts["NameSpace"] ?? Path.GetFileNameWithoutExtension(xmlFile);
                option.ConnName = atts["ConnName"];
                option.BaseClass = atts["BaseClass"];
            }

            // 保存文件名
            atts["ModelFile"] = xmlFile;

            return tables;
        }

        /// <summary>生成简易版模型</summary>
        /// <param name="tables">表集合</param>
        /// <param name="option">可选项</param>
        /// <returns></returns>
        public static Int32 BuildModels(IList<IDataTable> tables, BuilderOption option = null)
        {
            if (option == null)
                option = new BuilderOption();
            else
                option = option.Clone();

            option.Pure = true;

            var count = 0;
            foreach (var item in tables)
            {
                // 跳过排除项
                if (option.Excludes.Contains(item.Name)) continue;
                if (option.Excludes.Contains(item.TableName)) continue;

                var builder = new ClassBuilder
                {
                    Table = item,
                    Option = option.Clone(),
                };
                builder.Execute();
                builder.Save(null, true, false);

                count++;
            }

            return count;
        }

        /// <summary>生成简易版实体接口</summary>
        /// <param name="tables">表集合</param>
        /// <param name="option">可选项</param>
        /// <returns></returns>
        public static Int32 BuildInterfaces(IList<IDataTable> tables, BuilderOption option = null)
        {
            if (option == null)
                option = new BuilderOption();
            else
                option = option.Clone();

            option.Interface = true;

            var count = 0;
            foreach (var item in tables)
            {
                // 跳过排除项
                if (option.Excludes.Contains(item.Name)) continue;
                if (option.Excludes.Contains(item.TableName)) continue;

                var builder = new ClassBuilder
                {
                    Table = item,
                    Option = option.Clone(),
                };
                builder.Execute();
                builder.Save(null, true, false);

                count++;
            }

            return count;
        }
        #endregion

        #region 主方法
        /// <summary>执行生成</summary>
        public virtual void Execute()
        {
            if (ClassName.IsNullOrEmpty())
            {
                if (!Option.ClassNameTemplate.IsNullOrEmpty())
                    ClassName = Option.ClassNameTemplate.Replace("{name}", Table.Name);
                else
                    ClassName = Option.Interface ? ("I" + Table.Name) : Table.Name;
            }
            //WriteLog("生成 {0} {1}", Table.Name, Table.DisplayName);

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
            var us = Option.Usings;
            if (Option.Extend && !us.Contains("NewLife.Data")) us.Add("NewLife.Data");

            us = us.Distinct().OrderBy(e => e.StartsWith("System") ? 0 : 1).ThenBy(e => e).ToArray();
            foreach (var item in us)
            {
                WriteLine("using {0};", item);
            }
            WriteLine();

            var ns = Option.Namespace;
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

            // 基类
            var baseClass = GetBaseClass();
            if (!baseClass.IsNullOrEmpty()) baseClass = " : " + baseClass;

            // 分部类
            var partialClass = Option.Partial ? " partial" : "";

            // 类接口
            if (Option.Interface)
                WriteLine("public{2} interface {0}{1}", ClassName, baseClass, partialClass);
            else
                WriteLine("public{2} class {0}{1}", ClassName, baseClass, partialClass);
            WriteLine("{");
        }

        /// <summary>获取基类</summary>
        /// <returns></returns>
        protected virtual String GetBaseClass()
        {
            var baseClass = Option.BaseClass?.Replace("{name}", Table.Name);
            if (Option.Extend)
            {
                if (!baseClass.IsNullOrEmpty()) baseClass += ", ";
                baseClass += "IExtend";
            }

            return baseClass;
        }

        /// <summary>实体类头部</summary>
        protected virtual void BuildAttribute()
        {
            // 注释
            var des = Table.Description;
            if (!Option.DisplayNameTemplate.IsNullOrEmpty())
            {
                des = Table.Description.TrimStart(Table.DisplayName, "。");
                des = Option.DisplayNameTemplate.Replace("{displayName}", Table.DisplayName) + "。" + des;
            }
            WriteLine("/// <summary>{0}</summary>", des);

            if (!Option.Pure && !Option.Interface)
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

            if (!Option.Namespace.IsNullOrEmpty())
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
                var column = Table.Columns[i];

                // 跳过排除项
                if (Option.Excludes.Contains(column.Name)) continue;
                if (Option.Excludes.Contains(column.ColumnName)) continue;

                if (i > 0) WriteLine();
                BuildItem(column);
            }
            WriteLine("#endregion");

            if (Option.Extend)
            {
                WriteLine();
                BuildExtend();
            }

            // 生成拷贝函数。需要有基类
            //var bs = Option.BaseClass.Split(",").Select(e => e.Trim()).ToArray();
            //var model = bs.FirstOrDefault(e => e[0] == 'I' && e.Contains("{name}"));
            var model = Option.ModelNameForCopy;
            if (!Option.Interface && !model.IsNullOrEmpty())
            {
                WriteLine();
                BuildCopy(model.Replace("{name}", Table.Name));
            }
        }

        /// <summary>生成每一项</summary>
        protected virtual void BuildItem(IDataColumn column)
        {
            var dc = column;
            //BuildItemAttribute(column);
            // 注释
            var des = dc.Description;
            WriteLine("/// <summary>{0}</summary>", des);

            if (!Option.Pure && !Option.Interface)
            {
                if (!des.IsNullOrEmpty()) WriteLine("[Description(\"{0}\")]", des);

                var dis = dc.DisplayName;
                if (!dis.IsNullOrEmpty()) WriteLine("[DisplayName(\"{0}\")]", dis);
            }

            var type = dc.Properties["Type"];
            if (type.IsNullOrEmpty()) type = dc.DataType?.Name;

            if (Option.Interface)
                WriteLine("{0} {1} {{ get; set; }}", type, dc.Name);
            else
                WriteLine("public {0} {1} {{ get; set; }}", type, dc.Name);
        }

        private void BuildExtend()
        {
            WriteLine("#region 获取/设置 字段值");
            WriteLine("/// <summary>获取/设置 字段值</summary>");
            WriteLine("/// <param name=\"name\">字段名</param>");
            WriteLine("/// <returns></returns>");
            WriteLine("public virtual Object this[String name]");
            WriteLine("{");

            // get
            WriteLine("get");
            WriteLine("{");
            {
                WriteLine("switch (name)");
                WriteLine("{");
                foreach (var column in Table.Columns)
                {
                    // 跳过排除项
                    if (Option.Excludes.Contains(column.Name)) continue;
                    if (Option.Excludes.Contains(column.ColumnName)) continue;

                    WriteLine("case \"{0}\": return {0};", column.Name);
                }
                WriteLine("default: throw new KeyNotFoundException($\"{name} not found\");");
                WriteLine("}");
            }
            WriteLine("}");

            // set
            WriteLine("set");
            WriteLine("{");
            {
                WriteLine("switch (name)");
                WriteLine("{");
                var conv = typeof(Convert);
                foreach (var column in Table.Columns)
                {
                    // 跳过排除项
                    if (Option.Excludes.Contains(column.Name)) continue;
                    if (Option.Excludes.Contains(column.ColumnName)) continue;

                    var type = column.Properties["Type"];
                    if (type.IsNullOrEmpty()) type = column.DataType?.Name;

                    if (!type.IsNullOrEmpty())
                    {
                        if (!type.Contains("."))
                        {

                        }
                        if (!type.Contains(".") && conv.GetMethod("To" + type, new Type[] { typeof(Object) }) != null)
                        {
                            switch (type)
                            {
                                case "Int32":
                                    WriteLine("case \"{0}\": {0} = value.ToInt(); break;", column.Name);
                                    break;
                                case "Int64":
                                    WriteLine("case \"{0}\": {0} = value.ToLong(); break;", column.Name);
                                    break;
                                case "Double":
                                    WriteLine("case \"{0}\": {0} = value.ToDouble(); break;", column.Name);
                                    break;
                                case "Boolean":
                                    WriteLine("case \"{0}\": {0} = value.ToBoolean(); break;", column.Name);
                                    break;
                                case "DateTime":
                                    WriteLine("case \"{0}\": {0} = value.ToDateTime(); break;", column.Name);
                                    break;
                                default:
                                    WriteLine("case \"{0}\": {0} = Convert.To{1}(value); break;", column.Name, type);
                                    break;
                            }
                        }
                        else
                        {
                            try
                            {
                                // 特殊支持枚举
                                var type2 = type.GetTypeEx(false);
                                if (type2 != null && type2.IsEnum)
                                    WriteLine("case \"{0}\": {0} = ({1})value.ToInt(); break;", column.Name, type);
                                else
                                    WriteLine("case \"{0}\": {0} = ({1})value; break;", column.Name, type);
                            }
                            catch (Exception ex)
                            {
                                XTrace.WriteException(ex);
                                WriteLine("case \"{0}\": {0} = ({1})value; break;", column.Name, type);
                            }
                        }
                    }
                }
                WriteLine("default: throw new KeyNotFoundException($\"{name} not found\");");
                WriteLine("}");
            }
            WriteLine("}");

            WriteLine("}");
            WriteLine("#endregion");
        }

        /// <summary>生成拷贝函数</summary>
        /// <param name="model">模型类</param>
        protected virtual void BuildCopy(String model)
        {
            WriteLine("#region 拷贝");
            WriteLine("/// <summary>拷贝模型对象</summary>");
            WriteLine("/// <param name=\"model\">模型</param>");
            WriteLine("public void Copy({0} model)", model);
            WriteLine("{");
            foreach (var column in Table.Columns)
            {
                // 跳过排除项
                if (Option.Excludes.Contains(column.Name)) continue;
                if (Option.Excludes.Contains(column.ColumnName)) continue;

                WriteLine("{0} = model.{0};", column.Name);
            }
            WriteLine("}");
            WriteLine("#endregion");
        }
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
        public override String ToString() => Writer.ToString();
        #endregion

        #region 保存
        /// <summary>保存文件，返回文件路径</summary>
        public virtual String Save(String ext = null, Boolean overwrite = true, Boolean chineseFileName = true)
        {
            var p = Option.Output;

            if (ext.IsNullOrEmpty())
                ext = ".cs";
            else if (!ext.Contains("."))
                ext += ".cs";

            if (Option.Interface)
                p = p.CombinePath(ClassName + ext);
            else if (chineseFileName && !Table.DisplayName.IsNullOrEmpty())
                p = p.CombinePath(Table.DisplayName + ext);
            else
                p = p.CombinePath(ClassName + ext);

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
            if (v == null || v.Major == 0 || v.Major > 5) return $"nameof({name})";

            return "\"" + name + "\"";
        }

        /// <summary>驼峰命名，首字母小写</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected static String GetCamelCase(String name)
        {
            if (name.EqualIgnoreCase("id")) return "id";

            return Char.ToLower(name[0]) + name.Substring(1);
        }

        /// <summary>是否调试</summary>
        public static Boolean Debug { get; set; }

        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
        #endregion
    }
}