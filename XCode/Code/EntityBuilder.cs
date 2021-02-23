using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NewLife;
using NewLife.Log;
using NewLife.Reflection;
using XCode.DataAccessLayer;

namespace XCode.Code
{
    /// <summary>实体类生成器</summary>
    public class EntityBuilder : ClassBuilder
    {
        #region 属性
        /// <summary>业务类。</summary>
        public Boolean Business { get; set; }

        /// <summary>所有表类型名。用于扩展属性</summary>
        public IList<IDataTable> AllTables { get; set; } = new List<IDataTable>();
        #endregion

        #region 静态快速
        /// <summary>为Xml模型文件生成实体类</summary>
        /// <param name="xmlFile">模型文件</param>
        /// <param name="output">输出目录</param>
        /// <param name="nameSpace">命名空间</param>
        /// <param name="connName">连接名</param>
        /// <param name="chineseFileName">中文文件名</param>
        [Obsolete("=>BuildTables")]
        public static Int32 Build(String xmlFile = null, String output = null, String nameSpace = null, String connName = null, Boolean? chineseFileName = null)
        {
            var option = new BuilderOption
            {
                Output = output,
                Namespace = nameSpace,
                ConnName = connName,
                Partial = true,
            };

            var tables = ClassBuilder.LoadModels(xmlFile, option, out var atts);
            FixModelFile(xmlFile, option, atts, tables);

            return BuildTables(tables, option, chineseFileName ?? true);
        }

        /// <summary>修正模型文件</summary>
        /// <param name="xmlFile"></param>
        /// <param name="option"></param>
        /// <param name="atts"></param>
        /// <param name="tables"></param>
        public static void FixModelFile(String xmlFile, BuilderOption option, IDictionary<String, String> atts, IList<IDataTable> tables)
        {
            // 保存文件名
            if (xmlFile.IsNullOrEmpty()) xmlFile = atts["ModelFile"];

            // 反哺。确保输出空特性
            atts["Output"] = option.Output + "";
            atts["NameSpace"] = option.Namespace + "";
            atts["ConnName"] = option.ConnName + "";
            atts["BaseClass"] = option.BaseClass + "";
            atts.Remove("NameIgnoreCase");
            atts.Remove("IgnoreNameCase");
            atts.Remove("ChineseFileName");
            atts.Remove("ModelFile");

            // 更新xsd
            atts["xmlns"] = atts["xmlns"].Replace("ModelSchema", "Model2020");
            atts["xs:schemaLocation"] = atts["xs:schemaLocation"].Replace("ModelSchema", "Model2020");

            // 保存模型文件
            var xmlContent = File.ReadAllText(xmlFile);
            var xml2 = ModelHelper.ToXml(tables, atts);
            if (xmlContent != xml2) File.WriteAllText(xmlFile, xml2);
        }

        /// <summary>为Xml模型文件生成实体类</summary>
        /// <param name="tables">模型文件</param>
        /// <param name="option">生成可选项</param>
        /// <param name="chineseFileName">是否中文名称</param>
        public static Int32 BuildTables(IList<IDataTable> tables, BuilderOption option, Boolean chineseFileName = true)
        {
            if (tables == null || tables.Count == 0) return 0;

            if (option == null)
                option = new BuilderOption();
            else
                option = option.Clone();
            option.Partial = true;

            var count = 0;
            foreach (var item in tables)
            {
                // 跳过排除项
                if (option.Excludes.Contains(item.Name)) continue;
                if (option.Excludes.Contains(item.TableName)) continue;

                var builder = new EntityBuilder
                {
                    AllTables = tables,
                    Option = option.Clone(),
                };

                builder.Load(item);

                builder.Execute();
                builder.Save(null, true, chineseFileName);

                builder.Business = true;
                builder.Execute();
                builder.Save(null, false, chineseFileName);

                count++;
            }

            return count;
        }
        #endregion

        #region 方法
        /// <summary>加载数据表</summary>
        /// <param name="table"></param>
        public void Load(IDataTable table)
        {
            Table = table;

            var option = Option;

            // 命名空间
            var str = table.Properties["Namespace"];
            if (!str.IsNullOrEmpty()) option.Namespace = str;

            // 连接名
            var connName = table.ConnName;
            if (!connName.IsNullOrEmpty()) option.ConnName = connName;

            // 基类
            str = table.Properties["BaseClass"];
            if (!str.IsNullOrEmpty()) option.BaseClass = str;

            // 输出目录
            str = table.Properties["Output"];
            if (!str.IsNullOrEmpty()) option.Output = str.GetBasePath();
        }
        #endregion

