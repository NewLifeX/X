using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom;

#if NET4
using System.Linq;
#else
using NewLife.Linq;
#endif

namespace XCode.Code
{
    static class CodeDomHelper
    {
        public static CodeExpression ToExp(this Object p)
        {
            if (p == null) return new CodePrimitiveExpression(p);

            if (p is CodeExpression) return p as CodeExpression;
            if (p is Type) return new CodeTypeReferenceExpression(p as Type);

            var str = p.ToString();
            if (str == "") return new CodePrimitiveExpression(p);

            if (str[0] == '_') return new CodeFieldReferenceExpression(null, str);

            if (str[0] == '@') return new CodeArgumentReferenceExpression(str.Substring(1));

            if (str[0] == '$')
            {
                var name = str.Substring(1);
                if (name == "this")
                    return new CodeThisReferenceExpression();
                else if (name == "base")
                    return new CodeBaseReferenceExpression();
                else if (name[name.Length - 1] == ']')
                {
                    var idx = name.IndexOf('[');
                    if (idx > 0)
                    {
                        return new CodeIndexerExpression(str.Substring(0, idx + 1).ToExp(), name.Substring(idx + 1, name.Length - idx - 2).ToExp());
                    }
                }

                return new CodeVariableReferenceExpression(name);
            }

            if (p.GetType().IsEnum) return new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(p.GetType()), p.ToString());

            return new CodePrimitiveExpression(p);
        }

        public static CodeTypeMember AddAttribute<TAttribute>(this CodeTypeMember member, params Object[] ps)
        {
            //var cs = ps.Select(p =>
            //{
            //    if (p != null && p.GetType().IsEnum)
            //        return new CodeAttributeArgument(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(p.GetType()), p.ToString()));
            //    else
            //        return new CodeAttributeArgument(new CodePrimitiveExpression(p));
            //}).ToArray();
            var cs = ps.Select(p => new CodeAttributeArgument(p.ToExp())).ToArray();
            member.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(TAttribute)), cs));

            return member;
        }

        public static CodeCompileUnit AddAttribute<TAttribute>(this CodeCompileUnit unit, params Object[] ps)
        {
            var cs = ps.Select(p => new CodeAttributeArgument(p.ToExp())).ToArray();
            unit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(TAttribute)), cs));

            return unit;
        }

        public static CodeMethodInvokeExpression Invoke(this String methodName, params Object[] ps)
        {
            var cs = ps.Select(p => p.ToExp()).ToArray();
            return new CodeMethodInvokeExpression(null, methodName, cs);
        }

        public static CodeMethodInvokeExpression Invoke(this CodeExpression targetObject, String methodName, params Object[] ps)
        {
            var cs = ps.Select(p => p.ToExp()).ToArray();
            return new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(targetObject, methodName), cs);
        }

        public static CodeStatement ToStat(this CodeExpression exp)
        {
            return new CodeExpressionStatement(exp);
        }

        public static CodeMethodReturnStatement Return(this Object exp)
        {
            return new CodeMethodReturnStatement(exp.ToExp());
        }

        public static CodeAssignStatement Assign(this CodeExpression left, Object right)
        {
            return new CodeAssignStatement(left, right.ToExp());
        }

        public static CodeAssignStatement Assign(this String left, Object right)
        {
            return new CodeAssignStatement(left.ToExp(), right.ToExp());
        }

        public static CodeExpression Equal(this CodeExpression left, Object right)
        {
            return new CodeBinaryOperatorExpression(left, CodeBinaryOperatorType.ValueEquality, right.ToExp());
        }

        public static CodeExpression Equal(this String left, Object right)
        {
            return new CodeBinaryOperatorExpression(left.ToExp(), CodeBinaryOperatorType.ValueEquality, right.ToExp());
        }

        public static CodeConditionStatement IfTrue(this CodeExpression condition, params  CodeStatement[] trueStatements)
        {
            return new CodeConditionStatement(condition, trueStatements);
        }

        public static CodeCastExpression Cast(this Object exp, Type targetType)
        {
            return new CodeCastExpression(new CodeTypeReference(targetType), exp.ToExp());
        }

        #region 注释
        public static CodeTypeMember AddComment(this CodeTypeMember member, String name, String comment)
        {
            member.Comments.Add(new CodeCommentStatement(String.Format("<{0}>{1}</{0}>", name, comment), true));

            return member;
        }

        public static CodeTypeMember AddSummary(this CodeTypeMember member, String comment)
        {
            member.AddComment("summary", comment);

            return member;
        }

        public static CodeTypeMember AddParamComment(this CodeTypeMember member, String name, String comment)
        {
            member.Comments.Add(new CodeCommentStatement(String.Format("<param name=\"{0}\">{1}</param>", name, comment), true));

            return member;
        }

        public static CodeTypeMember AddReturnComment(this CodeTypeMember member, String comment)
        {
            member.AddComment("return", comment);

            return member;
        }
        #endregion
    }
}