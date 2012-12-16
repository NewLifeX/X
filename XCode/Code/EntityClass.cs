using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using XCode.DataAccessLayer;
using XCode.Exceptions;

namespace XCode.Code
{
    /// <summary>实体类</summary>
    /// <remarks>提供由IDataTable生成实体类的支持</remarks>
    public class EntityClass
    {
        #region 属性
        private String _Name;
        /// <summary>类名</summary>
        public String Name { get { return _Name; } set { _Name = value; } }

        private String _ConnName;
        /// <summary>连接名</summary>
        public String ConnName { get { return _ConnName; } set { _ConnName = value; } }

        private IDataTable _Table;
        /// <summary>表</summary>
        public IDataTable Table { get { return _Table; } set { _Table = value; } }

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

        //private EntityAssembly _Assembly;
        ///// <summary>实体程序集</summary>
        //public EntityAssembly Assembly { get { return _Assembly; } set { _Assembly = value; } }
        #endregion

        #region 生成属性
        private CodeTypeDeclaration _Class;
        /// <summary>实体类</summary>
        public CodeTypeDeclaration Class { get { return _Class; } set { _Class = value; } }
        #endregion

        #region 方法
        /// <summary>创建实体类</summary>
        public void Create()
        {
            Class = new CodeTypeDeclaration(Name);
            Class.IsClass = true;
            Class.IsPartial = true;
            Class.TypeAttributes = TypeAttributes.Public;
            //Class.Comments.Add(new CodeCommentStatement(Table.Description, true));
            //Class.Comments.Add(AddSummary(Table.Description));
            Class.AddSummary(Table.Description);

            // 特性
            //Class.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(SerializableAttribute))));
            //Class.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(DataObjectAttribute))));
            Class.AddAttribute<SerializableAttribute>();
            Class.AddAttribute<DataObjectAttribute>();
            //if (!Table.Description.IsNullOrWhiteSpace())
            //{
            //    Class.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(DescriptionAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(Table.Description))));
            //    Class.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(DisplayNameAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(Table.Description))));
            //}
            if (!Table.Description.IsNullOrWhiteSpace()) Class.AddAttribute<DescriptionAttribute>(Table.Description);
            if (!Table.DisplayName.IsNullOrWhiteSpace()) Class.AddAttribute<DisplayNameAttribute>(Table.DisplayName);
            //Class.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(CompilerGeneratedAttribute))));
            Class.AddAttribute<CompilerGeneratedAttribute>();

            // 索引和关系
            if (Table.Indexes != null && Table.Indexes.Count > 0)
            {
                foreach (var item in Table.Indexes)
                {
                    if (item.Columns == null || item.Columns.Length < 1) continue;

                    //Class.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(BindIndexAttribute)),
                    //    new CodeAttributeArgument(new CodePrimitiveExpression(item.Name)),
                    //    new CodeAttributeArgument(new CodePrimitiveExpression(item.Unique)),
                    //    new CodeAttributeArgument(new CodePrimitiveExpression(String.Join(",", item.Columns)))
                    //    ));
                    Class.AddAttribute<BindIndexAttribute>(item.Name, item.Unique, String.Join(",", item.Columns));
                }
            }
            if (Table.Relations != null && Table.Relations.Count > 0)
            {
                foreach (var item in Table.Relations)
                {
                    //Class.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(BindRelationAttribute)),
                    //    new CodeAttributeArgument(new CodePrimitiveExpression(item.Column)),
                    //    new CodeAttributeArgument(new CodePrimitiveExpression(item.Unique)),
                    //    new CodeAttributeArgument(new CodePrimitiveExpression(item.RelationTable)),
                    //    new CodeAttributeArgument(new CodePrimitiveExpression(item.RelationColumn))
                    //  ));
                    Class.AddAttribute<BindRelationAttribute>(item.Column, item.Unique, item.RelationTable, item.RelationColumn);
                }
            }

            // 绑定表
            //Class.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(BindTableAttribute)),
            //    new CodeAttributeArgument(new CodePrimitiveExpression(Table.TableName)),
            //    new CodeAttributeArgument("Description", new CodePrimitiveExpression(Table.Description)),
            //    new CodeAttributeArgument("ConnName", new CodePrimitiveExpression(Assembly.ConnName)),
            //    new CodeAttributeArgument("DbType", new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(DatabaseType)), Table.DbType.ToString())),
            //    new CodeAttributeArgument("IsView", new CodePrimitiveExpression(Table.IsView))
            //    ));
            Class.AddAttribute<BindTableAttribute>(Table.TableName, Table.Description, ConnName, Table.DbType, Table.IsView);

            // 基类
            //Type type = typeof(Entity<>);
            //type=type.MakeGenericType(typeof())
            //Class.BaseTypes.Add(type);
            Class.BaseTypes.Add(BaseType);

            //Assembly.NameSpace.Types.Add(Class);
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
        /// <param name="field"></param>
        public CodeMemberField AddField(IDataColumn field)
        {
            var f = new CodeMemberField();
            f.Attributes = MemberAttributes.Private;
            //f.Name = "_" + field.Name;
            //f.Name = "_" + FieldNames[field.Name];
            f.Name = "_" + field.Name;
            f.Type = new CodeTypeReference(field.DataType);
            Class.Members.Add(f);
            return f;
        }

        /// <summary>添加单个属性</summary>
        /// <param name="field"></param>
        public CodeMemberProperty AddProperty(IDataColumn field)
        {
            //String name = FieldNames[field.Name];
            var name = field.Name;

            var p = new CodeMemberProperty();
            p.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            p.Name = name;
            p.Type = new CodeTypeReference(field.DataType);
            //p.Comments.Add(AddSummary(field.Description));
            p.AddSummary(field.Description);

            // 特性
            //if (!field.Description.IsNullOrWhiteSpace())
            //{
            //    p.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(DescriptionAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(field.Description))));
            //    p.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(DisplayNameAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(field.Description))));
            //}
            if (!field.Description.IsNullOrWhiteSpace()) p.AddAttribute<DescriptionAttribute>(field.Description);
            if (!field.DisplayName.IsNullOrWhiteSpace()) p.AddAttribute<DisplayNameAttribute>(field.DisplayName);

            //p.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(DataObjectFieldAttribute)),
            //    new CodeAttributeArgument(new CodePrimitiveExpression(field.PrimaryKey)),
            //    new CodeAttributeArgument(new CodePrimitiveExpression(field.Identity)),
            //    new CodeAttributeArgument(new CodePrimitiveExpression(field.Nullable)),
            //    new CodeAttributeArgument(new CodePrimitiveExpression(field.Length))
            //   ));
            //p.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(BindColumnAttribute)),
            //    new CodeAttributeArgument(new CodePrimitiveExpression(field.ID)),
            //    new CodeAttributeArgument(new CodePrimitiveExpression(field.ColumnName)),
            //    new CodeAttributeArgument(new CodePrimitiveExpression(field.Description)),
            //    new CodeAttributeArgument(new CodePrimitiveExpression(field.Default == null ? null : field.Default)),
            //    new CodeAttributeArgument(new CodePrimitiveExpression(field.RawType)),
            //    new CodeAttributeArgument(new CodePrimitiveExpression(field.Precision)),
            //    new CodeAttributeArgument(new CodePrimitiveExpression(field.Scale)),
            //    new CodeAttributeArgument(new CodePrimitiveExpression(field.IsUnicode))
            //    ));
            p.AddAttribute<DataObjectFieldAttribute>(field.PrimaryKey, field.Identity, field.Nullable, field.Length);
            p.AddAttribute<BindColumnAttribute>(field.ID, field.ColumnName, field.Description, field.Default == null ? null : field.Default, field.RawType, field.Precision, field.Scale, field.IsUnicode);

            p.HasGet = true;
            p.HasSet = true;

            var f = ("_" + p.Name).ToExp();
            //p.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(null, "_" + p.Name)));
            p.GetStatements.Add(f.Return());

            //var changing = new CodeMethodInvokeExpression(null, "OnPropertyChanging", new CodePrimitiveExpression(p.Name), new CodeVariableReferenceExpression("value"));
            //var changed = new CodeMethodInvokeExpression(null, "OnPropertyChanged", new CodePrimitiveExpression(p.Name));

            var changing = "OnPropertyChanging".Invoke(p.Name, "$value");
            var changed = "OnPropertyChanged".Invoke(p.Name);

            //var cas = new CodeAssignStatement();
            //cas.Left = new CodeFieldReferenceExpression(null, "_" + p.Name);
            //cas.Right = new CodeVariableReferenceExpression("value");
            //var cas = f.Assign("$value".ToExp());

            //p.SetStatements.Add(new CodeConditionStatement(changing, cas, changed.ToStat()));
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
            //p.Comments.Add(AddSummary("获取/设置 字段值。"));
            //p.Comments.Add(AddParamComment("name", "属性名"));
            //p.Comments.Add(AddComment("return", "属性值"));
            p.AddSummary("获取/设置 字段值。");
            p.AddParamComment("name", "属性名");
            p.AddReturnComment("属性值");

            p.HasGet = true;
            p.HasSet = true;

            foreach (var item in Table.Columns)
            {
                //String name = FieldNames[item.Name];
                var name = item.Name;

                // 取值
                //var cond = new CodeConditionStatement();
                //p.GetStatements.Add(cond);
                //cond.Condition = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("name"), CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(name));
                //cond.TrueStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(null, "_" + name)));
                var cond = "$name".Equal(name);
                p.GetStatements.Add(cond.IfTrue(("_" + name).Return()));

                // 设置值
                //cond = new CodeConditionStatement();
                //p.SetStatements.Add(cond);
                //cond.Condition = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("name"), CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(name));

                var type = typeof(Convert);
                var mi = type.GetMethod("To" + item.DataType.Name, new Type[] { typeof(Object) });
                CodeExpression ce = null;
                if (mi != null)
                {
                    //var mie = new CodeMethodInvokeExpression();
                    //mie.Method = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(typeof(Convert)), "To" + item.DataType.Name);
                    //mie.Parameters.Add(new CodeArgumentReferenceExpression("value"));
                    //// _Name = Convert.ToString(value);
                    //ce = mie;

                    ce = typeof(Convert).ToExp().Invoke("To" + item.DataType.Name, "@value");
                }
                else
                {
                    //var cce = new CodeCastExpression();
                    //cce.TargetType = new CodeTypeReference(item.DataType);
                    //cce.Expression = new CodeArgumentReferenceExpression("value");
                    //ce = cce;

                    ce = "@value".Cast(item.DataType);
                }
                //cond.TrueStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(null, "_" + name), ce));

                //// return;
                //cond.TrueStatements.Add(new CodeMethodReturnStatement());

                p.SetStatements.Add(cond.IfTrue(("_" + name).Assign(ce), new CodeMethodReturnStatement()));
            }
            // 取值
            //var cmrs = new CodeMethodReturnStatement();
            //cmrs.Expression = new CodeIndexerExpression(new CodeBaseReferenceExpression(), new CodeVariableReferenceExpression("name"));
            //p.GetStatements.Add(cmrs);
            p.GetStatements.Add("$base[$name]".Return());

            // 设置值
            //var cas = new CodeAssignStatement();
            //cas.Left = new CodeIndexerExpression(new CodeBaseReferenceExpression(), new CodeVariableReferenceExpression("name"));
            //cas.Right = new CodeVariableReferenceExpression("value");
            //p.SetStatements.Add(cas);
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

        #region 辅助函数
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return Name; }
        #endregion
    }
}