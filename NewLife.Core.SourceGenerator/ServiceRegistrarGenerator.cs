using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NewLife.SourceGenerator;

/// <summary>服务注册扫描生成器。扫描容器注册调用点（AddTransient&lt;TService, TImpl&gt; 等），为"唯一公开无参构造"的实现类型生成无参工厂注册，供容器解析时优先使用，消除反射构造与表达式编译</summary>
/// <remarks>
/// 只收录与容器原构造语义完全一致的实现类型（唯一公开无参构造），有参构造注入、实例注册、工厂注册等场景保持原路径。
/// 生成代码依赖运行时接口 NewLife.Model.ServiceFactoryTable 与 .NET 5+ 的模块初始化器，低版本目标不生成。
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class ServiceRegistrarGenerator : IIncrementalGenerator
{
    #region 常量
    // 构造注册方法名白名单：双泛型（TService, TImpl）形式
    private static readonly HashSet<String> _dualNames = new(StringComparer.Ordinal)
    {
        "AddTransient", "TryAddTransient", "AddSingleton", "TryAddSingleton", "AddScoped", "TryAddScoped",
    };

    // 单泛型构造注册白名单：仅 AddTransient<T>/AddScoped<T> 语义为"以自身为实现注册"；
    // AddSingleton<T>/TryAddXxx<T> 是实例重载（可选 instance 参数），不产出工厂
    private static readonly HashSet<String> _selfNames = new(StringComparer.Ordinal)
    {
        "AddTransient", "AddScoped",
    };
    #endregion

    #region 生成入口
    /// <summary>初始化增量生成管线</summary>
    /// <param name="context">生成上下文</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
            .CreateSyntaxProvider(IsCandidate, Extract)
            .Where(static x => x != null);

        var combined = context.CompilationProvider.Combine(candidates.Collect());

        context.RegisterSourceOutput(combined, static (spc, pair) => Emit(spc, pair.Left, pair.Right));
    }
    #endregion

    #region 提取
    // 廉价语法谓词：只放行"零实参的注册名调用"与"两个 typeof 实参的注册名调用"
    // 廉价语法谓词：只放行"零实参的注册名调用"与"两个 typeof 实参的注册名调用"
    private static Boolean IsCandidate(SyntaxNode node, CancellationToken token)
    {
        if (node is not InvocationExpressionSyntax inv) return false;
        if (inv.Expression is not MemberAccessExpressionSyntax member) return false;

        var count = inv.ArgumentList.Arguments.Count;
        if (count == 0)
        {
            if (member.Name is not GenericNameSyntax generic) return false;

            var name = generic.Identifier.Text;
            return _dualNames.Contains(name) || _selfNames.Contains(name);
        }
        if (count == 2)
        {
            if (member.Name is not SimpleNameSyntax simple) return false;
            if (!_dualNames.Contains(simple.Identifier.Text)) return false;

            return inv.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax;
        }

        return false;
    }

    // 语义提取：确认是容器注册扩展方法，取实现类型；返回全限定名（增量缓存友好，不缓存符号）
    private static String? Extract(GeneratorSyntaxContext context, CancellationToken token)
    {
        var inv = (InvocationExpressionSyntax)context.Node;
        var model = context.SemanticModel;
        if (model.GetSymbolInfo(inv, token).Symbol is not IMethodSymbol method) return null;

        // 扩展方法调用返回 reduced 符号（参数不含 this），取原始方法判定接收者，避免误扫用户同名方法
        var original = method.ReducedFrom ?? method;
        if (!original.IsExtensionMethod || original.Parameters.Length == 0) return null;
        if (original.Parameters[0].Type.Name != "IObjectContainer") return null;

        INamedTypeSymbol? impl = null;
        if (method.IsGenericMethod && inv.ArgumentList.Arguments.Count == 0)
        {
            var arguments = method.TypeArguments;
            if (arguments.Length == 2 && _dualNames.Contains(method.Name))
                impl = arguments[1] as INamedTypeSymbol;
            else if (arguments.Length == 1 && _selfNames.Contains(method.Name))
                impl = arguments[0] as INamedTypeSymbol;
        }
        else if (!method.IsGenericMethod && inv.ArgumentList.Arguments.Count == 2 && _dualNames.Contains(method.Name))
        {
            // AddXxx(typeof(IService), typeof(Impl)) 形式，仅处理字面量 typeof
            if (inv.ArgumentList.Arguments[1].Expression is TypeOfExpressionSyntax typeOf)
                impl = model.GetTypeInfo(typeOf.Type, token).Type as INamedTypeSymbol;
        }

        if (impl == null || !IsGeneratable(impl, model.Compilation)) return null;

        return impl.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    // 可生成条件：唯一公开无参构造（与容器"选参数最多的可用构造"原路径语义一致），且当前程序集可访问
    private static Boolean IsGeneratable(INamedTypeSymbol impl, Compilation compilation)
    {
        if (impl.TypeKind != TypeKind.Class || impl.IsStatic || impl.IsAbstract) return false;
        if (impl.IsUnboundGenericType) return false;

        var hasPublicCtor = false;
        foreach (var ctor in impl.InstanceConstructors)
        {
            if (ctor.DeclaredAccessibility != Accessibility.Public) continue;
            if (ctor.Parameters.Length > 0) return false;

            hasPublicCtor = true;
        }
        if (!hasPublicCtor) return false;

        // 生成代码位于当前程序集，保证类型引用与构造调用均合法（避免生成编译错误）
        return compilation.IsSymbolAccessibleWithin(impl, compilation.Assembly);
    }
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
        builder.AppendLine("    /// <summary>源生成的服务工厂注册。NewLife.Core 源生成器自动生成，请勿修改</summary>");
        builder.AppendLine("    internal static class ServiceFactoryRegistrar");
        builder.AppendLine("    {");
        builder.AppendLine("        [global::System.Runtime.CompilerServices.ModuleInitializer]");
        builder.AppendLine("        internal static void Initialize()");
        builder.AppendLine("        {");
        foreach (var name in set)
        {
            builder.Append("            global::NewLife.Model.ServiceFactoryTable.Register(typeof(")
                   .Append(name)
                   .Append("), () => new ")
                   .Append(name)
                   .AppendLine("());");
        }
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        context.AddSource("ServiceFactoryRegistrar.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }
    #endregion
}
