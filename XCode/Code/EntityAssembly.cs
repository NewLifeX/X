using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Reflection;
using XCode.DataAccessLayer;

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
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>连接名</summary>
        public String ConnName { get; set; }

        /// <summary>表集合</summary>
        public IList<IDataTable> Tables { get; set; }

        /// <summary>程序集</summary>
        public Assembly Assembly { get; private set; }

        /// <summary>类型映射。数据表映射到哪个类上</summary>
        public Dictionary<String, String> TypeMaps { get; private set; }

        private List<String> Codes { get; set; } = new List<String>();
        #endregion

        #region 构造
        private static ConcurrentDictionary<String, EntityAssembly> cache = new ConcurrentDictionary<String, EntityAssembly>();
        /// <summary>为数据模型创建实体程序集，带缓存，依赖于表和字段名称，不依赖名称以外的信息。</summary>
        /// <param name="name">名称</param>
        /// <param name="tables"></param>
        /// <returns></returns>
        public static EntityAssembly CreateWithCache(String name, List<IDataTable> tables)
        {
            if (name.IsNullOrEmpty() || tables == null || tables.Count == 0) return null;

            return cache.GetOrAdd(name, k =>
            {
                var asm = new EntityAssembly
                {
                    Name = name,
                    ConnName = name,
                    Tables = tables
                };

                return asm;
            });
        }
        #endregion

        #region 方法
        /// <summary>在该程序集中创建一个实体类</summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public EntityBuilder Create(IDataTable table)
        {
            if (String.IsNullOrEmpty(table.Name)) throw new ArgumentNullException(nameof(table.Name), "数据表中将用作实体类名的Name不能为空！");

            // 复制一份，以免修改原来的结构
            var tb = table.Clone() as IDataTable;
            return Create2(tb);
        }

        EntityBuilder Create2(IDataTable table)
        {
            var builder = new EntityBuilder
            {
                Table = table,
                ConnName = ConnName,
                Output = XTrace.TempPath.CombinePath(ConnName).EnsureDirectory(false)
            };

            if (Debug) builder.Log = XTrace.Log;
            builder.CSharp = new Version(5, 0);

            builder.Execute();
            Codes.Add(builder.Save(null, true));

            builder.Business = true;
            builder.Execute();
            Codes.Add(builder.Save(null, true));

            return builder;
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
                        for (var i = 2; i < Int32.MaxValue; i++)
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
            }
            TypeMaps = dic;
        }

        /// <summary>获取类型</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public Type GetType(String name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            // 第一次使用时编译
            if (Assembly == null) Compile();
            if (Assembly == null) throw new Exception("未编译！");

            var asmx = AssemblyX.Create(Assembly);

            if (TypeMaps.TryGetValue(name, out var typeName))
                return asmx.GetType(typeName);
            else
                return asmx.GetType(name);
        }
        #endregion

        #region 编译
        /// <summary>编译</summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public CompilerResults Compile(CompilerParameters options)
        {
            if (Codes.Count < 1) CreateAll();

            var tempPath = XTrace.TempPath.EnsureDirectory(false);
            if (options == null)
            {
                options = new CompilerParameters
                {
                    GenerateInMemory = true
                };

                if (Debug)
                {
                    options.GenerateInMemory = false;

                    options.OutputAssembly = tempPath.CombinePath("XCode.{0}.dll".F(Name));
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

                if (!options.ReferencedAssemblies.Contains(name)) options.ReferencedAssemblies.Add(name);
            }

            var provider = CodeDomProvider.CreateProvider("CSharp");
            // 编译
            return provider.CompileAssemblyFromFile(options, Codes.ToArray());
        }

        /// <summary>编译并返回程序集</summary>
        /// <returns></returns>
        public Assembly Compile()
        {
            var rs = Compile(null);
            if (rs.Errors == null || !rs.Errors.HasErrors) return Assembly = rs.CompiledAssembly;

            var err = rs.Errors[0];
            var msg = String.Format("{0} {1} {2}({3},{4})", err.ErrorNumber, err.ErrorText, err.FileName, err.Line, err.Column);
            throw new XCodeException(msg);
        }
        #endregion

        #region 调试
        /// <summary>是否启用动态代码调试，把动态生成的实体类代码和程序集输出到临时目录，默认不启用</summary>
        public static Boolean Debug { get; set; }
        #endregion

        #region 辅助函数
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => Name;
        #endregion
    }
}