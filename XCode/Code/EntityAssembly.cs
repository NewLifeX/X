using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using NewLife.Collections;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Security;
using XCode.DataAccessLayer;
using XCode.Exceptions;

#if NET4
using System.Linq;
#else
using NewLife.Linq;
#endif

namespace XCode.Code
{
    /// <summary>实体程序集</summary>
    /// <example>
    /// 外部修改实体类生成行为的例子：
    /// <code>
    /// var dal = DAL.Create("Common");
    /// var ea = dal.Assembly;
    /// 
    /// ea.OnClassCreating += (s, e) =&gt;
    /// {
    ///     if (e.Class.Name == "Log") e.Class.BaseType = "Test.TestEntity&lt;Log&gt;";
    /// };
    /// 
    /// var eop = dal.CreateOperate("Log");
    /// var type = eop.Default.GetType();
    /// Console.WriteLine(type);
    /// type = type.BaseType;
    /// Console.WriteLine(type);
    /// type = type.BaseType;
    /// Console.WriteLine(type);
    /// </code>
    /// </example>
    public class EntityAssembly
    {
        #region 属性
        //private DAL _Dal;
        ///// <summary>数据访问层</summary>
        //public DAL Dal
        //{
        //    get { return _Dal; }
        //    set { _Dal = value; }
        //}
        private String _Name;
        /// <summary>名称</summary>
        public String Name { get { return _Name; } set { _Name = value; } }

        private String _ConnName;
        /// <summary>连接名</summary>
        public String ConnName { get { return !String.IsNullOrEmpty(_ConnName) ? _ConnName : Name; } set { _ConnName = value; } }

        private List<IDataTable> _Tables;
        /// <summary>表集合</summary>
        public List<IDataTable> Tables { get { return _Tables; } set { _Tables = value; } }

        private List<EntityClass> _Classes;
        /// <summary>实体类集合</summary>
        public List<EntityClass> Classes
        {
            get { return _Classes ?? (_Classes = new List<EntityClass>()); }
            //set { _Classes = value; }
        }

        private Assembly _Assembly;
        /// <summary>程序集</summary>
        public Assembly Assembly { get { return _Assembly; } }

        private Dictionary<String, String> _TypeMaps;
        /// <summary>类型映射。数据表映射到哪个类上</summary>
        public Dictionary<String, String> TypeMaps { get { return _TypeMaps; } }
        #endregion

        #region 生成属性
        private CodeCompileUnit _Unit;
        /// <summary>代码编译单元</summary>
        public CodeCompileUnit Unit
        {
            get
            {
                if (_Unit == null) _Unit = new CodeCompileUnit();
                return _Unit;
            }
            set { _Unit = value; }
        }

        private CodeNamespace _NameSpace;
        /// <summary>命名空间</summary>
        public CodeNamespace NameSpace
        {
            get
            {
                if (_NameSpace == null)
                {
                    _NameSpace = new CodeNamespace(String.Format("XCode.{0}.Entities", Name));
                    _NameSpace.Imports.Add(new CodeNamespaceImport("System"));
                    _NameSpace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
                    _NameSpace.Imports.Add(new CodeNamespaceImport("System.ComponentModel"));
                    _NameSpace.Imports.Add(new CodeNamespaceImport("XCode"));

                    Unit.Namespaces.Add(_NameSpace);
                }
                return _NameSpace;
            }
            set
            {
                _NameSpace = value;
                if (_NameSpace != null && !Unit.Namespaces.Contains(_NameSpace)) Unit.Namespaces.Add(_NameSpace);
            }
        }
        #endregion

        #region 构造
        private static DictionaryCache<String, EntityAssembly> cache = new DictionaryCache<String, EntityAssembly>();
        /// <summary>为数据模型创建实体程序集，带缓存，依赖于表和字段名称，不依赖名称以外的信息。</summary>
        /// <param name="name">名称</param>
        /// <param name="tables"></param>
        /// <returns></returns>
        public static EntityAssembly CreateWithCache(String name, List<IDataTable> tables)
        {
            if (String.IsNullOrEmpty(name)) return null;
            if (tables == null) return null;

            // 构建缓存Key
            var sb = new StringBuilder();
            sb.Append(name);
            foreach (var item in tables)
            {
                sb.Append("|");
                sb.Append(item.TableName);
                foreach (var dc in item.Columns)
                {
                    sb.Append(",");
                    sb.Append(dc.ColumnName);
                }
            }
            var key = DataHelper.Hash(sb.ToString());

            return cache.GetItem<String, List<IDataTable>>(key, name, tables, (k, n, ts) =>
            {
                var asm = new EntityAssembly();
                asm.Name = n;
                asm.Tables = ts;
                //asm.CreateAll();

                //asm.Compile();
                return asm;
            });
        }

        /// <summary>为数据模型创建实体程序集，无缓存</summary>
        /// <param name="name">程序集名</param>
        /// <param name="connName">连接名</param>
        /// <param name="tables">模型表</param>
        /// <returns></returns>
        public static EntityAssembly Create(String name, String connName, List<IDataTable> tables)
        {
            if (String.IsNullOrEmpty(name)) return null;
            if (tables == null) return null;

            var asm = new EntityAssembly();
            asm.Name = name;
            asm.ConnName = connName;
            asm.Tables = tables;
            //asm.CreateAll();

            //asm.Compile();

            return asm;
        }
        #endregion

