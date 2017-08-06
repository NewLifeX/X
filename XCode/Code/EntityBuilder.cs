using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NewLife.Reflection;
using XCode.DataAccessLayer;

namespace XCode.Code
{
    /// <summary>实体类生成器</summary>
    public class EntityBuilder : ClassBuilder
    {
        #region 属性
        /// <summary>连接名</summary>
        public String ConnName { get; set; }

        /// <summary>泛型实体类。泛型参数名TEntity</summary>
        public Boolean GenericType { get; set; }

        /// <summary>业务类。</summary>
        public Boolean Business { get; set; }

        /// <summary>所有表类型名。用于扩展属性</summary>
        public ICollection<IDataTable> AllTables { get; } = new HashSet<IDataTable>();
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public EntityBuilder()
        {
            Usings.Add("NewLife.Model");
            Usings.Add("NewLife.Web");
            Usings.Add("XCode");
            Usings.Add("XCode.Cache");
            Usings.Add("XCode.Configuration");
            Usings.Add("XCode.DataAccessLayer");

            Pure = false;
        }
        #endregion

        #region 基础
        /// <summary>获取类名</summary>
        /// <returns></returns>
        protected override String GetClassName()
        {
            // 类名
            var name = base.GetClassName();
            if (GenericType) name += "<TEntity>";

            return name;
        }

        /// <summary>获取基类</summary>
        /// <returns></returns>
        protected override String GetBaseClass()
        {
            // 数据类的基类只有接口，业务类基类则比较复杂
            if (!Business) return "I" + Table.Name;

            var name = BaseClass;
            if (name.IsNullOrEmpty()) name = "Entity";

            if (GenericType)
                name = "{0}<TEntity> where TEntity : {1}<TEntity>, new()".F(name, Table.Name);
            else
                name = "{0}<{1}>".F(name, Table.Name);

            return name;

            return base.GetBaseClass();
        }

        /// <summary>保存</summary>
        /// <param name="ext"></param>
        /// <param name="overwrite"></param>
        public override void Save(String ext = null, Boolean overwrite = true)
        {
            if (ext.IsNullOrEmpty() && Business)
            {
                ext = ".Biz.cs";
                overwrite = false;
            }

            base.Save(ext, overwrite);
        }

        /// <summary>生成尾部</summary>
        protected override void OnExecuted()
        {
            // 类接口
            WriteLine("}");

            if (!Business)
            {
                WriteLine();
                BuildInterface();
            }

            var ns = Namespace;
            if (!ns.IsNullOrEmpty())
            {
                Writer.Write("}");
            }
        }
        #endregion

