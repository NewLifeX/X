using System;
using System.Collections.Generic;
using System.Text;
using XCode.DataAccessLayer;
using System.CodeDom;
using System.Reflection;
using System.CodeDom.Compiler;
using System.IO;
using System.ComponentModel;

namespace XCode.Code
{
    /// <summary>
    /// 实体类
    /// </summary>
    /// <remarks>提供由XTable生成实体类的支持</remarks>
    public class EntityClass
    {
        #region 属性
        private XTable _Table;
        /// <summary>表</summary>
        public XTable Table
        {
            get { return _Table; }
            set { _Table = value; }
        }

        private EntityAssembly _Assembly;
        /// <summary>实体程序集</summary>
        public EntityAssembly Assembly
        {
            get { return _Assembly; }
            set { _Assembly = value; }
        }
        #endregion

        #region 生成属性
        private CodeTypeDeclaration _Class;
        /// <summary>实体类</summary>
        public CodeTypeDeclaration Class
        {
            get { return _Class; }
            set { _Class = value; }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 创建实体类
        /// </summary>
        public void Create()
        {
            Class = new CodeTypeDeclaration(Table.Name);
            Class.IsClass = true;
            Class.IsPartial = true;
            Class.TypeAttributes = TypeAttributes.Public;
            //Class.Comments.Add(new CodeCommentStatement(Table.Description, true));
            Class.Comments.Add(AddSummary(Table.Description));

            // 特性
            Class.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(SerializableAttribute))));
            Class.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(DataObjectAttribute))));
            Class.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(DescriptionAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(Table.Description))));
            Class.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(DisplayNameAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(Table.Description))));
            Class.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(BindTableAttribute)),
                new CodeAttributeArgument(new CodePrimitiveExpression(Table.Name)),
                new CodeAttributeArgument("Description", new CodePrimitiveExpression(Table.Description)),
                new CodeAttributeArgument("ConnName", new CodePrimitiveExpression(Assembly.Dal.ConnName))
                ));

            // 基类
            Type type = typeof(Entity<>);
            //type=type.MakeGenericType(typeof())
            //Class.BaseTypes.Add(type);
            Class.BaseTypes.Add(String.Format("Entity<{0}>", Table.Name));

            Assembly.NameSpace.Types.Add(Class);
        }

        /// <summary>
        /// 添加属性集合
        /// </summary>
        public void AddProperties()
        {
            Int32 n = Class.Members.Count;
            foreach (XField item in Table.Fields)
            {
                AddField(item);
                AddProperty(item);
            }
            Class.Members[n].StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "属性"));
            n = Class.Members.Count;
            Class.Members[n - 1].EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, null));
        }

        /// <summary>
        /// 添加私有字段
        /// </summary>
        /// <param name="field"></param>
        public CodeMemberField AddField(XField field)
        {
            CodeMemberField f = new CodeMemberField();
            f.Attributes = MemberAttributes.Private;
            f.Name = "_" + field.Name;
            f.Type = new CodeTypeReference(field.DataType);
            Class.Members.Add(f);
            return f;
        }

        /// <summary>
        /// 添加单个属性
        /// </summary>
        /// <param name="field"></param>
        public CodeMemberProperty AddProperty(XField field)
        {
            CodeMemberProperty p = new CodeMemberProperty();
            p.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            p.Name = field.Name;
            p.Type = new CodeTypeReference(field.DataType);
            p.Comments.Add(AddSummary(field.Description));

            // 特性
            p.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(DescriptionAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(field.Description))));
            p.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(DisplayNameAttribute)), new CodeAttributeArgument(new CodePrimitiveExpression(field.Description))));
            p.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(DataObjectFieldAttribute)),
                new CodeAttributeArgument(new CodePrimitiveExpression(field.PrimaryKey)),
                new CodeAttributeArgument(new CodePrimitiveExpression(field.Identity)),
                new CodeAttributeArgument(new CodePrimitiveExpression(field.Nullable)),
                new CodeAttributeArgument(new CodePrimitiveExpression(field.Length))
               ));
            p.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(BindColumnAttribute)),
                new CodeAttributeArgument(new CodePrimitiveExpression(field.Name)),
                new CodeAttributeArgument("Description", new CodePrimitiveExpression(field.Description)),
                new CodeAttributeArgument("DefaultValue", new CodePrimitiveExpression(field.Default)),
                new CodeAttributeArgument("Order", new CodePrimitiveExpression(field.ID))
                ));

            p.HasGet = true;
            p.HasSet = true;

            p.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(null, "_" + field.Name)));

            CodeMethodInvokeExpression invokeExp = new CodeMethodInvokeExpression();
            invokeExp.Method = new CodeMethodReferenceExpression(null, "OnPropertyChange");
            invokeExp.Parameters.Add(new CodePrimitiveExpression(p.Name));
            invokeExp.Parameters.Add(new CodeVariableReferenceExpression("value"));

            CodeAssignStatement cas = new CodeAssignStatement();
            cas.Left = new CodeFieldReferenceExpression(null, "_" + p.Name);
            cas.Right = new CodeVariableReferenceExpression("value");

            p.SetStatements.Add(new CodeConditionStatement(invokeExp, cas));

            Class.Members.Add(p);
            return p;
        }

        /// <summary>
        /// 添加索引器
        /// </summary>
        public CodeMemberProperty AddIndexs()
        {
            CodeMemberProperty p = new CodeMemberProperty();
            p.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            p.Name = "Item";
            p.Type = new CodeTypeReference(typeof(Object));
            p.Parameters.Add(new CodeParameterDeclarationExpression(typeof(String), "name"));
            p.Comments.Add(AddSummary("获取/设置 字段值。"));
            p.Comments.Add(AddParamComment("name", "属性名"));
            p.Comments.Add(AddComment("return", "属性值"));

            p.HasGet = true;
            p.HasSet = true;

            foreach (XField item in Table.Fields)
            {
                // 取值
                CodeConditionStatement cond = new CodeConditionStatement();
                p.GetStatements.Add(cond);
                cond.Condition = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("name"), CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(item.Name));
                cond.TrueStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(null, "_" + item.Name)));

                // 设置值
                cond = new CodeConditionStatement();
                p.SetStatements.Add(cond);
                cond.Condition = new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("name"), CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(item.Name));

                Type type = typeof(Convert);
                MethodInfo mi = type.GetMethod("To" + item.DataType.Name, new Type[] { typeof(Object) });
                CodeExpression ce = null;
                if (mi != null)
                {
                    CodeMethodInvokeExpression mie = new CodeMethodInvokeExpression();
                    mie.Method = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(typeof(Convert)), "To" + item.DataType.Name);
                    mie.Parameters.Add(new CodeArgumentReferenceExpression("value"));
                    // _Name = Convert.ToString(value);
                    ce = mie;
                }
                else
                {
                    CodeCastExpression cce = new CodeCastExpression();
                    cce.TargetType = new CodeTypeReference(item.DataType);
                    cce.Expression = new CodeArgumentReferenceExpression("value");
                    ce = cce;
                }
                cond.TrueStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(null, "_" + item.Name), ce));

                // return;
                cond.TrueStatements.Add(new CodeMethodReturnStatement());
            }
            // 取值
            CodeMethodReturnStatement cmrs = new CodeMethodReturnStatement();
            cmrs.Expression = new CodeIndexerExpression(new CodeBaseReferenceExpression(), new CodeVariableReferenceExpression("name"));
            p.GetStatements.Add(cmrs);

            // 设置值
            CodeAssignStatement cas = new CodeAssignStatement();
            cas.Left = new CodeIndexerExpression(new CodeBaseReferenceExpression(), new CodeVariableReferenceExpression("name"));
            cas.Right = new CodeVariableReferenceExpression("value");
            p.SetStatements.Add(cas);

            p.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "获取/设置 字段值"));
            p.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, null));
            Class.Members.Add(p);

            return p;
        }

        /// <summary>
        /// 添加字段名类
        /// </summary>
        public void AddNames()
        {
            CodeTypeDeclaration cs = new CodeTypeDeclaration("_");
            cs.IsClass = true;
            cs.Attributes = MemberAttributes.Public;
            cs.Comments.Add(AddSummary("取得字段名的快捷方式"));

            foreach (XField item in Table.Fields)
            {
                CodeMemberField f = new CodeMemberField();
                f.Name = item.Name;
                f.Attributes = MemberAttributes.Public | MemberAttributes.Const;
                f.Type = new CodeTypeReference(typeof(String));
                f.InitExpression = new CodePrimitiveExpression(f.Name);
                f.Comments.Add(AddSummary(item.Description));
                cs.Members.Add(f);
            }

            cs.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "字段名"));
            cs.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, null));
            Class.Members.Add(cs);
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

        #region 缩进
        static String GetSpace(Int32 n)
        {
            return new String(' ', n);
        }

        static String GetTabSpace(Int32 n)
        {
            return GetSpace(n * 4);
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
                provider.GenerateCodeFromType(Class, writer, options);
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
    }
}