        #region 基础
        /// <summary>执行生成</summary>
        protected override void OnExecuting()
        {
            // 增加常用命名空间
            AddNameSpace();

            base.OnExecuting();
        }

        /// <summary>增加常用命名空间</summary>
        protected virtual void AddNameSpace()
        {
            var us = Option.Usings;

            us.Add("XCode");
            us.Add("XCode.Configuration");
            us.Add("XCode.DataAccessLayer");

            if (Business && !Option.Pure)
            {
                us.Add("System.IO");
                us.Add("System.Linq");
                us.Add("System.Reflection");
                us.Add("System.Text");
                us.Add("System.Threading.Tasks");
                us.Add("System.Web");
                us.Add("System.Web.Script.Serialization");
                us.Add("System.Xml.Serialization");
                us.Add("System.Runtime.Serialization");

                us.Add("NewLife");
                us.Add("NewLife.Data");
                us.Add("NewLife.Model");
                us.Add("NewLife.Log");
                us.Add("NewLife.Reflection");
                us.Add("NewLife.Threading");
                us.Add("NewLife.Web");
                us.Add("XCode.Cache");
                us.Add("XCode.Membership");
            }
        }

        /// <summary>获取基类</summary>
        /// <returns></returns>
        protected override String GetBaseClass()
        {
            var baseClass = Option.BaseClass;
            if (Option.Extend)
            {
                if (!baseClass.IsNullOrEmpty()) baseClass += ", ";
                baseClass += "IExtend";
            }

            var bs = baseClass?.Split(",").Select(e => e.Trim()).ToArray();

            // 数据类的基类只有接口，业务类基类则比较复杂
            var name = "";
            if (Business)
            {
                // 数据类只要实体基类
                name = bs?.FirstOrDefault(e => e.Contains("Entity"));
                if (name.IsNullOrEmpty()) name = "Entity";

                name = $"{name}<{ClassName}>";
            }
            else
            {
                // 数据类不要实体基类
                name = bs?.Where(e => !e.Contains("Entity")).Join(", ");
            }

            return name?.Replace("{name}", ClassName);
        }

        /// <summary>保存</summary>
        /// <param name="ext"></param>
        /// <param name="overwrite"></param>
        /// <param name="chineseFileName"></param>
        public override String Save(String ext = null, Boolean overwrite = true, Boolean chineseFileName = true)
        {
            if (ext.IsNullOrEmpty() && Business)
            {
                ext = ".Biz.cs";
                //overwrite = false;
            }

            return base.Save(ext, overwrite, chineseFileName);
        }

        /// <summary>生成尾部</summary>
        protected override void OnExecuted()
        {
            // 类接口
            WriteLine("}");

            //if (!Business)
            //{
            //    WriteLine();
            //    BuildInterface();
            //}

            if (!Option.Namespace.IsNullOrEmpty())
            {
                Writer.Write("}");
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
            if (cn.IsNullOrEmpty()) cn = Option.ConnName;
            WriteLine("[BindTable(\"{0}\", Description = \"{1}\", ConnName = \"{2}\", DbType = DatabaseType.{3})]", dt.TableName, dt.Description, cn, dt.DbType);
        }

        /// <summary>生成每一项</summary>
        protected override void BuildItem(IDataColumn column)
        {
            var dc = column;

            var type = dc.Properties["Type"];
            if (type.IsNullOrEmpty()) type = dc.DataType?.Name;

            // 字段
            WriteLine("private {0} _{1};", type, dc.Name);

            // 注释
            var des = dc.Description;
            WriteLine("/// <summary>{0}</summary>", des);

            // 附加特性
            if (dc.Properties.TryGetValue("Attribute", out var att))
                WriteLine("[{0}]", att.Replace("{name}", dc.Name));

            if (!Option.Pure)
            {
                var dis = dc.DisplayName;
                if (!dis.IsNullOrEmpty()) WriteLine("[DisplayName(\"{0}\")]", dis);

                if (!des.IsNullOrEmpty()) WriteLine("[Description(\"{0}\")]", des);
            }

            WriteLine("[DataObjectField({0}, {1}, {2}, {3})]", dc.PrimaryKey.ToString().ToLower(), dc.Identity.ToString().ToLower(), dc.Nullable.ToString().ToLower(), dc.Length);

            // 支持生成带精度的特性
            if (!dc.ItemType.IsNullOrEmpty())
                WriteLine("[BindColumn(\"{0}\", \"{1}\", \"{2}\", ItemType = \"{3}\")]", dc.ColumnName, dc.Description, dc.RawType, dc.ItemType);
            else if (dc.Precision > 0 || dc.Scale > 0)
                WriteLine("[BindColumn(\"{0}\", \"{1}\", \"{2}\", Precision = {3}, Scale = {4})]", dc.ColumnName, dc.Description, dc.RawType, dc.Precision, dc.Scale);
            else
                WriteLine("[BindColumn(\"{0}\", \"{1}\", \"{2}\"{3})]", dc.ColumnName, dc.Description, dc.RawType, dc.Master ? ", Master = true" : "");

            if (Option.Interface)
                WriteLine("{0} {1} {{ get; set; }}", type, dc.Name);
            else
                WriteLine("public {0} {1} {{ get => _{1}; set {{ if (OnPropertyChanging(\"{1}\", value)) {{ _{1} = value; OnPropertyChanged(\"{1}\"); }} }} }}", type, dc.Name);
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
                BuildExtend();

                WriteLine();
                BuildFieldName();
            }
        }

