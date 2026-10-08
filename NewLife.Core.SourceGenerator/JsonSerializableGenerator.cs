using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace NewLife.SourceGenerator;

/// <summary>Json 成员访问生成器。扫描 <c>[JsonSerializable(typeof(T))]</c> 标记，为 T 的公开实例属性与字段生成静态读写委托注册，JsonHelper 序列化时优先使用，避免反射与表达式编译</summary>
/// <remarks>
/// 依赖 System.Text.Json 的 <c>JsonSerializableAttribute</c>（net5+ 共享框架自带，无需额外包）。
/// 只生成可直接静态访问的成员（public、非只读/init-only 赋值、非元组/指针/ref struct、类型可访问）；
/// 未标记类型与不可生成成员保持原反射路径，行为不变。
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class JsonSerializableGenerator : IIncrementalGenerator
{
    #region 常量
    // 全限定名 + 关键字转义；去掉 UseSpecialTypes，保证使用 Int32/String 等正式类型名（与库编码规范一致）
    private static readonly SymbolDisplayFormat _format = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);
    #endregion

    #region 生成入口
    /// <summary>初始化增量生成管线</summary>
    /// <param name="context">生成上下文</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var lines = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "System.Text.Json.Serialization.JsonSerializableAttribute",
                predicate: static (node, token) => true,
                transform: static (ctx, token) => Extract(ctx, token))
            .SelectMany(static (items, token) => items)
            .Where(static x => x != null);

        var combined = context.CompilationProvider.Combine(lines.Collect());

        context.RegisterSourceOutput(combined, static (spc, pair) => Emit(spc, pair.Left, pair.Right));
    }
    #endregion

    #region 提取
    // 提取标记类型的所有可生成成员注册行（值语义字符串，增量缓存友好）
    private static ImmutableArray<String?> Extract(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        var builder = ImmutableArray.CreateBuilder<String?>();
        var compilation = context.SemanticModel.Compilation;

        foreach (var attribute in context.Attributes)
        {
            if (attribute.ConstructorArguments.Length == 0) continue;
            if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol type) continue;

            AddMembers(builder, type, compilation);
        }

        return builder.ToImmutable();
    }

    // 遍历类型及其基类的公开实例成员，生成访问行；派生类同名成员优先（先遇到先保留）
    private static void AddMembers(ImmutableArray<String?>.Builder builder, INamedTypeSymbol type, Compilation compilation)
    {
        if (type.IsUnboundGenericType) return;
        if (!compilation.IsSymbolAccessibleWithin(type, compilation.Assembly)) return;

        var typeName = type.ToDisplayString(_format);
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
                    if (!IsAccessible(property.Type, compilation)) continue;

                    memberType = property.Type.ToDisplayString(_format);
                    if (property.GetMethod?.DeclaredAccessibility == Accessibility.Public)
                        getter = $"o => (({typeName})o).{Escape(property.Name)}";

                    // init-only/required 赋值在外部不可用，保持原路径
                    if (property.SetMethod?.DeclaredAccessibility == Accessibility.Public && !property.SetMethod.IsInitOnly)
                        setter = BuildSetter(typeName, property.Name, memberType, property.Type);
                }
                else if (member is IFieldSymbol field)
                {
                    if (field.IsConst) continue;
                    if (!IsAccessible(field.Type, compilation)) continue;

                    memberType = field.Type.ToDisplayString(_format);
                    getter = $"o => (({typeName})o).{Escape(field.Name)}";

                    if (!field.IsReadOnly)
                        setter = BuildSetter(typeName, field.Name, memberType, field.Type);
                }
                else
                {
                    continue;
                }

                if (getter == null && setter == null) continue;
                if (!names.Add(member.Name)) continue;

                var getterText = getter ?? "null";
                var setterText = setter ?? "null";
                builder.Add($"global::NewLife.Serialization.JsonMemberTable.Register(typeof({typeName}), \"{member.Name}\", {getterText}, {setterText});");
            }
        }
    }

    // 赋值委托：值类型成员补 null 兜底（与原 SetValue 的容错语义一致）
    private static String BuildSetter(String typeName, String memberName, String memberType, ITypeSymbol type)
    {
        var name = Escape(memberName);
        if (type.IsValueType && type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
            return $"(o, v) => (({typeName})o).{name} = v == null ? default({memberType}) : ({memberType})v!";

        return $"(o, v) => (({typeName})o).{name} = ({memberType})v!";
    }

    // 成员类型可生成：非元组、非指针、非 ref struct（否则无法装箱进委托或需要额外类型引用）
    private static Boolean IsAccessible(ITypeSymbol type, Compilation compilation)
    {
        if (type.TypeKind == TypeKind.Pointer || type.TypeKind == TypeKind.FunctionPointer) return false;
        if (type.IsRefLikeType) return false;
        if (type is INamedTypeSymbol named && named.IsTupleType) return false;

        return compilation.IsSymbolAccessibleWithin(type, compilation.Assembly);
    }

    // 关键字成员名需要 @ 转义后访问
    private static String Escape(String name)
        => Microsoft.CodeAnalysis.CSharp.SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;
    #endregion

    #region 产出
    // 输出生成源码。仅 .NET 5+（含模块初始化器）且在可用语言版本下生成
    private static void Emit(SourceProductionContext context, Compilation compilation, ImmutableArray<String?> items)
    {
        if (items.IsDefaultOrEmpty) return;

        var set = new SortedSet<String>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (!String.IsNullOrEmpty(item)) set.Add(item!);
        }
        if (set.Count == 0) return;

        var tree = compilation.SyntaxTrees.FirstOrDefault();
        if (tree?.Options is not CSharpParseOptions options) return;
        if (!options.PreprocessorSymbolNames.Contains("NET5_0_OR_GREATER")) return;

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated/>");
        if ((Int32)options.LanguageVersion >= (Int32)LanguageVersion.CSharp8) builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("namespace NewLife.SourceGenerator.Generated");
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>源生成的 Json 成员访问注册。NewLife.Core 源生成器自动生成，请勿修改</summary>");
        builder.AppendLine("    internal static class JsonMemberRegistrar");
        builder.AppendLine("    {");
        builder.AppendLine("        [global::System.Runtime.CompilerServices.ModuleInitializer]");
        builder.AppendLine("        internal static void Initialize()");
        builder.AppendLine("        {");
        foreach (var line in set)
        {
            builder.Append("            ").AppendLine(line);
        }
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        context.AddSource("JsonMemberRegistrar.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }
    #endregion
}
