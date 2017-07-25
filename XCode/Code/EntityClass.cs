using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using XCode.DataAccessLayer;

namespace XCode.Code
{
    /// <summary>实体类</summary>
    /// <remarks>提供由IDataTable生成实体类的支持</remarks>
    public class EntityClass
    {
        #region 属性
        /// <summary>类名</summary>
        public String Name { get; set; }

        /// <summary>连接名</summary>
        public String ConnName { get; set; }

        /// <summary>表</summary>
        public IDataTable Table { get; set; }

        private String _BaseType;
        /// <summary>基类</summary>
        public String BaseType
        {
            get
            {
                if (_BaseType == null && !String.IsNullOrEmpty(Name)) _BaseType = String.Format("Entity<{0}>", Name);

                return _BaseType;
            }
            set { _BaseType = value; }
        }
        #endregion

        #region 生成属性
        /// <summary>实体类</summary>
        public CodeTypeDeclaration Class { get; set; }
        #endregion

        #region 方法
        /// <summary>创建实体类</summary>
        public void Create()
        {
            Class = new CodeTypeDeclaration(Name);
            Class.IsClass = true;
            Class.IsPartial = true;
            Class.TypeAttributes = TypeAttributes.Public;
            Class.AddSummary(Table.Description);

            // 特性
            Class.AddAttribute<SerializableAttribute>();
            Class.AddAttribute<DataObjectAttribute>();
            if (!Table.Description.IsNullOrWhiteSpace()) Class.AddAttribute<DescriptionAttribute>(Table.Description);
            if (!Table.DisplayName.IsNullOrWhiteSpace() && Table.DisplayName != Table.Name) Class.AddAttribute<DisplayNameAttribute>(Table.DisplayName);
            Class.AddAttribute<CompilerGeneratedAttribute>();

            // 索引和关系
            if (Table.Indexes != null && Table.Indexes.Count > 0)
            {
                foreach (var item in Table.Indexes)
                {
                    if (item.Columns.Length < 1) continue;

                    Class.AddAttribute<BindIndexAttribute>(item.Name, item.Unique, String.Join(",", item.Columns));
                }
            }
            //if (Table.Relations != null && Table.Relations.Count > 0)
            //{
            //    foreach (var item in Table.Relations)
            //    {
            //        Class.AddAttribute<BindRelationAttribute>(item.Column, item.Unique, item.RelationTable, item.RelationColumn);
            //    }
            //}

            // 绑定表
            Class.AddAttribute<BindTableAttribute>(Table.TableName, Table.Description, Table.ConnName ?? ConnName, Table.DbType, Table.IsView);

            // 基类
            Class.BaseTypes.Add(BaseType);
        }

