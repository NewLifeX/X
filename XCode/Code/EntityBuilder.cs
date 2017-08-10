using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewLife.Log;
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
        public IList<IDataTable> AllTables { get; set; } = new List<IDataTable>();
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public EntityBuilder()
        {
            Usings.Add("XCode");
            Usings.Add("XCode.Configuration");
            Usings.Add("XCode.DataAccessLayer");

            Pure = false;
        }
        #endregion

        #region 静态快速
        /// <summary>为Xml模型文件生成实体类</summary>
        /// <param name="xmlFile">模型文件</param>
        /// <param name="output">输出目录</param>
        /// <param name="nameSpace">命名空间</param>
        /// <param name="connName">连接名</param>
        public static Int32 Build(String xmlFile = null, String output = null, String nameSpace = null, String connName = null)
        {
            if (xmlFile.IsNullOrEmpty())
            {
                var di = ".".AsDirectory();
                XTrace.WriteLine("未指定模型文件，准备从目录中查找第一个xml文件 {0}", di.FullName);
                // 选当前目录第一个
                xmlFile = di.GetFiles("*.xml", SearchOption.TopDirectoryOnly).FirstOrDefault()?.FullName;
            }

            if (xmlFile.IsNullOrEmpty()) throw new Exception("找不到任何模型文件！");

            xmlFile = xmlFile.GetFullPath();
            if (!File.Exists(xmlFile)) throw new FileNotFoundException("指定模型文件不存在！", xmlFile);

            // 导入模型
            var tables = DAL.Import(File.ReadAllText(xmlFile));
            if (tables.Count == 0) return 0;

            // 输出
            if (output.IsNullOrEmpty()) output = Path.GetDirectoryName(xmlFile);

            // 命名空间
            if (nameSpace.IsNullOrEmpty()) nameSpace = Path.GetFileNameWithoutExtension(xmlFile);

            // 连接名
            if (connName.IsNullOrEmpty() && !nameSpace.IsNullOrEmpty() && nameSpace.Contains(".")) connName = nameSpace.Substring(nameSpace.LastIndexOf(".") + 1);

            XTrace.WriteLine("代码生成源：{0}", xmlFile);

            return Build(tables, output, nameSpace, connName);
        }

        /// <summary>为Xml模型文件生成实体类</summary>
        /// <param name="tables">模型文件</param>
        /// <param name="output">输出目录</param>
        /// <param name="nameSpace">命名空间</param>
        /// <param name="connName">连接名</param>
        public static Int32 Build(IList<IDataTable> tables, String output = null, String nameSpace = null, String connName = null)
        {
            if (tables == null || tables.Count == 0) return 0;

            // 连接名
            if (connName.IsNullOrEmpty() && !nameSpace.IsNullOrEmpty() && nameSpace.Contains(".")) connName = nameSpace.Substring(nameSpace.LastIndexOf(".") + 1);

            XTrace.WriteLine("代码生成：{0} 输出：{1} 命名空间：{2} 连接名：{3}", tables.Count, output, nameSpace, connName);

            var count = 0;
            foreach (var item in tables)
            {
                var builder = new EntityBuilder();
                builder.Table = item;
                builder.AllTables = tables;
                builder.GenericType = item.Properties["RenderGenEntity"].ToBoolean();
                builder.Namespace = nameSpace;
                builder.ConnName = connName;

                if (Debug) builder.Log = XTrace.Log;

                builder.Execute();
                builder.Output = output;
                builder.Save();

                builder.Business = true;
                builder.Execute();
                //#if DEBUG
                //builder.Save(".Biz.cs", true);
                //#else
                builder.Save();
                //#endif

                count++;
            }

            return count;
        }
        #endregion

        #region 基础
        /// <summary>执行生成</summary>
        public override void Execute()
        {
            // 增加常用命名空间
            if (Business) AddNameSpace();

            base.Execute();
        }

        /// <summary>实体类头部</summary>
        protected override void BuildClassHeader()
        {
            // 泛型实体类增加默认实例
            if (GenericType && Business)
            {
                WriteLine("/// <summary>{0}</summary>", Table.DisplayName);
                WriteLine("[Serializable]");
                WriteLine("[ModelCheckMode(ModelCheckModes.CheckTableWhenFirstUse)]");
                WriteLine("public class {0} : {0}<{0}> {{ }}", Table.Name);
                WriteLine();
            }

            base.BuildClassHeader();
        }

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

            //return base.GetBaseClass();
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

        /// <summary>增加常用命名空间</summary>
        protected virtual void AddNameSpace()
        {
            var us = Usings;
            if (!Pure && !us.Contains("System.Web"))
            {
                us.Add("System.IO");
                us.Add("System.Linq");
                us.Add("System.Reflection");
                us.Add("System.Text");
                us.Add("System.Threading.Tasks");
                us.Add("System.Web");
                //us.Add("System.Web.Script.Serialization");
                us.Add("System.Xml.Serialization");

                us.Add("NewLife");
                us.Add("NewLife.Data");
                us.Add("NewLife.Model");
                us.Add("NewLife.Log");
                us.Add("NewLife.Reflection");
                us.Add("NewLife.Threading");
                us.Add("NewLife.Web");
                us.Add("XCode.Cache");
            }
        }
        #endregion

        #region 数据类
        /// <summary>实体类头部</summary>
        protected override void BuildAttribute()
        {
            if (Business)
            {
                WriteLine("/// <summary>{0}</summary>", Table.Description);
                return;
            }

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
            WriteLine("[BindColumn(\"{0}\", \"{1}\", \"{2}\"{3})]", dc.ColumnName, dc.Description, dc.RawType, dc.Master ? ", Master = true" : "");

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
                            WriteLine("case __.{0} : _{0} = ({1})Convert.ToInt32(value); break;", dc.Name, dc.DataType.Name);
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

            WriteLine("/// <summary>取得{0}字段信息的快捷方式</summary>", Table.DisplayName);
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

            WriteLine("/// <summary>取得{0}字段名称的快捷方式</summary>", Table.DisplayName);
            WriteLine("public partial class __");
            WriteLine("{");
            var k = Table.Columns.Count;
            foreach (var dc in Table.Columns)
            {
                WriteLine("/// <summary>{0}</summary>", dc.Description);
                WriteLine("public const String {0} = \"{0}\";", dc.Name);
                if (--k > 0) WriteLine();
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
            var k = Table.Columns.Count;
            foreach (var dc in Table.Columns)
            {
                WriteLine("/// <summary>{0}</summary>", dc.Description);
                WriteLine("{0} {1} {{ get; set; }}", dc.DataType.Name, dc.Name);
                if (--k > 0) WriteLine();
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
                    WriteLine("[XmlIgnore]");
                    WriteLine("//[ScriptIgnore]");
                    WriteLine("public {1} {0} {{ get {{ return Extends.Get(nameof({0}), k => {1}.FindBy{3}({2})); }} }}", pname, dt.Name, dc.Name, pk.Name);

                    // 主字段
                    var master = dt.Master ?? dt.GetColumn("Name");
                    if (master != null)
                    {
                        WriteLine();
                        WriteLine("/// <summary>{0}</summary>", dis);
                        WriteLine("[XmlIgnore]");
                        WriteLine("//[ScriptIgnore]");
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

            // 主键
            IDataColumn pk = null;
            if (Table.PrimaryKeys.Length == 1)
            {
                pk = Table.PrimaryKeys[0];
                var name = pk.Name.ToLower();

                WriteLine("/// <summary>根据{0}查找</summary>", pk.DisplayName);
                WriteLine("/// <param name=\"{0}\">{1}</param>", name, pk.DisplayName);
                WriteLine("/// <returns>实体对象</returns>");
                WriteLine("public static {3} FindBy{0}({1} {2})", pk.Name, pk.DataType.Name, name, GenericType ? "TEntity" : Table.Name);
                WriteLine("{");
                {
                    if (pk.DataType.IsInt())
                        WriteLine("if ({0} <= 0) return null;", name);
                    else if (pk.DataType == typeof(String))
                        WriteLine("if ({0}.IsNullOrEmpty()) return null;", name);
                    WriteLine();
                    WriteLine("if (Meta.Count >= 1000)");
                    WriteLine("    return Find(__.{0}, {1});", pk.Name, name);
                    WriteLine("else // 实体缓存");
                    WriteLine("    return Meta.Cache.Entities.Find(__.{0}, {1});", pk.Name, name);

                    WriteLine();
                    WriteLine("// 实体缓存");
                    WriteLine("//return Meta.SingleCache[{0}];", name);
                }
                WriteLine("}");
            }

            // 索引
            foreach (var di in Table.Indexes)
            {
                // 跳过主键
                if (di.Columns.Length == 1 && pk != null && di.Columns[0].EqualIgnoreCase(pk.Name, pk.ColumnName)) continue;

                var cs = Table.GetColumns(di.Columns);
                // 只有整数和字符串能生成查询函数
                if (!cs.All(e => e.DataType.IsInt() || e.DataType == typeof(String))) continue;

                WriteLine();
                WriteLine("/// <summary>根据{0}查找</summary>", cs.Select(e => e.DisplayName).Join("、"));
                foreach (var dc in cs)
                {
                    WriteLine("/// <param name=\"{0}\">{1}</param>", dc.Name.ToLower(), dc.DisplayName);
                }

                var rt = GenericType ? "TEntity" : Table.Name;
                if (!di.Unique) rt = "EntityList<{0}>".F(rt);

                WriteLine("/// <returns>{0}</returns>", di.Unique ? "实体对象" : "实体列表");
                WriteLine("public static {2} FindBy{0}({1})", cs.Select(e => e.Name).Join("And"), cs.Select(e => e.DataType.Name + " " + e.Name.ToLower()).Join(", "), rt);
                WriteLine("{");
                {
                    var act = "Find";
                    if (!di.Unique) act = "FindAll";

                    if (cs.Length == 1)
                    {
                        var dc = cs[0];
                        WriteLine("if (Meta.Count >= 1000)");
                        WriteLine("    return {2}(__.{0}, {1});", dc.Name, dc.Name.ToLower(), act);
                        WriteLine("else // 实体缓存");
                        WriteLine("    return Meta.Cache.Entities.{2}(__.{0}, {1});", dc.Name, dc.Name.ToLower(), act);
                    }
                    else
                    {
                        WriteLine("if (Meta.Count >= 1000)");
                        WriteLine("    return {1}({0});", cs.Select(e => "_.{0} == {1}".F(e.Name, e.Name.ToLower())).Join(" & "), act);
                        WriteLine("else // 实体缓存");
                        WriteLine("    return Meta.Cache.Entities.{1}(e => {0});", cs.Select(e => "e.{0} == {1}".F(e.Name, e.Name.ToLower())).Join(" && "), act);
                    }
                }
                WriteLine("}");
            }

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