        #region 方法
        /// <summary>在该程序集中创建一个实体类</summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public EntityClass Create(IDataTable table)
        {
            if (String.IsNullOrEmpty(table.Name)) throw new ArgumentNullException("Alias", "数据表中将用作实体类名的别名Alias不能为空！");

            // 复制一份，以免修改原来的结构
            var tb = table.Clone() as IDataTable;
            return Create2(tb);
        }

        EntityClass Create2(IDataTable table)
        {
            var entity = new EntityClass();
            //entity.Assembly = this;
            entity.Name = table.Name;
            entity.Table = table;
            entity.ConnName = ConnName;

            if (OnClassCreating != null)
            {
                var e = new EntityClassEventArgs { Class = entity };
                OnClassCreating(this, e);
                if (e.Cancel) return null;

                entity = e.Class;
            }

            entity.Create();
            entity.AddProperties();
            entity.AddIndexs();
            //entity.AddNames();

            if (OnClassCreated != null)
            {
                var e = new EntityClassEventArgs { Class = entity };
                OnClassCreated(this, e);
                if (e.Cancel) return null;

                entity = e.Class;
            }

            if (entity != null)
            {
                NameSpace.Types.Add(entity.Class);

                Classes.Add(entity);
            }

            return entity;
        }

        /// <summary>根据名称创建</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public EntityClass Create(String name)
        {
            foreach (var item in Tables)
            {
                if (item.TableName == name) return Create(item);
            }
            return null;
        }

        /// <summary>创建所有表的实体类</summary>
        public void CreateAll()
        {
            if (Tables == null) return;

            var dic = new Dictionary<String, String>();
            var list = new List<String>();
            foreach (var item in Tables)
            {
                // 复制一份，以免修改原来的结构
                var tb = item.Clone() as IDataTable;

                #region 避免类名重名
                if (!list.Contains(tb.Name))
                    list.Add(tb.Name);
                else
                {
                    if (!list.Contains(tb.TableName))
                    {
                        tb.Name = tb.TableName;
                        list.Add(tb.Name);
                    }
                    else
                    {
                        var name = tb.Name;
                        for (int i = 2; i < Int32.MaxValue; i++)
                        {
                            name = tb.Name + i;
                            if (!list.Contains(name))
                            {
                                tb.Name = name;
                                list.Add(tb.Name);
                                break;
                            }
                        }
                    }
                }
                #endregion

                var entity = Create2(tb);
                if (entity != null) dic.Add(tb.TableName, tb.Name);

                //entity.Create();
                //entity.AddProperties();
                //entity.AddIndexs();
                //entity.AddNames();
            }
            _TypeMaps = dic;

            Unit.AddAttribute<AssemblyVersionAttribute>(String.Format("{0:yyyy}.{0:MMdd}.*", DateTime.Now));

            Unit.AddAttribute<AssemblyTitleAttribute>("XCode动态程序集" + Name);
            Unit.AddAttribute<AssemblyDescriptionAttribute>("XCode动态生成的实体类程序集");

            var asmx = AssemblyX.Create(Assembly.GetExecutingAssembly());
            Unit.AddAttribute<AssemblyFileVersionAttribute>(asmx.FileVersion);
            Unit.AddAttribute<AssemblyCompanyAttribute>(asmx.Company);
            Unit.AddAttribute<AssemblyProductAttribute>(asmx.Asm.GetCustomAttributeValue<AssemblyProductAttribute, String>());
            Unit.AddAttribute<AssemblyCopyrightAttribute>(asmx.Asm.GetCustomAttributeValue<AssemblyCopyrightAttribute, String>());
            Unit.AddAttribute<AssemblyTrademarkAttribute>(asmx.Asm.GetCustomAttributeValue<AssemblyTrademarkAttribute, String>());
        }

        /// <summary>获取类型</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public Type GetType(String name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            // 第一次使用时编译
            if (Assembly == null) Compile();
            if (Assembly == null) throw new Exception("未编译！");

            var asmx = AssemblyX.Create(Assembly);

            String typeName = null;
            if (TypeMaps.TryGetValue(name, out typeName))
                return asmx.GetType(typeName);
            else
                return asmx.GetType(name);
        }
        #endregion

        #region 生成代码
        /// <summary>生成C#代码</summary>
        /// <returns></returns>
        public String GenerateCSharpCode()
        {
            if (NameSpace.Types.Count < 1) CreateAll();

            var provider = CodeDomProvider.CreateProvider("CSharp");
            var options = new CodeGeneratorOptions();
            options.BracingStyle = "C";
            options.VerbatimOrder = true;
            using (var writer = new StringWriter())
            {
                provider.GenerateCodeFromCompileUnit(Unit, writer, options);
                //return writer.ToString();

                var str = writer.ToString();

                // 去掉头部
                //str = str.Substring(str.IndexOf("using"));
                var dt = typeof(DateTime);
                str = str.Replace(dt.ToString(), dt.Name);

                return str;
            }
        }
        #endregion

