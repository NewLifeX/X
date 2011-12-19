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

namespace XCode.Code
{
    /// <summary>实体程序集</summary>
    public class EntityAssembly
    {
        #region 属性
        private DAL _Dal;
        /// <summary>数据访问层</summary>
        public DAL Dal
        {
            get { return _Dal; }
            set { _Dal = value; }
        }

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
                    _NameSpace = new CodeNamespace(String.Format("XCode.{0}.Entities", Dal.ConnName));
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
        /// <summary>
        ///为数据访问层创建实体程序集
        /// </summary>
        /// <param name="dal"></param>
        /// <returns></returns>
        public static Assembly Create(DAL dal)
        {
            //String key = dal.ConnName;
            //if (cache.ContainsKey(key)) return cache[key];
            //lock (cache)
            //{
            //    if (cache.ContainsKey(key)) return cache[key];

            return cache.GetItem(dal.ConnName, delegate(String key)
            {
                EntityAssembly asm = new EntityAssembly();
                asm.Dal = dal;
                asm.CreateAll();

                Assembly am = asm.Compile();
                //cache.Add(key, am);
                return am;
            });
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
            foreach (IDataTable item in Dal.Tables)
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
            foreach (IDataTable item in Dal.Tables)
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

                if (DAL.Debug)
                {
                    options.GenerateInMemory = false;
                    options.OutputAssembly = String.Format("XCode.{0}.dll", Dal.ConnName);

                    String path = XTrace.TempPath;
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    options.TempFiles = new TempFileCollection(path, true);
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

            var rs = provider.CompileAssemblyFromDom(options, Unit);
            // 既然编译正常，这里就删除临时文件
            options.TempFiles.Delete();
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

        #region 辅助函数
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Dal.ToString();
        }
        #endregion
    }
}