        #region 数据类
        /// <summary>实体类头部</summary>
        protected override void BuildAttribute()
        {
            if (Business) return;

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
            if (Business)
                BuildBiz();
            else
            {
                base.BuildItems();

                WriteLine();
                BuildIndex();

                WriteLine();
                BuildFieldName();
            }
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
        #endregion

        #region 业务类
        /// <summary>生成实体类业务部分</summary>
        protected virtual void BuildBiz()
        {
            BuildAction();

            WriteLine();
            BuildExtendProperty();

            WriteLine();
            BuildExtendSearch();

            WriteLine();
            BuildSearch();

            WriteLine();
            BuildBusiness();
        }

        /// <summary>对象操作</summary>
        protected virtual void BuildAction()
        {
            WriteLine("#region 对象操作");

            // 静态构造函数
            BuildCctor();

            // 验证函数
            WriteLine();
            BuildValid();

            // 初始化数据
            WriteLine();
            BuildInitData();

            // 重写添删改
            WriteLine();
            BuildOverride();

            WriteLine("#endregion");
        }

        /// <summary>生成静态构造函数</summary>
        protected virtual void BuildCctor()
        {
            WriteLine("static {0}()", Table.Name);
            WriteLine("{");
            {
                if (GenericType)
                {
                    WriteLine("// 用于引发基类的静态构造函数，所有层次的泛型实体类都应该有一个");
                    WriteLine("var entity = new TEntity();");
                }

                WriteLine();
                WriteLine("// 累加字典");
                WriteLine("//Meta.Factory.AdditionalFields.Add(__.Logins);");

                WriteLine();
                WriteLine("// 过滤器");
                WriteLine("//Meta.Modules.Add<UserModule>();");
                WriteLine("//Meta.Modules.Add<TimeModule>();");
                WriteLine("//Meta.Modules.Add<IPModule>();");
            }
            WriteLine("}");
        }

        /// <summary>数据验证</summary>
        protected virtual void BuildValid()
        {
            WriteLine("/// <summary>验证数据，通过抛出异常的方式提示验证失败。</summary>");
            WriteLine("/// <param name=\"isNew\">是否插入</param>");
            WriteLine("public override void Valid(Boolean isNew)");
            WriteLine("{");
            {
                WriteLine("// 如果没有脏数据，则不需要进行任何处理");
                WriteLine("if (!HasDirty) return;");

                // 非空判断
                var cs = Table.Columns.Where(e => !e.Nullable && e.DataType == typeof(String)).ToArray();
                if (cs.Length > 0)
                {
                    WriteLine();
                    WriteLine("// 这里验证参数范围，建议抛出参数异常，指定参数名，前端用户界面可以捕获参数异常并聚焦到对应的参数输入框");
                    foreach (var item in cs)
                    {
                        WriteLine("if (String.IsNullOrEmpty({0})) throw new ArgumentNullException(nameof({0}), \"{1}不能为空！\");", item.Name, item.DisplayName ?? item.Name);
                    }
                }

                WriteLine();
                WriteLine("// 建议先调用基类方法，基类方法会对唯一索引的数据进行验证");
                WriteLine("base.Valid(isNew);");

                WriteLine();
                WriteLine("// 在新插入数据或者修改了指定字段时进行修正");

                // 货币类型保留小数位数
                cs = Table.Columns.Where(e => e.DataType == typeof(Decimal)).ToArray();
                if (cs.Length > 0)
                {
                    WriteLine("// 货币保留6位小数");
                    foreach (var item in cs)
                    {
                        WriteLine("{0} = Math.Round({0}, 6);", item.Name);
                    }
                }

                // 处理当前已登录用户信息
                cs = Table.Columns.Where(e => e.DataType == typeof(Int32) && e.Name.EqualIgnoreCase("CreateUserID", "UpdateUserID")).ToArray();
                if (cs.Length > 0)
                {
                    WriteLine("// 处理当前已登录用户信息");
                    WriteLine("//var user = ManageProvider.User;");
                    WriteLine("//if (user != null)");
                    WriteLine("{");
                    foreach (var item in cs)
                    {
                        if (item.Name.EqualIgnoreCase("CreateUserID"))
                            WriteLine("//if (isNew && !Dirtys[nameof({0})) {0} = user.ID;", item.Name);
                        else
                            WriteLine("//if (!Dirtys[nameof({0})]) {0} = user.ID;", item.Name);
                    }
                    WriteLine("}");
                }

                var dc = Table.Columns.FirstOrDefault(e => e.Name.EqualIgnoreCase("CreateTime"));
                if (dc != null) WriteLine("//if (isNew && !Dirtys[nameof({0})]) {0} = DateTime.Now;", dc.Name);

                dc = Table.Columns.FirstOrDefault(e => e.Name.EqualIgnoreCase("UpdateTime"));
                if (dc != null) WriteLine("//if (!Dirtys[nameof({0})]) {0} = DateTime.Now;", dc.Name);

                dc = Table.Columns.FirstOrDefault(e => e.Name.EqualIgnoreCase("CreateIP"));
                if (dc != null) WriteLine("//if (isNew && !Dirtys[nameof({0})]) {0} = WebHelper.UserHost;", dc.Name);

                dc = Table.Columns.FirstOrDefault(e => e.Name.EqualIgnoreCase("UpdateIP"));
                if (dc != null) WriteLine("//if (!Dirtys[nameof({0})]) {0} = WebHelper.UserHost;", dc.Name);
            }
            WriteLine("}");
        }

        /// <summary>初始化数据</summary>
        protected virtual void BuildInitData()
        {
            var name = GenericType ? "TEntity" : Table.Name;

            WriteLine("///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>");
            WriteLine("//[EditorBrowsable(EditorBrowsableState.Never)]");
            WriteLine("//protected override void InitData()");
            WriteLine("//{");
            WriteLine("//    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用");
            WriteLine("//    if (Meta.Count > 0) return;");
            WriteLine();
            WriteLine("//    if (XTrace.Debug) XTrace.WriteLine(\"开始初始化{0}[{1}]数据……\");", name, Table.DisplayName);
            WriteLine();
            WriteLine("//    var entity = new {0}();", name);
            foreach (var dc in Table.Columns)
            {
                switch (dc.DataType.GetTypeCode())
                {
                    case TypeCode.Boolean:
                        WriteLine("//    entity.{0} = true;", dc.Name);
                        break;
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        WriteLine("//    entity.{0} = 0;", dc.Name);
                        break;
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        WriteLine("//    entity.{0} = 0.0;", dc.Name);
                        break;
                    case TypeCode.DateTime:
                        WriteLine("//    entity.{0} = DateTime.Now;", dc.Name);
                        break;
                    case TypeCode.String:
                        WriteLine("//    entity.{0} = \"abc\";", dc.Name);
                        break;
                    default:
                        break;
                }
            }
            WriteLine("//    entity.Insert();");
            WriteLine();
            WriteLine("//    if (XTrace.Debug) XTrace.WriteLine(\"完成初始化{0}[{1}]数据！\"", name, Table.DisplayName);
            WriteLine("//}");
        }

        /// <summary>重写添删改</summary>
        protected virtual void BuildOverride()
        {
            WriteLine("///// <summary>已重载。基类先调用Valid(true)验证数据，然后在事务保护内调用OnInsert</summary>");
            WriteLine("///// <returns></returns>");
            WriteLine("//public override Int32 Insert()");
            WriteLine("//{");
            WriteLine("//    return base.Insert();");
            WriteLine("//}");
            WriteLine();
            WriteLine("///// <summary>已重载。在事务保护范围内处理业务，位于Valid之后</summary>");
            WriteLine("///// <returns></returns>");
            WriteLine("//protected override Int32 OnDelete()");
            WriteLine("//{");
            WriteLine("//    return base.OnDelete();");
            WriteLine("//}");
        }

        /// <summary>扩展属性</summary>
        protected virtual void BuildExtendProperty()
        {
            WriteLine("#region 扩展属性");

            foreach (var dc in Table.Columns)
            {
                // 找到名字映射
                var dt = AllTables.FirstOrDefault(e => e.PrimaryKeys.Length == 1 && e.PrimaryKeys[0].DataType == dc.DataType && (e.Name + e.PrimaryKeys[0].Name).EqualIgnoreCase(dc.Name));
                if (dt != null)
                {
                    // 属性名
                    var pname = dt.Name;

                    // 备注
                    var dis = dc.DisplayName;
                    if (dis.IsNullOrEmpty()) dis = dt.DisplayName;

                    var pk = dt.PrimaryKeys[0];

                    WriteLine("/// <summary>{0}</summary>", dis);
                    WriteLine("public {1} {0} {{ get {{ return Extends.Get(nameof({0}), k => {1}.FindBy{3}({2})); }} }}", pname, dt.Name, dc.Name, pk.Name);

                    // 主字段
                    var master = dt.Master ?? dt.GetColumn("Name");
                    if (master != null)
                    {
                        WriteLine();
                        WriteLine("/// <summary>{0}</summary>", dis);
                        if (!dis.IsNullOrEmpty()) WriteLine("[DisplayName(\"{0}\")]", dis);
                        WriteLine("[Map(__.{0}, typeof({1}), \"{2}\")]", dc.Name, dt.Name, pk.Name);
                        if (master.DataType == typeof(String))
                            WriteLine("public {2} {0}{1} {{ get {{ return {0}?.{1}; }} }}", pname, master.Name, master.DataType.Name);
                        else
                            WriteLine("public {2} {0}{1} {{ get {{ return {0} != null ? {0}.{1} : 0; }} }}", pname, master.Name, master.DataType.Name);
                    }
                }
            }

            WriteLine("#endregion");
        }

        /// <summary>扩展查询</summary>
        protected virtual void BuildExtendSearch()
        {
            WriteLine("#region 扩展查询");
            WriteLine("#endregion");
        }

        /// <summary>高级查询</summary>
        protected virtual void BuildSearch()
        {
            WriteLine("#region 高级查询");
            WriteLine("#endregion");
        }

        /// <summary>业务操作</summary>
        protected virtual void BuildBusiness()
        {
            WriteLine("#region 业务操作");
            WriteLine("#endregion");
        }
        #endregion
    }
}