        #region 编译
        /// <summary>编译</summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public CompilerResults Compile(CompilerParameters options)
        {
            if (NameSpace.Types.Count < 1) CreateAll();

            if (options == null)
            {
                options = new CompilerParameters();
                options.GenerateInMemory = true;

                if (Debug)
                {
                    options.GenerateInMemory = false;

                    var tempPath = XTrace.TempPath;
                    //if (!String.IsNullOrEmpty(tempPath)) tempPath = Path.Combine(tempPath, Name);
                    if (!String.IsNullOrEmpty(tempPath) && !Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);
                    options.OutputAssembly = Path.Combine(tempPath, String.Format("XCode.{0}.dll", Name));
                    options.TempFiles = new TempFileCollection(tempPath, false);
                }
            }

            var refs = new String[] { "System.dll", "XCode.dll", "NewLife.Core.dll" };
            foreach (var item in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (item is AssemblyBuilder) continue;

                String name = null;
                try
                {
                    name = item.Location;
                }
                catch { }
                if (String.IsNullOrEmpty(name)) continue;

                //var fileName = Path.GetFileName(name);
                //if (!refs.Contains(fileName, StringComparer.OrdinalIgnoreCase)) continue;

                if (!options.ReferencedAssemblies.Contains(name)) options.ReferencedAssemblies.Add(name);
            }

            //// 引用入口程序集
            //var entry = Assembly.GetEntryAssembly();
            //options.ReferencedAssemblies.Add(entry.Location);

            var provider = CodeDomProvider.CreateProvider("CSharp");

            CompilerResults rs = null;
            if (!Debug)
                rs = provider.CompileAssemblyFromDom(options, Unit);
            else
            {
                // 先生成代码文件，方便调试
                var fileName = Path.Combine(XTrace.TempPath, Name + ".cs");
                using (var writer = new StreamWriter(fileName))
                {
                    var op = new CodeGeneratorOptions();
                    op.BracingStyle = "C";
                    op.VerbatimOrder = true;

                    provider.GenerateCodeFromCompileUnit(Unit, writer, op);
                }

                // 如果目标DLL文件已经存在，则尝试删除，如果删除失败，则不要输出DLL
                var dllfile = Path.Combine(XTrace.TempPath, options.OutputAssembly);
                if (File.Exists(dllfile))
                {
                    try
                    {
                        File.Delete(dllfile);
                    }
                    catch
                    {
                        options.GenerateInMemory = true;
                    }
                }

                // 编译
                rs = provider.CompileAssemblyFromFile(options, fileName);

                // 既然编译正常，这里就删除临时文件
                if (!rs.Errors.HasErrors)
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch { }
                }

                //#region 既然编译正常，这里就删除临时文件
                //var tp = options.TempFiles.BasePath;
                //var ss = Directory.GetFiles(Path.GetDirectoryName(tp), Path.GetFileName(tp) + ".*", SearchOption.TopDirectoryOnly);
                //if (ss != null)
                //{
                //    foreach (var item in ss)
                //    {
                //        try
                //        {
                //            File.Delete(item);
                //        }
                //        catch { }
                //    }
                //}
                //#endregion
            }

            return rs;
        }

        /// <summary>编译并返回程序集</summary>
        /// <returns></returns>
        public Assembly Compile()
        {
            var rs = Compile(null);
            if (rs.Errors == null || !rs.Errors.HasErrors) return _Assembly = rs.CompiledAssembly;

            //throw new XCodeException(rs.Errors[0].ErrorText);

            var err = rs.Errors[0];
            var msg = String.Format("{0} {1} {2}({3},{4})", err.ErrorNumber, err.ErrorText, err.FileName, err.Line, err.Column);
            throw new XCodeException(msg);
        }
        #endregion

        #region 事件
        /// <summary>创建实体类开始前触发，用户可以在此修改实体类的创建行为</summary>
        public event EventHandler<EntityClassEventArgs> OnClassCreating;

        /// <summary>创建实体类完成后触发，用户可以在此修改实体类的创建行为</summary>
        public event EventHandler<EntityClassEventArgs> OnClassCreated;
        #endregion

        #region 调试
        private static Boolean? _Debug;
        /// <summary>是否启用动态代码调试，把动态生成的实体类代码和程序集输出到临时目录，默认不启用</summary>
        public static Boolean Debug
        {
            get
            {
                if (_Debug != null) return _Debug.Value;

                _Debug = Config.GetConfig<Boolean>("XCode.Code.Debug", false);

                return _Debug.Value;
            }
            set { _Debug = value; }
        }
        #endregion

        #region 辅助函数
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return Name; }
        #endregion
    }

    /// <summary>实体类事件参数</summary>
    public class EntityClassEventArgs : CancelEventArgs
    {
        private EntityClass _Class;
        /// <summary>实体类</summary>
        public EntityClass Class { get { return _Class; } set { _Class = value; } }
    }
}