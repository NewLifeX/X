using System;
using System.Linq;
using XCode.DataAccessLayer;

namespace XCode.Code
{
    /// <summary>实体类生成器</summary>
    public class EntityBuilder : ClassBuilder
    {
        #region 属性
        /// <summary>连接名</summary>
        public String ConnName { get; set; }
        #endregion

        /// <summary>实例化</summary>
        public EntityBuilder()
        {
            Usings.Add("XCode");
            Usings.Add("XCode.Configuration");
            Usings.Add("XCode.DataAccessLayer");

            Pure = false;
        }

        /// <summary>执行生成</summary>
        public override void Execute()
        {
            BaseClass = "I" + Table.Name;

            base.Execute();
        }

        /// <summary>实体类头部</summary>
        protected override void BuildAttribute()
        {
            base.BuildAttribute();

            var dt = Table;
            foreach (var item in dt.Indexes)
            {
                WriteLine("[BindIndex(\"{0}\", {1}, \"{2}\")]", item.Name, item.Unique.ToString().ToLower(), item.Columns.Join());
            }

            var cn = dt.Properties["ConnName"];
            if (cn.IsNullOrEmpty()) cn = ConnName;
            WriteLine("[BindTable(\"{0}\", Description = \"{1}\", ConnName = \"{2}\", DbType = DatabaseType.{3})]", dt.TableName, dt.Description, cn, dt.DbType);
        }

        /// <summary>生成每一项</summary>
        protected override void BuildItem(IDataColumn column)
        {
            var dc = column;

            // 字段
            WriteLine("private {0} _{1};", dc.DataType.Name, dc.Name);

            // 注释
            var des = dc.Description;
            WriteLine("/// <summary>{0}</summary>", des);

            if (!Pure)
            {
                var dis = dc.DisplayName;
                if (!dis.IsNullOrEmpty()) WriteLine("[DisplayName(\"{0}\")]", dis);

                if (!des.IsNullOrEmpty()) WriteLine("[Description(\"{0}\")]", des);
            }

            WriteLine("[DataObjectField({0}, {1}, {2}, {3})]", dc.PrimaryKey.ToString().ToLower(), dc.Identity.ToString().ToLower(), dc.Nullable.ToString().ToLower(), dc.Length);
            WriteLine("[BindColumn(\"{0}\", \"{1}\", \"{2}\", {3}, {4})]", dc.ColumnName, dc.DisplayName, dc.RawType, dc.Precision, dc.Scale);

            if (Interface)
                WriteLine("{0} {1} {{ get; set; }}", dc.DataType.Name, dc.Name);
            else
                WriteLine("public {0} {1} {{ get {{ return _{1}; }} set {{ if (OnPropertyChanging(__.{1}, value)) {{ _{1} = value; OnPropertyChanged(__.{1}); }} }} }}", dc.DataType.Name, dc.Name);
        }

        /// <summary>生成主体</summary>
        protected override void BuildItems()
        {
            base.BuildItems();

            WriteLine();
            BuildIndex();

            WriteLine();
            BuildFieldName();
        }

        private void BuildIndex()
        {
            WriteLine("#region 获取/设置 字段值");
            WriteLine("/// <summary>获取/设置 字段值</summary>");
            WriteLine("/// <param name=\"name\">字段名</param>");
            WriteLine("/// <returns></returns>");
            WriteLine("public override Object this[String name]");
            WriteLine("{");

            // get
            {
                WriteLine("get");
                WriteLine("{");
                {
                    WriteLine("switch (name)");
                    WriteLine("{");
                    foreach (var dc in Table.Columns)
                    {
                        WriteLine("case __.{0} : return _{0};", dc.Name);
                    }
                    WriteLine("default: return base[name];");
                    WriteLine("}");
                }
                WriteLine("}");
            }

            // set
            {
                WriteLine("set");
                WriteLine("{");
                {
                    WriteLine("switch (name)");
                    WriteLine("{");
                    var conv = typeof(Convert);
                    foreach (var dc in Table.Columns)
                    {
                        if (conv.GetMethod("To" + dc.DataType.Name, new Type[] { typeof(Object) }) != null)
                            WriteLine("case __.{0} : _{0} = Convert.To{1}(value); break;", dc.Name, dc.DataType.Name);
                        else
                            WriteLine("case __.{0} : _{0} = ({1})value; break;", dc.Name, dc.DataType.Name);
                    }
                    WriteLine("default: base[name] = value; break;");
                    WriteLine("}");
                }
                WriteLine("}");
            }

            WriteLine("}");
            WriteLine("#endregion");
        }

        private void BuildFieldName()
        {
            WriteLine("#region 字段名");

            WriteLine("/// <summary>取得角色字段信息的快捷方式</summary>");
            WriteLine("public partial class _");
            WriteLine("{");
            foreach (var dc in Table.Columns)
            {
                WriteLine("/// <summary>{0}</summary>", dc.Description);
                WriteLine("public static readonly Field {0} = FindByName(__.{0});", dc.Name);
                WriteLine();
            }
            WriteLine("static Field FindByName(String name) { return Meta.Table.FindByName(name); }");
            WriteLine("}");

            WriteLine();

            WriteLine("/// <summary>取得角色字段名称的快捷方式</summary>");
            WriteLine("public partial class __");
            WriteLine("{");
            foreach (var dc in Table.Columns)
            {
                WriteLine("/// <summary>{0}</summary>", dc.Description);
                WriteLine("public const String {0} = \"{0}\";", dc.Name);
                WriteLine();
            }
            WriteLine("}");

            WriteLine("#endregion");
        }

        /// <summary>生成尾部</summary>
        protected override void OnExecuted()
        {
            // 类接口
            WriteLine("}");

            WriteLine();
            BuildInterface();

            var ns = Namespace;
            if (!ns.IsNullOrEmpty())
            {
                Writer.Write("}");
            }
        }

        private void BuildInterface()
        {
            var dt = Table;
            WriteLine("/// <summary>{0}接口</summary>", dt.Description);
            WriteLine("public partial interface I{0}", dt.Name);
            WriteLine("{");

            WriteLine("#region 属性");
            foreach (var dc in Table.Columns)
            {
                WriteLine("/// <summary>{0}</summary>", dc.Description);
                WriteLine("{0} {1} {{ get; set; }}", dc.DataType.Name, dc.Name);
                WriteLine();
            }
            WriteLine("#endregion");

            WriteLine();
            WriteLine("#region 获取/设置 字段值");
            WriteLine("/// <summary>获取/设置 字段值</summary>");
            WriteLine("/// <param name=\"name\">字段名</param>");
            WriteLine("/// <returns></returns>");
            WriteLine("Object this[String name] { get; set; }");
            WriteLine("#endregion");

            WriteLine("}");
        }
    }
}