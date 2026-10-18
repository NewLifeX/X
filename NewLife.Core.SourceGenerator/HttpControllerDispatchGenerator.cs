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

/// <summary>Http 控制器静态分发生成器。扫描 <c>MapController&lt;T&gt;()</c> / <c>MapController(typeof(T))</c> 注册调用点，为可安全静态化的公开方法生成"方法名 → 强类型调用"委托并注册到 <c>HttpDispatchTable</c>，请求分发时优先命中，消除方法查找、参数绑定与调用的反射，NativeAOT 友好</summary>
/// <remarks>
/// 参数绑定语义镜像 <c>ParameterBinder.Bind</c>：按名取值并转换 → 显式默认值 → <c>IHttpContext</c>/<c>IServiceProvider</c> 注入 → 缺失时值类型抛必填参数异常、引用类型留空；单参数方法在取值为空时再按整份参数字典反序列化。
/// 仅 .NET 5+（模块初始化器可用）且方法形态可完全静态化时生成，其余场景保持原反射路径：同名多候选（重载/大小写冲突）、泛型方法、ref/out/params 参数、ValueTask 返回值、含类型参数或不可访问的成员类型一律跳过。
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class HttpControllerDispatchGenerator : IIncrementalGenerator
{
    #region 常量
    private const String ControllerMethodName = "MapController";
    private const String ControllerNamespace = "NewLife.Http";
    private const String ControllerTypeName = "HttpServer";
    private const Char TypeSeparator = '\u0000';
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
    // 廉价语法谓词：只放行名为 MapController 的成员调用
    private static Boolean IsCandidate(SyntaxNode node, CancellationToken token)
    {
        if (node is not InvocationExpressionSyntax inv) return false;
        if (inv.Expression is not MemberAccessExpressionSyntax member) return false;

        return member.Name is SimpleNameSyntax simple && simple.Identifier.Text == ControllerMethodName;
    }

    // 语义提取：确认是 HttpServer.MapController，取控制器类型并生成该控制器的分发代码片段（片段形如"类型全名 + 成员代码"，字符串模型对增量缓存友好）
    private static String? Extract(GeneratorSyntaxContext context, CancellationToken token)
    {
        var inv = (InvocationExpressionSyntax)context.Node;
        var model = context.SemanticModel;
        if (model.GetSymbolInfo(inv, token).Symbol is not IMethodSymbol method) return null;
        if (method.Name != ControllerMethodName) return null;

        var containing = method.ContainingType;
        if (containing == null || containing.Name != ControllerTypeName) return null;
        if (containing.ContainingNamespace?.ToDisplayString() != ControllerNamespace) return null;

        INamedTypeSymbol? controller = null;
        if (method.IsGenericMethod && method.TypeArguments.Length > 0)
            controller = method.TypeArguments[0] as INamedTypeSymbol;
        else if (inv.ArgumentList.Arguments.Count > 0 && inv.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax typeOf)
            controller = model.GetTypeInfo(typeOf.Type, token).Type as INamedTypeSymbol;

        return controller == null ? null : BuildFragment(controller, model.Compilation);
    }
    #endregion

    #region 构建
    // 为控制器类型生成完整片段：类型全名 + 成员代码（方法调用委托与分发表字段）
    private static String? BuildFragment(INamedTypeSymbol controller, Compilation compilation)
    {
        if (controller.TypeKind != TypeKind.Class || controller.IsStatic || controller.IsAbstract) return null;
        if (controller.IsUnboundGenericType || MemberLineBuilder.HasTypeParameters(controller)) return null;
        if (!compilation.IsSymbolAccessibleWithin(controller, compilation.Assembly)) return null;

        var typeName = controller.ToDisplayString(MemberLineBuilder.Format);
        var prefix = "Invoke_" + Sanitize(typeName) + "_";

        // 收集候选方法：本类型及基类的公开普通方法（排除 Object）；同名多候选（重载/大小写冲突）整体跳过，避免与原反射路径的"首个匹配"产生歧义
        var byName = new Dictionary<String, IMethodSymbol>(StringComparer.OrdinalIgnoreCase);
        var duplicates = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        for (var current = controller; current != null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is not IMethodSymbol m) continue;
                if (m.MethodKind != MethodKind.Ordinary || m.IsImplicitlyDeclared) continue;
                if (m.DeclaredAccessibility != Accessibility.Public) continue;
                if (m.IsGenericMethod) continue;

                if (byName.TryGetValue(m.Name, out var exist))
                {
                    if (!SymbolEqualityComparer.Default.Equals(exist, m)) duplicates.Add(m.Name);
                }
                else
                {
                    byName[m.Name] = m;
                }
            }
        }

        var builds = new List<MethodBuild>();
        foreach (var name in byName.Keys.OrderBy(e => e, StringComparer.Ordinal))
        {
            if (duplicates.Contains(name)) continue;

            var built = BuildMethod(byName[name], typeName, prefix, compilation);
            if (built != null) builds.Add(built);
        }
        if (builds.Count == 0) return null;

        var field = "Dispatchers_" + Sanitize(typeName);
        var sb = new StringBuilder();
        foreach (var item in builds)
        {
            sb.AppendLine();
            sb.Append(item.Code);
        }

        sb.AppendLine();
        sb.Append("        internal static readonly global::System.Collections.Generic.Dictionary<global::System.String, global::NewLife.Http.HttpDispatchTable.ControllerInvokeDelegate> ").Append(field).AppendLine(" = new global::System.Collections.Generic.Dictionary<global::System.String, global::NewLife.Http.HttpDispatchTable.ControllerInvokeDelegate>(global::System.StringComparer.OrdinalIgnoreCase)");
        sb.AppendLine("        {");
        foreach (var item in builds)
        {
            sb.Append("            [\"").Append(item.Name).Append("\"] = ").Append(item.Identifier).AppendLine(",");
        }
        sb.AppendLine("        };");

        return typeName + TypeSeparator + sb.ToString();
    }

    // 构建单个方法的调用委托代码；不可静态化的形态返回 null
    private static MethodBuild? BuildMethod(IMethodSymbol method, String controllerTypeName, String prefix, Compilation compilation)
    {
        var parameters = new List<String>();
        var bindings = new StringBuilder();
        String? singleTypeName = null;
        var singleNullable = false;

        for (var i = 0; i < method.Parameters.Length; i++)
        {
            var p = method.Parameters[i];
            if (p.RefKind != RefKind.None || p.IsParams) return null;
            if (String.IsNullOrEmpty(p.Name)) return null;
            if (!IsGeneratable(p.Type, compilation)) return null;

            var typeName = p.Type.ToDisplayString(MemberLineBuilder.Format);
            var fallback = BuildFallback(p);
            if (fallback == null) return null;

            bindings.Append("            ").Append(typeName).Append(" p").Append(i).AppendLine(";");
            bindings.Append("            if (context.Parameters.TryGetValue(\"").Append(p.Name).Append("\", out var raw").Append(i).AppendLine("))");
            bindings.Append("                p").Append(i).Append(" = global::NewLife.Reflection.Reflect.ChangeType<").Append(typeName).Append(">(raw").Append(i).AppendLine(")!;");
            bindings.AppendLine("            else");
            if (fallback.StartsWith("throw ", StringComparison.Ordinal))
                bindings.Append("                ").Append(fallback).AppendLine(";");
            else if (fallback == "null")
                bindings.Append("                p").Append(i).AppendLine(" = default!;");
            else
                bindings.Append("                p").Append(i).Append(" = ").Append(fallback).AppendLine(";");

            parameters.Add("p" + i);
            if (method.Parameters.Length == 1)
            {
                singleTypeName = typeName;
                singleNullable = p.Type.IsReferenceType || IsNullable(p.Type);
            }
        }

        // 镜像 ParameterBinder：单参数且取值为空时按整份参数字典反序列化（值类型缺失时上面已抛必填异常，无需回退）
        if (method.Parameters.Length == 1 && singleNullable && singleTypeName != null)
        {
            bindings.Append("            if (p0 is null) p0 = (").Append(singleTypeName).Append(")global::NewLife.Serialization.JsonHelper.Default.Convert(context.Parameters, typeof(").Append(singleTypeName).AppendLine("));");
        }

        var form = AnalyzeReturn(method.ReturnType, compilation);
        if (form == null) return null;

        var call = method.IsStatic
            ? controllerTypeName + "." + MemberLineBuilder.Escape(method.Name) + "(" + String.Join(", ", parameters) + ")"
            : "instance." + MemberLineBuilder.Escape(method.Name) + "(" + String.Join(", ", parameters) + ")";

        var identifier = prefix + Sanitize(method.Name);
        var sb = new StringBuilder();
        sb.Append("        private static ").Append(form.Async ? "async " : "").Append("global::System.Threading.Tasks.Task ").Append(identifier).AppendLine("(global::System.Object controller, global::NewLife.Http.IHttpContext context)");
        sb.AppendLine("        {");
        if (!method.IsStatic)
            sb.Append("            var instance = (").Append(controllerTypeName).AppendLine(")controller;");
        sb.Append(bindings);

        switch (form.Kind)
        {
            case "void":
                sb.Append("            ").Append(call).AppendLine(";");
                sb.AppendLine("            return global::System.Threading.Tasks.Task.CompletedTask;");
                break;
            case "task":
                sb.Append("            await ").Append(call).AppendLine(".ConfigureAwait(false);");
                break;
            case "taskof":
                sb.Append("            var value = await ").Append(call).AppendLine(".ConfigureAwait(false);");
                EmitResult(sb, form);
                break;
            default:
                sb.Append("            var value = ").Append(call).AppendLine(";");
                EmitResult(sb, form);
                sb.AppendLine("            return global::System.Threading.Tasks.Task.CompletedTask;");
                break;
        }

        sb.AppendLine("        }");

        return new MethodBuild(method.Name, identifier, sb.ToString());
    }

    // 结果写回：引用类型/可空类型判空后写入（镜像原路径的 result != null 判断），非空值类型直接写入（装箱后必然非空）
    private static void EmitResult(StringBuilder sb, ReturnForm form)
    {
        if (form.NullCheck)
            sb.AppendLine("            if (value != null) context.Response.SetResult(value!);");
        else
            sb.AppendLine("            context.Response.SetResult(value);");
    }

    // 参数缺失时的回退表达式：显式默认值 → 上下文注入 → 值类型抛必填异常 → 引用类型留空；无法表达时返回 null（整方法跳过生成）
    private static String? BuildFallback(IParameterSymbol p)
    {
        if (p.HasExplicitDefaultValue) return BuildDefaultLiteral(p);

        var type = p.Type;
        if (type.Name == "IHttpContext" && type.ContainingNamespace?.ToDisplayString() == ControllerNamespace) return "context";
        if (type.Name == "IServiceProvider" && type.ContainingNamespace?.ToDisplayString() == "System") return "context.ServiceProvider";

        if (type.IsValueType && !IsNullable(type))
            return "throw new global::System.ArgumentException(global::NewLife.Localization.Translate(\"Missing required parameter [{0}].\", \"" + p.Name + "\"), \"" + p.Name + "\")";

        return "null";
    }

    // 显式默认值字面量；无法表达时返回 null
    private static String? BuildDefaultLiteral(IParameterSymbol p)
    {
        var typeName = p.Type.ToDisplayString(MemberLineBuilder.Format);
        var value = p.ExplicitDefaultValue;
        if (value == null) return "default(" + typeName + ")";

        switch (value)
        {
            case String s:
                return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            case Boolean b:
                return b ? "true" : "false";
            case Char c:
                return "'" + c.ToString().Replace("\\", "\\\\").Replace("'", "\\'") + "'";
            case Single f:
                if (Single.IsNaN(f)) return "global::System.Single.NaN";
                if (Single.IsPositiveInfinity(f)) return "global::System.Single.PositiveInfinity";
                if (Single.IsNegativeInfinity(f)) return "global::System.Single.NegativeInfinity";
                return f.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "f";
            case Double d:
                if (Double.IsNaN(d)) return "global::System.Double.NaN";
                if (Double.IsPositiveInfinity(d)) return "global::System.Double.PositiveInfinity";
                if (Double.IsNegativeInfinity(d)) return "global::System.Double.NegativeInfinity";
                return d.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            case Decimal m:
                return m.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m";
            case Int64 l:
                return l.ToString(System.Globalization.CultureInfo.InvariantCulture) + "L";
            case UInt64 ul:
                return ul.ToString(System.Globalization.CultureInfo.InvariantCulture) + "UL";
            default:
                if (p.Type.TypeKind == TypeKind.Enum) return "(" + typeName + ")" + Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
                return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    // 返回值形态：void / Task / Task<T> / 普通值；ValueTask 等可疑形态跳过生成
    private static ReturnForm? AnalyzeReturn(ITypeSymbol type, Compilation compilation)
    {
        if (type.SpecialType == SpecialType.System_Void) return new ReturnForm("void", false, false);

        if (type is INamedTypeSymbol named && named.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks")
        {
            if (named.Name == "Task")
            {
                if (!named.IsGenericType) return new ReturnForm("task", true, false);

                var arg = named.TypeArguments[0];
                if (!IsGeneratable(arg, compilation)) return null;

                return new ReturnForm("taskof", true, arg.IsReferenceType || IsNullable(arg));
            }
            if (named.Name == "ValueTask") return null;
        }

        if (!IsGeneratable(type, compilation)) return null;

        return new ReturnForm("value", false, type.IsReferenceType || IsNullable(type));
    }

    // 可生成类型：复用成员生成器的判定（非类型参数/元组/指针/ref struct，且当前程序集可访问）
    private static Boolean IsGeneratable(ITypeSymbol type, Compilation compilation) => MemberLineBuilder.IsGeneratable(type, compilation);

    // 可空值类型（Nullable<T>）
    private static Boolean IsNullable(ITypeSymbol type) => type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

    // 生成标识符片段：去掉全限定前缀并把非标识符字符折叠为下划线
    private static String Sanitize(String name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (Char.IsLetterOrDigit(c) || c == '_')
                sb.Append(c);
            else if (sb.Length > 0 && sb[sb.Length - 1] != '_')
                sb.Append('_');
        }
        return sb.ToString().TrimEnd('_');
    }
    #endregion

    #region 产出
    // 输出生成源码。仅 .NET 5+（模块初始化器）时生成
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

        // 字段名去重：不同控制器类型的净化名可能折叠为同一标识符，按排序顺序追加序号保证确定性
        var fields = new Dictionary<String, String>(StringComparer.Ordinal);
        var used = new HashSet<String>(StringComparer.Ordinal);
        var index = 0;
        foreach (var item in set)
        {
            var typeName = Split(item).TypeName;
            var baseName = "Dispatchers_" + Sanitize(typeName);
            var field = baseName;
            while (!used.Add(field))
            {
                index++;
                field = baseName + "_" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            fields[typeName] = field;
        }

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated/>");
        if ((Int32)options.LanguageVersion >= (Int32)LanguageVersion.CSharp8) builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("namespace NewLife.SourceGenerator.Generated");
        builder.AppendLine("{");
        builder.AppendLine("    /// <summary>源生成的控制器静态分发注册。NewLife.Core 源生成器自动生成，请勿修改</summary>");
        builder.AppendLine("    internal static class ControllerDispatchRegistrar");
        builder.AppendLine("    {");

        foreach (var item in set)
        {
            builder.AppendLine();
            builder.Append(Split(item).Code);
        }

        builder.AppendLine();
        builder.AppendLine("        [global::System.Runtime.CompilerServices.ModuleInitializer]");
        builder.AppendLine("        internal static void Initialize()");
        builder.AppendLine("        {");
        foreach (var item in set)
        {
            var typeName = Split(item).TypeName;
            builder.Append("            global::NewLife.Http.HttpDispatchTable.RegisterController(typeof(")
                   .Append(typeName)
                   .Append("), ")
                   .Append(fields[typeName])
                   .AppendLine(");");
        }
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        context.AddSource("ControllerDispatchRegistrar.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    // 拆分"类型全名 + 成员代码"片段
    private static (String TypeName, String Code) Split(String item)
    {
        var i = item.IndexOf(TypeSeparator);
        return i < 0 ? (item, "") : (item.Substring(0, i), item.Substring(i + 1));
    }
    #endregion

    #region 模型
    // 返回值形态
    private sealed class ReturnForm
    {
        public String Kind { get; }

        public Boolean Async { get; }

        public Boolean NullCheck { get; }

        public ReturnForm(String kind, Boolean async, Boolean nullCheck)
        {
            Kind = kind;
            Async = async;
            NullCheck = nullCheck;
        }
    }

    // 单个方法的生成结果
    private sealed class MethodBuild
    {
        public String Name { get; }

        public String Identifier { get; }

        public String Code { get; }

        public MethodBuild(String name, String identifier, String code)
        {
            Name = name;
            Identifier = identifier;
            Code = code;
        }
    }
    #endregion
}
