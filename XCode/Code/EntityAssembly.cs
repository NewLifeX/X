using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NewLife.Collections;
using NewLife.Log;
using XCode.DataAccessLayer;
using XCode.Exceptions;
using NewLife.Linq;
using System.Text;
using NewLife.Security;
using NewLife.Configuration;

namespace XCode.Code
{
    /// <summary>实体程序集</summary>
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
        private static DictionaryCache<String, Assembly> cache = new DictionaryCache<String, Assembly>();
        /// <summary>为数据模型创建实体程序集，带缓存，依赖于表和字段名称，不依赖名称以外的信息。</summary>
        /// <param name="name"></param>
        /// <param name="tables"></param>
        /// <returns></returns>
        public static Assembly CreateWithCache(String name, List<IDataTable> tables)
        {
            if (String.IsNullOrEmpty(name)) return null;
            if (tables == null) return null;

            // 构建缓存Key
            var sb = new StringBuilder();
            sb.Append(name);
            foreach (var item in tables)
            {
                sb.Append("|");
                sb.Append(item.Name);
                foreach (var dc in item.Columns)
                {
                    sb.Append(",");
                    sb.Append(dc.Name);
                }
            }
            var key = DataHelper.Hash(sb.ToString());

            return cache.GetItem<String, List<IDataTable>>(key, name, tables, (k, n, ts) =>
            {
                EntityAssembly asm = new EntityAssembly();
                asm.Name = n;
                asm.Tables = ts;
                asm.CreateAll();

                return asm.Compile();
            });
        }

        /// <summary>为数据模型创建实体程序集，无缓存</summary>
        /// <param name="name">程序集名</param>
        /// <param name="connName">连接名</param>
        /// <param name="tables">模型表</param>
        /// <returns></returns>
        public static Assembly Create(String name, String connName, List<IDataTable> tables)
        {
            if (String.IsNullOrEmpty(name)) return null;
            if (tables == null) return null;

            EntityAssembly asm = new EntityAssembly();
            asm.Name = name;
            asm.ConnName = connName;
            asm.Tables = tables;
            asm.CreateAll();

            return asm.Compile();
        }
        #endregion

        #region 方法
        /// <summary>
        /// 在该程序集中创建一个实体类
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public EntityClass Create(IDataTable table)
        {
            // 复制一份，以免修改原来的结构
            IDataTable tb = table.Clone() as IDataTable;
            //String className = tb.Name.Replace("$", null);

            //// 计算名称，防止属性名和类型名重名
            //StringCollection list = new StringCollection();
            //list.Add("Item");
            //list.Add("System");
            //list.Add(className);

            //// 保存属性名，可能跟字段名不一致
            //Dictionary<String, String> fieldNames = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
            //foreach (IDataColumn item in tb.Columns)
            //{
            //    String name = item.Name;
            //    for (int i = 2; list.Contains(name); i++)
            //    {
            //        name = item.Name + i;
            //    }
            //    //item.Name = name;
            //    fieldNames.Add(item.Name, name);
            //}

            EntityClass entity = new EntityClass();
            entity.Assembly = this;
            entity.ClassName = tb.Alias;
            entity.Table = tb;
            //entity.FieldNames = fieldNames;
            entity.Create();
            entity.AddProperties();
            entity.AddIndexs();
            //entity.AddNames();

            Classes.Add(entity);
            return entity;
        }

        /// <summary>
        /// 根据名称创建
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public EntityClass Create(String name)
        {
            foreach (IDataTable item in Tables)
            {
                if (item.Name == name) return Create(item);
            }
            return null;
        }

        /// <summary>
        /// 创建所以表的实体类
        /// </summary>
        public void CreateAll()
        {
            if (Tables == null) return;

            foreach (IDataTable item in Tables)
            {
                EntityClass entity = Create(item);
                //entity.Create();
                //entity.AddProperties();
                //entity.AddIndexs();
                //entity.AddNames();
            }
        }
        #endregion

        #region 生成代码
        /// <summary>
        /// 生成C#代码
        /// </summary>
        /// <returns></returns>
        public String GenerateCSharpCode()
        {
            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
            CodeGeneratorOptions options = new CodeGeneratorOptions();
            options.BracingStyle = "C";
            options.VerbatimOrder = true;
            using (StringWriter writer = new StringWriter())
            {
                provider.GenerateCodeFromCompileUnit(Unit, writer, options);
                //return writer.ToString();

                String str = writer.ToString();

                // 去掉头部
                //str = str.Substring(str.IndexOf("using"));
                Type dt = typeof(DateTime);
                str = str.Replace(dt.ToString(), dt.Name);

                return str;
            }
        }
        #endregion

        #region 编译
        /// <summary>
        /// 编译
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public CompilerResults Compile(CompilerParameters options)
        {
            if (options == null)
            {
                options = new CompilerParameters();
                options.GenerateInMemory = true;

                if (Debug)
                {
                    options.GenerateInMemory = false;

                    String tempPath = XTrace.TempPath;
                    //if (!String.IsNullOrEmpty(tempPath)) tempPath = Path.Combine(tempPath, Name);
                    if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);
                    options.OutputAssembly = Path.Combine(tempPath, String.Format("XCode.{0}.dll", Name));
                    options.TempFiles = new TempFileCollection(tempPath, false);
                }
            }

            String[] refs = new String[] { "System.dll", "XCode.dll", "NewLife.Core.dll" };
            Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly item in asms)
            {
                String name = null;
                try
                {
                    name = item.Location;
                }
                catch { }
                if (String.IsNullOrEmpty(name)) continue;

                String fileName = Path.GetFileName(name);
                if (!refs.Contains(fileName, StringComparer.OrdinalIgnoreCase)) continue;

                if (!options.ReferencedAssemblies.Contains(name)) options.ReferencedAssemblies.Add(name);
            }

            CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");

            CompilerResults rs = null;
            if (!Debug)
                rs = provider.CompileAssemblyFromDom(options, Unit);
            else
            {
                // 先生成代码文件，方便调试
                String fileName = Path.Combine(XTrace.TempPath, Name + ".cs");
                using (StreamWriter writer = new StreamWriter(fileName))
                {
                    var op = new CodeGeneratorOptions();
                    op.BracingStyle = "C";
                    op.VerbatimOrder = true;

                    provider.GenerateCodeFromCompileUnit(Unit, writer, op);
                }

                // 编译
                rs = provider.CompileAssemblyFromFile(options, fileName);

                // 既然编译正常，这里就删除临时文件
                try
                {
                    File.Delete(fileName);
                }
                catch { }

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

        /// <summary>
        /// 编译并返回程序集
        /// </summary>
        /// <returns></returns>
        public Assembly Compile()
        {
            CompilerResults rs = Compile(null);
            if (rs.Errors == null || !rs.Errors.HasErrors) return rs.CompiledAssembly;

            //throw new XCodeException(rs.Errors[0].ErrorText);

            CompilerError err = rs.Errors[0];
            String msg = String.Format("{0} {1} {2}({3},{4})", err.ErrorNumber, err.ErrorText, err.FileName, err.Line, err.Column);
            throw new XCodeException(msg);
        }
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
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString() { return Name; }
        #endregion
    }
}