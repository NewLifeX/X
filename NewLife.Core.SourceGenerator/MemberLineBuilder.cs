using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace NewLife.SourceGenerator;

/// <summary>成员访问行构建器。为类型及其基类的公开实例属性/字段生成 MemberAccessorTable 注册行，供 Json 序列化与配置绑定生成器共用</summary>
internal static class MemberLineBuilder
{
    #region 常量
    // 全限定名 + 关键字转义；去掉 UseSpecialTypes，保证使用 Int32/String 等正式类型名（与库编码规范一致）
    public static readonly SymbolDisplayFormat Format = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);
    #endregion

    #region 方法
    /// <summary>为类型及其基类的公开实例成员追加注册行。派生类同名成员优先（先遇到先保留）</summary>
    /// <param name="lines">注册行集合</param>
    /// <param name="type">目标类型</param>
    /// <param name="compilation">编译上下文</param>
    public static void AddMembers(ICollection<String?> lines, INamedTypeSymbol type, Compilation compilation)
    {
        if (type.IsUnboundGenericType) return;
        if (HasTypeParameters(type)) return;
        if (!compilation.IsSymbolAccessibleWithin(type, compilation.Assembly)) return;

        var typeName = type.ToDisplayString(Format);
        var names = new HashSet<String>(StringComparer.Ordinal);

        for (var current = type; current != null; current = current.BaseType)
        {
            if (current.SpecialType != SpecialType.None) break;

            foreach (var member in current.GetMembers())
            {
                if (member.IsStatic || member.IsImplicitlyDeclared) continue;
                if (member.DeclaredAccessibility != Accessibility.Public) continue;

                String? memberType;
                String? getter = null;
                String? setter = null;

                if (member is IPropertySymbol property)
                {
                    if (property.IsIndexer) continue;
                    if (!IsGeneratable(property.Type, compilation)) continue;

                    memberType = property.Type.ToDisplayString(Format);
                    if (property.GetMethod?.DeclaredAccessibility == Accessibility.Public)
                        getter = $"o => (({typeName})o).{Escape(property.Name)}";

                    // init-only/required 赋值在外部不可用，保持原路径
                    if (property.SetMethod?.DeclaredAccessibility == Accessibility.Public && !property.SetMethod.IsInitOnly)
                        setter = BuildSetter(typeName, property.Name, memberType);
                }
                else if (member is IFieldSymbol field)
                {
                    if (field.IsConst) continue;
                    if (!IsGeneratable(field.Type, compilation)) continue;

                    memberType = field.Type.ToDisplayString(Format);
                    getter = $"o => (({typeName})o).{Escape(field.Name)}";

                    if (!field.IsReadOnly)
                        setter = BuildSetter(typeName, field.Name, memberType);
                }
                else
                {
                    continue;
                }

                if (getter == null && setter == null) continue;
                if (!names.Add(member.Name)) continue;

                var getterText = getter ?? "null";
                var setterText = setter ?? "null";
                lines.Add($"global::NewLife.Reflection.MemberAccessorTable.Register(typeof({typeName}), \"{member.Name}\", {getterText}, {setterText});");
            }
        }
    }

    // 赋值委托：值统一经 ChangeType<T> 转换（同类型快路径、null 安全），与 Reflect.SetValue 的原语义一致
    private static String BuildSetter(String typeName, String memberName, String memberType)
        => $"(o, v) => (({typeName})o).{Escape(memberName)} = global::NewLife.Reflection.Reflect.ChangeType<{memberType}>(v)!";

    // 成员类型可生成：非类型参数、非元组、非指针、非 ref struct（否则无法装箱进委托或需要额外类型引用）
    private static Boolean IsGeneratable(ITypeSymbol type, Compilation compilation)
    {
        if (HasTypeParameters(type)) return false;
        if (type.TypeKind == TypeKind.Pointer || type.TypeKind == TypeKind.FunctionPointer) return false;
        if (type.IsRefLikeType) return false;
        if (type is INamedTypeSymbol named && named.IsTupleType) return false;

        return compilation.IsSymbolAccessibleWithin(type, compilation.Assembly);
    }

    // 关键字成员名需要 @ 转义后访问
    private static String Escape(String name)
        => Microsoft.CodeAnalysis.CSharp.SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;

    // 含未绑定类型参数（开放泛型的类型/成员无法在生成代码中表达）
    private static Boolean HasTypeParameters(ITypeSymbol type)
    {
        switch (type)
        {
            case ITypeParameterSymbol:
                return true;
            case IArrayTypeSymbol array:
                return HasTypeParameters(array.ElementType);
            case IPointerTypeSymbol pointer:
                return HasTypeParameters(pointer.PointedAtType);
            case INamedTypeSymbol named:
                if (named.ContainingType != null && HasTypeParameters(named.ContainingType)) return true;

                foreach (var argument in named.TypeArguments)
                {
                    if (HasTypeParameters(argument)) return true;
                }
                return false;
            default:
                return false;
        }
    }
    #endregion
}