        /// <summary>添加属性集合</summary>
        public void AddProperties()
        {
            if (Table.Columns == null || Table.Columns.Count < 1) return;

            var n = Class.Members.Count;
            foreach (var item in Table.Columns)
            {
                if (item.DataType == null) throw new XCodeException("[{0}]的[{1}]字段类型DataType不能为空！", Table.DisplayName, item.DisplayName);
                AddField(item);
                AddProperty(item);
            }
            Class.Members[n].StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "属性"));
            n = Class.Members.Count;
            Class.Members[n - 1].EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, null));
        }

        /// <summary>添加私有字段</summary>
        /// <param name="field">字段</param>
        public CodeMemberField AddField(IDataColumn field)
        {
            var f = new CodeMemberField();
            f.Attributes = MemberAttributes.Private;
            f.Name = "_" + field.Name;
            f.Type = new CodeTypeReference(field.DataType);
            Class.Members.Add(f);
            return f;
        }

        /// <summary>添加单个属性</summary>
        /// <param name="field">字段</param>
        public CodeMemberProperty AddProperty(IDataColumn field)
        {
            //String name = FieldNames[field.Name];
            var name = field.Name;

            var p = new CodeMemberProperty();
            p.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            p.Name = name;
            p.Type = new CodeTypeReference(field.DataType);
            p.AddSummary(field.Description);

            // 特性
            if (!field.Description.IsNullOrWhiteSpace()) p.AddAttribute<DescriptionAttribute>(field.Description);
            if (!field.DisplayName.IsNullOrWhiteSpace() && field.DisplayName != field.Name) p.AddAttribute<DisplayNameAttribute>(field.DisplayName);

            p.AddAttribute<DataObjectFieldAttribute>(field.PrimaryKey, field.Identity, field.Nullable, field.Length);
            p.AddAttribute<BindColumnAttribute>(field.ID, field.ColumnName, field.Description, field.Default == null ? null : field.Default, field.RawType, field.Precision, field.Scale, field.IsUnicode);

            p.HasGet = true;
            p.HasSet = true;

            var f = ("_" + p.Name).ToExp();
            p.GetStatements.Add(f.Return());

            var changing = "OnPropertyChanging".Invoke(p.Name, "$value");
            var changed = "OnPropertyChanged".Invoke(p.Name);

            p.SetStatements.Add(changing.IfTrue(f.Assign("$value"), changed.ToStat()));

            Class.Members.Add(p);
            return p;
        }

        /// <summary>添加索引器</summary>
        public CodeMemberProperty AddIndexs()
        {
            var p = new CodeMemberProperty();
            p.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            p.Name = "Item";
            p.Type = new CodeTypeReference(typeof(Object));
            p.Parameters.Add(new CodeParameterDeclarationExpression(typeof(String), "name"));
            p.AddSummary("获取/设置 字段值。");
            p.AddParamComment("name", "属性名");
            p.AddReturnComment("属性值");

            p.HasGet = true;
            p.HasSet = true;

            foreach (var item in Table.Columns)
            {
                var name = item.Name;

                // 取值
                var cond = "$name".Equal(name);
                p.GetStatements.Add(cond.IfTrue(("_" + name).Return()));

                // 设置值

                var type = typeof(Convert);
                var mi = type.GetMethod("To" + item.DataType.Name, new Type[] { typeof(Object) });
                CodeExpression ce = null;
                if (mi != null)
                    ce = typeof(Convert).ToExp().Invoke("To" + item.DataType.Name, "@value");
                else
                    ce = "@value".Cast(item.DataType);

                p.SetStatements.Add(cond.IfTrue(("_" + name).Assign(ce), new CodeMethodReturnStatement()));
            }
            // 取值
            p.GetStatements.Add("$base[$name]".Return());

            // 设置值
            p.SetStatements.Add("$base[$name]".Assign("$value"));

            p.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "获取/设置 字段值"));
            p.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, null));
            Class.Members.Add(p);

            return p;
        }
        #endregion

        #region 注释
        CodeCommentStatement AddComment(String doc, String comment)
        {
            return new CodeCommentStatement(String.Format("<{0}>{1}</{0}>", doc, comment), true);
        }

        CodeCommentStatement AddSummary(String comment)
        {
            return AddComment("summary", comment);
        }

        CodeCommentStatement AddParamComment(String name, String comment)
        {
            return new CodeCommentStatement(String.Format("<param name=\"{0}\">{1}</param>", name, comment), true);
        }
        #endregion

        #region 生成代码
        /// <summary>生成C#代码</summary>
        /// <returns></returns>
        public String GenerateCSharpCode()
        {
            var provider = CodeDomProvider.CreateProvider("CSharp");
            var options = new CodeGeneratorOptions();
            options.BracingStyle = "C";
            options.VerbatimOrder = true;
            using (var writer = new StringWriter())
            {
                provider.GenerateCodeFromType(Class, writer, options);

                var str = writer.ToString();

                // 去掉头部
                var dt = typeof(DateTime);
                str = str.Replace(dt.ToString(), dt.Name);

                return str;
            }
        }
        #endregion

        #region 辅助函数
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() { return Name; }
        #endregion
    }
}