        private void BuildExtend()
        {
            WriteLine("#region 获取/设置 字段值");
            WriteLine("/// <summary>获取/设置 字段值</summary>");
            WriteLine("/// <param name=\"name\">字段名</param>");
            WriteLine("/// <returns></returns>");
            WriteLine("public override Object this[String name]");
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

                    WriteLine("case \"{0}\": return _{0};", column.Name);
                }
                WriteLine("default: return base[name];");
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
                        if (!type.Contains(".") && conv.GetMethod("To" + type, new Type[] { typeof(Object) }) != null)
                        {
                            switch (type)
                            {
                                case "Int32":
                                    WriteLine("case \"{0}\": _{0} = value.ToInt(); break;", column.Name);
                                    break;
                                case "Int64":
                                    WriteLine("case \"{0}\": _{0} = value.ToLong(); break;", column.Name);
                                    break;
                                case "Double":
                                    WriteLine("case \"{0}\": _{0} = value.ToDouble(); break;", column.Name);
                                    break;
                                case "Boolean":
                                    WriteLine("case \"{0}\": _{0} = value.ToBoolean(); break;", column.Name);
                                    break;
                                case "DateTime":
                                    WriteLine("case \"{0}\": _{0} = value.ToDateTime(); break;", column.Name);
                                    break;
                                default:
                                    WriteLine("case \"{0}\": _{0} = Convert.To{1}(value); break;", column.Name, type);
                                    break;
                            }
                        }
                        else
                        {
                            try
                            {
                                // 特殊支持枚举
                                if (column.DataType.IsInt())
                                    WriteLine("case \"{0}\": _{0} = ({1})value.ToInt(); break;", column.Name, type);
                                else
                                    WriteLine("case \"{0}\": _{0} = ({1})value; break;", column.Name, type);
                            }
                            catch (Exception ex)
                            {
                                XTrace.WriteException(ex);
                                WriteLine("case \"{0}\": _{0} = ({1})value; break;", column.Name, type);
                            }
                        }
                    }
                }
                WriteLine("default: base[name] = value; break;");
                WriteLine("}");
            }
            WriteLine("}");

            WriteLine("}");
            WriteLine("#endregion");
        }

        private void BuildFieldName()
        {
            WriteLine("#region 字段名");

            WriteLine("/// <summary>取得{0}字段信息的快捷方式</summary>", Table.DisplayName);
            WriteLine("public partial class _");
            WriteLine("{");
            foreach (var column in Table.Columns)
            {
                // 跳过排除项
                if (Option.Excludes.Contains(column.Name)) continue;
                if (Option.Excludes.Contains(column.ColumnName)) continue;

                WriteLine("/// <summary>{0}</summary>", column.Description);
                WriteLine("public static readonly Field {0} = FindByName(\"{0}\");", column.Name);
                WriteLine();
            }
            WriteLine("static Field FindByName(String name) => Meta.Table.FindByName(name);");
            WriteLine("}");

            WriteLine();

            WriteLine("/// <summary>取得{0}字段名称的快捷方式</summary>", Table.DisplayName);
            WriteLine("public partial class __");
            WriteLine("{");
            var k = Table.Columns.Count;
            foreach (var column in Table.Columns)
            {
                // 跳过排除项
                if (Option.Excludes.Contains(column.Name)) continue;
                if (Option.Excludes.Contains(column.ColumnName)) continue;

                WriteLine("/// <summary>{0}</summary>", column.Description);
                WriteLine("public const String {0} = \"{0}\";", column.Name);
                if (--k > 0) WriteLine();
            }
            WriteLine("}");

            WriteLine("#endregion");
        }

        private void BuildInterface()
        {
            var dt = Table;
            WriteLine("/// <summary>{0}接口</summary>", dt.Description);
            WriteLine("public partial interface I{0}", ClassName);
            WriteLine("{");

            WriteLine("#region 属性");
            var k = Table.Columns.Count;
            foreach (var column in Table.Columns)
            {
                // 跳过排除项
                if (Option.Excludes.Contains(column.Name)) continue;
                if (Option.Excludes.Contains(column.ColumnName)) continue;

                var type = column.Properties["Type"];
                if (type.IsNullOrEmpty()) type = column.DataType?.Name;

                WriteLine("/// <summary>{0}</summary>", column.Description);
                WriteLine("{0} {1} {{ get; set; }}", type, column.Name);
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
            WriteLine("static {0}()", ClassName);
            WriteLine("{");
            {
                // 第一个非自增非主键整型字段，生成累加字段代码
                var dc = Table.Columns.FirstOrDefault(e => !e.Identity && !e.PrimaryKey && (e.DataType == typeof(Int32) || e.DataType == typeof(Int64)));
                if (dc != null)
                {
                    WriteLine("// 累加字段，生成 Update xx Set Count=Count+1234 Where xxx");
                    WriteLine("//var df = Meta.Factory.AdditionalFields;");
                    WriteLine("//df.Add(nameof({0}));", dc.Name);
                }

                var ns = new HashSet<String>(Table.Columns.Select(e => e.Name), StringComparer.OrdinalIgnoreCase);
                WriteLine();
                WriteLine("// 过滤器 UserModule、TimeModule、IPModule");
                if (ns.Contains("CreateUserID") || ns.Contains("CreateUser") || ns.Contains("UpdateUserID") || ns.Contains("UpdateUser"))
                    WriteLine("Meta.Modules.Add<UserModule>();");
                if (ns.Contains("CreateTime") || ns.Contains("UpdateTime"))
                    WriteLine("Meta.Modules.Add<TimeModule>();");
                if (ns.Contains("CreateIP") || ns.Contains("UpdateIP"))
                    WriteLine("Meta.Modules.Add<IPModule>();");

                // 唯一索引不是主键，又刚好是Master，使用单对象缓存从键
                var di = Table.Indexes.FirstOrDefault(e => e.Unique && e.Columns.Length == 1 && Table.GetColumn(e.Columns[0]).Master);
                if (di != null)
                {
                    dc = Table.GetColumn(di.Columns[0]);

                    WriteLine();
                    WriteLine("// 单对象缓存");
                    WriteLine("var sc = Meta.SingleCache;");
                    WriteLine("sc.FindSlaveKeyMethod = k => Find(_.{0} == k);", dc.Name);
                    WriteLine("sc.GetSlaveKeyMethod = e => e.{0};", dc.Name);
                }
            }
            WriteLine("}");
        }

        /// <summary>数据验证</summary>
        protected virtual void BuildValid()
        {
            WriteLine("/// <summary>验证并修补数据，通过抛出异常的方式提示验证失败。</summary>");
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
                        WriteLine("if ({0}.IsNullOrEmpty()) throw new ArgumentNullException({1}, \"{2}不能为空！\");", item.Name, NameOf(item.Name), item.DisplayName ?? item.Name);
                    }
                }

                WriteLine();
                WriteLine("// 建议先调用基类方法，基类方法会做一些统一处理");
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
                    WriteLine("// 处理当前已登录用户信息，可以由UserModule过滤器代劳");
                    WriteLine("/*var user = ManageProvider.User;");
                    WriteLine("if (user != null)");
                    WriteLine("{");
                    foreach (var item in cs)
                    {
                        if (item.Name.EqualIgnoreCase("CreateUserID"))
                            WriteLine("if (isNew && !Dirtys[{0}]) {1} = user.ID;", NameOf(item.Name), item.Name);
                        else
                            WriteLine("if (!Dirtys[{0}]) {1} = user.ID;", NameOf(item.Name), item.Name);
                    }
                    WriteLine("}*/");
                }

                var dc = Table.Columns.FirstOrDefault(e => e.Name.EqualIgnoreCase("CreateTime"));
                if (dc != null) WriteLine("//if (isNew && !Dirtys[{0}]) {1} = DateTime.Now;", NameOf(dc.Name), dc.Name);

                dc = Table.Columns.FirstOrDefault(e => e.Name.EqualIgnoreCase("UpdateTime"));
                if (dc != null) WriteLine("//if (!Dirtys[{0}]) {1} = DateTime.Now;", NameOf(dc.Name), dc.Name);

                dc = Table.Columns.FirstOrDefault(e => e.Name.EqualIgnoreCase("CreateIP"));
                if (dc != null) WriteLine("//if (isNew && !Dirtys[{0}]) {1} = ManageProvider.UserHost;", NameOf(dc.Name), dc.Name);

                dc = Table.Columns.FirstOrDefault(e => e.Name.EqualIgnoreCase("UpdateIP"));
                if (dc != null) WriteLine("//if (!Dirtys[{0}]) {1} = ManageProvider.UserHost;", NameOf(dc.Name), dc.Name);

                // 唯一索引检查唯一性
                var dis = Table.Indexes.Where(e => e.Unique).ToArray();
                if (dis.Length > 0)
                {
                    WriteLine();
                    WriteLine("// 检查唯一索引");
                    foreach (var item in dis)
                    {
                        //WriteLine("if (!_IsFromDatabase) CheckExist(isNew, {0});", Table.GetColumns(item.Columns).Select(e => "__." + e.Name).Join(", "));
                        WriteLine("// CheckExist(isNew, {0});", Table.GetColumns(item.Columns).Select(e => $"nameof({e.Name})").Join(", "));
                    }
                }
            }
            WriteLine("}");
        }

        /// <summary>初始化数据</summary>
        protected virtual void BuildInitData()
        {
            var name = ClassName;

            WriteLine("///// <summary>首次连接数据库时初始化数据，仅用于实体类重载，用户不应该调用该方法</summary>");
            WriteLine("//[EditorBrowsable(EditorBrowsableState.Never)]");
            //zilo555 去掉internal
            WriteLine("//protected override void InitData()");
            WriteLine("//{");
            WriteLine("//    // InitData一般用于当数据表没有数据时添加一些默认数据，该实体类的任何第一次数据库操作都会触发该方法，默认异步调用");
            WriteLine("//    if (Meta.Session.Count > 0) return;");
            WriteLine();
            WriteLine("//    if (XTrace.Debug) XTrace.WriteLine(\"开始初始化{0}[{1}]数据……\");", name, Table.DisplayName);
            WriteLine();
            WriteLine("//    var entity = new {0}();", name);
            foreach (var column in Table.Columns)
            {
                // 跳过排除项
                if (Option.Excludes.Contains(column.Name)) continue;
                if (Option.Excludes.Contains(column.ColumnName)) continue;

                switch (column.DataType.GetTypeCode())
                {
                    case TypeCode.Boolean:
                        WriteLine("//    entity.{0} = true;", column.Name);
                        break;
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        WriteLine("//    entity.{0} = 0;", column.Name);
                        break;
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        WriteLine("//    entity.{0} = 0.0;", column.Name);
                        break;
                    case TypeCode.DateTime:
                        WriteLine("//    entity.{0} = DateTime.Now;", column.Name);
                        break;
                    case TypeCode.String:
                        WriteLine("//    entity.{0} = \"abc\";", column.Name);
                        break;
                    default:
                        break;
                }
            }
            WriteLine("//    entity.Insert();");
            WriteLine();
            WriteLine("//    if (XTrace.Debug) XTrace.WriteLine(\"完成初始化{0}[{1}]数据！\");", name, Table.DisplayName);
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

            foreach (var column in Table.Columns)
            {
                // 跳过排除项
                if (Option.Excludes.Contains(column.Name)) continue;
                if (Option.Excludes.Contains(column.ColumnName)) continue;

                // 找到名字映射
                var dt = AllTables.FirstOrDefault(
                    e => e.PrimaryKeys.Length == 1 &&
                    e.PrimaryKeys[0].DataType == column.DataType &&
                    (e.Name + e.PrimaryKeys[0].Name).EqualIgnoreCase(column.Name));

                if (dt != null)
                {
                    // 属性名
                    var pname = dt.Name;

                    // 备注
                    var dis = column.DisplayName;
                    if (dis.IsNullOrEmpty()) dis = dt.DisplayName;

                    var pk = dt.PrimaryKeys[0];

                    WriteLine("/// <summary>{0}</summary>", dis);
                    WriteLine("[XmlIgnore, IgnoreDataMember]");
                    WriteLine("//[ScriptIgnore]");
                    WriteLine("public {1} {1} => Extends.Get({0}, k => {1}.FindBy{3}({2}));", NameOf(pname), dt.Name, column.Name, pk.Name);

                    // 主字段
                    var master = dt.Master ?? dt.GetColumn("Name");
                    // 扩展属性有可能恰巧跟已有字段同名
                    if (master != null && !master.PrimaryKey && !dt.Columns.Any(e => e.Name.EqualIgnoreCase(pname + master.Name)))
                    {
                        WriteLine();
                        WriteLine("/// <summary>{0}</summary>", dis);
                        //WriteLine("[XmlIgnore, IgnoreDataMember]");
                        //WriteLine("//[ScriptIgnore]");
                        //if (!dis.IsNullOrEmpty()) WriteLine("[DisplayName(\"{0}\")]", dis);
                        WriteLine("[Map(nameof({0}), typeof({1}), \"{2}\")]", column.Name, dt.Name, pk.Name);
                        if (master.DataType == typeof(String))
                            WriteLine("public {2} {0}{1} => {0}?.{1};", pname, master.Name, master.DataType.Name);
                        else
                            WriteLine("public {2} {0}{1} => {0} != null ? {0}.{1} : 0;", pname, master.Name, master.DataType.Name);
                    }

                    WriteLine();
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
                var name = pk.CamelName();

                WriteLine("/// <summary>根据{0}查找</summary>", pk.DisplayName);
                WriteLine("/// <param name=\"{0}\">{1}</param>", name, pk.DisplayName);
                WriteLine("/// <returns>实体对象</returns>");
                WriteLine("public static {3} FindBy{0}({1} {2})", pk.Name, pk.DataType.Name, name, ClassName);
                WriteLine("{");
                {
                    if (pk.DataType.IsInt())
                        WriteLine("if ({0} <= 0) return null;", name);
                    else if (pk.DataType == typeof(String))
                        WriteLine("if ({0}.IsNullOrEmpty()) return null;", name);

                    WriteLine();
                    WriteLine("// 实体缓存");
                    if (pk.DataType == typeof(String))
                        WriteLine("if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.{0}.EqualIgnoreCase({1}));", pk.Name, name);
                    else
                        WriteLine("if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => e.{0} == {1});", pk.Name, name);

                    WriteLine();
                    WriteLine("// 单对象缓存");
                    WriteLine("return Meta.SingleCache[{0}];", name);

                    WriteLine();
                    WriteLine("//return Find(_.{0} == {1});", pk.Name, name);
                }
                WriteLine("}");
            }

            // 索引
            foreach (var di in Table.Indexes)
            {
                // 跳过主键
                if (di.Columns.Length == 1 && pk != null && di.Columns[0].EqualIgnoreCase(pk.Name, pk.ColumnName)) continue;

                // 超过2字段索引，不要生成查询函数
                if (di.Columns.Length > 2) continue;

                var cs = Table.GetColumns(di.Columns);
                if (cs == null || cs.Length != di.Columns.Length) continue;

                // 只有整数和字符串能生成查询函数
                if (!cs.All(e => e.DataType.IsInt() || e.DataType == typeof(String))) continue;

                WriteLine();
                WriteLine("/// <summary>根据{0}查找</summary>", cs.Select(e => e.DisplayName).Join("、"));
                foreach (var dc in cs)
                {
                    WriteLine("/// <param name=\"{0}\">{1}</param>", dc.CamelName(), dc.DisplayName);
                }

                // 返回类型
                var rt = ClassName;
                if (!di.Unique) rt = $"IList<{rt}>";

                WriteLine("/// <returns>{0}</returns>", di.Unique ? "实体对象" : "实体列表");
                WriteLine("public static {2} Find{3}By{0}({1})", cs.Select(e => e.Name).Join("And"), cs.Select(e => e.DataType.Name + " " + e.CamelName()).Join(", "), rt, di.Unique ? "" : "All");
                WriteLine("{");
                {
                    var exp = new StringBuilder();
                    var wh = new StringBuilder();
                    foreach (var dc in cs)
                    {
                        if (exp.Length > 0) exp.Append(" & ");
                        exp.AppendFormat("_.{0} == {1}", dc.Name, dc.CamelName());

                        if (wh.Length > 0) wh.Append(" && ");
                        if (dc.DataType == typeof(String))
                            wh.AppendFormat("e.{0}.EqualIgnoreCase({1})", dc.Name, dc.CamelName());
                        else
                            wh.AppendFormat("e.{0} == {1}", dc.Name, dc.CamelName());
                    }

                    if (di.Unique)
                    {
                        WriteLine("// 实体缓存");
                        WriteLine("if (Meta.Session.Count < 1000) return Meta.Cache.Find(e => {0});", wh);

                        // 单对象缓存
                        if (cs.Length == 1 && cs[0].Master)
                        {
                            WriteLine();
                            WriteLine("// 单对象缓存");
                            WriteLine("//return Meta.SingleCache.GetItemWithSlaveKey({0}) as {1};", cs[0].CamelName(), rt);
                        }

                        WriteLine();
                        WriteLine("return Find({0});", exp);
                    }
                    else
                    {
                        WriteLine("// 实体缓存");
                        WriteLine("if (Meta.Session.Count < 1000) return Meta.Cache.FindAll(e => {0});", wh);

                        WriteLine();
                        WriteLine("return FindAll({0});", exp);
                    }
                }
                WriteLine("}");
            }

            WriteLine("#endregion");
        }

        /// <summary>高级查询</summary>
        protected virtual void BuildSearch()
        {
            // 收集索引信息，索引中的所有字段都参与，构造一个高级查询模板
            var idx = Table.Indexes ?? new List<IDataIndex>();
            var cs = new List<IDataColumn>();
            if (idx != null && idx.Count > 0)
            {
                // 索引中的所有字段，按照表字段顺序
                var dcs = idx.SelectMany(e => e.Columns).Distinct().ToArray();
                foreach (var dc in Table.Columns)
                {
                    // 主键和自增，不参与
                    if (dc.PrimaryKey || dc.Identity) continue;

                    if (dc.Name.EqualIgnoreCase(dcs) || dc.ColumnName.EqualIgnoreCase(dcs)) cs.Add(dc);
                }
            }

            var returnName = ClassName;

            WriteLine("#region 高级查询");
            if (cs.Count > 0)
            {
                // 时间字段。无差别支持UpdateTime/CreateTime
                var dcTime = cs.FirstOrDefault(e => e.DataType == typeof(DateTime));
                if (dcTime == null) dcTime = Table.GetColumns(new[] { "UpdateTime", "CreateTime" })?.FirstOrDefault();
                cs.Remove(dcTime);

                // 可用于关键字模糊搜索的字段
                var keys = Table.Columns.Where(e => e.DataType == typeof(String) && !cs.Contains(e)).ToList();

                // 注释部分
                WriteLine("/// <summary>高级查询</summary>");
                foreach (var dc in cs)
                {
                    WriteLine("/// <param name=\"{0}\">{1}</param>", dc.CamelName(), dc.Description);
                }
                if (dcTime != null)
                {
                    WriteLine("/// <param name=\"start\">{0}开始</param>", dcTime.DisplayName);
                    WriteLine("/// <param name=\"end\">{0}结束</param>", dcTime.DisplayName);
                }
                WriteLine("/// <param name=\"key\">关键字</param>");
                WriteLine("/// <param name=\"page\">分页参数信息。可携带统计和数据权限扩展查询等信息</param>");
                WriteLine("/// <returns>实体列表</returns>");

                // 参数部分
                //var pis = cs.Join(", ", dc => $"{dc.DataType.Name} {dc.CamelName()}");
                var pis = new StringBuilder();
                foreach (var dc in cs)
                {
                    if (pis.Length > 0) pis.Append(", ");

                    if (dc.DataType == typeof(Boolean))
                        pis.Append($"{dc.DataType.Name}? {dc.CamelName()}");
                    else
                        pis.Append($"{dc.DataType.Name} {dc.CamelName()}");
                }
                var piTime = dcTime == null ? "" : "DateTime start, DateTime end, ";
                WriteLine("public static IList<{0}> Search({1}, {2}String key, PageParameter page)", returnName, pis, piTime);
                WriteLine("{");
                {
                    WriteLine("var exp = new WhereExpression();");

                    // 构造表达式
                    WriteLine();
                    foreach (var dc in cs)
                    {
                        if (dc.DataType.IsInt())
                            WriteLine("if ({0} >= 0) exp &= _.{1} == {0};", dc.CamelName(), dc.Name);
                        else if (dc.DataType == typeof(Boolean))
                            WriteLine("if ({0} != null) exp &= _.{1} == {0};", dc.CamelName(), dc.Name);
                        else if (dc.DataType == typeof(String))
                            WriteLine("if (!{0}.IsNullOrEmpty()) exp &= _.{1} == {0};", dc.CamelName(), dc.Name);
                    }
                    if (dcTime != null)
                    {
                        WriteLine("exp &= _.{0}.Between(start, end);", dcTime.Name);
                    }
                    if (keys.Count > 0)
                    {
                        WriteLine("if (!key.IsNullOrEmpty()) exp &= {0};", keys.Join(" | ", k => $"_.{k.Name}.Contains(key)"));
                    }

                    // 查询返回
                    WriteLine();
                    WriteLine("return FindAll(exp, page);");
                }
                WriteLine("}");

            }

            // 字段缓存，用于魔方前台下拉选择
            {
                // 主键和时间字段
                var pk = Table.Columns.FirstOrDefault(e => e.Identity);
                var pname = pk?.Name ?? "Id";
                var dcTime = cs.FirstOrDefault(e => e.DataType == typeof(DateTime));
                var tname = dcTime?.Name ?? "CreateTime";

                // 遍历索引，第一个字段是字符串类型，则为其生成下拉选择
                var count = 0;
                foreach (var di in idx)
                {
                    if (di.Columns == null || di.Columns.Length == 0) continue;

                    // 单字段唯一索引，不需要
                    if (di.Unique && di.Columns.Length == 1) continue;

                    var dc = Table.GetColumn(di.Columns[0]);
                    if (dc == null || dc.DataType != typeof(String) || dc.Master) continue;

                    var name = dc.Name;

                    WriteLine();
                    WriteLine($"// Select Count({pname}) as {pname},{name} From {Table.TableName} Where {tname}>'2020-01-24 00:00:00' Group By {name} Order By {pname} Desc limit 20");
                    WriteLine($"static readonly FieldCache<{returnName}> _{name}Cache = new FieldCache<{returnName}>(nameof({name}))");
                    WriteLine("{");
                    {
                        WriteLine($"//Where = _.{tname} > DateTime.Today.AddDays(-30) & Expression.Empty");
                    }
                    WriteLine("};");
                    WriteLine();
                    WriteLine($"/// <summary>获取{dc.DisplayName}列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>");
                    WriteLine("/// <returns></returns>");
                    WriteLine($"public static IDictionary<String, String> Get{name}List() => _{name}Cache.FindAllName();");

                    count++;
                }

                // 如果没有输出，则生成一个注释的模板
                if (count == 0)
                {
                    WriteLine();
                    WriteLine($"// Select Count({pname}) as {pname},Category From {Table.TableName} Where {tname}>'2020-01-24 00:00:00' Group By Category Order By {pname} Desc limit 20");
                    WriteLine($"//static readonly FieldCache<{returnName}> _CategoryCache = new FieldCache<{returnName}>(nameof(Category))");
                    WriteLine("//{");
                    {
                        WriteLine($"//Where = _.{tname} > DateTime.Today.AddDays(-30) & Expression.Empty");
                    }
                    WriteLine("//};");
                    WriteLine();
                    WriteLine("///// <summary>获取类别列表，字段缓存10分钟，分组统计数据最多的前20种，用于魔方前台下拉选择</summary>");
                    WriteLine("///// <returns></returns>");
                    WriteLine("//public static IDictionary<String, String> GetCategoryList() => _CategoryCache.FindAllName();");
                }
            }

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