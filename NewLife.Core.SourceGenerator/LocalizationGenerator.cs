using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace NewLife.SourceGenerator;

/// <summary>把 Locales/*.yml 生成为语言表</summary>
[Generator(LanguageNames.CSharp)]
public sealed class LocalizationGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor SyntaxError = new(
        "LOC001", "Invalid localization entry", "本地化文件格式错误：{0}（{1}:{2}）", "Localization",
        DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor DuplicateKey = new(
        "LOC002", "Duplicate localization key", "本地化键重复：{0}（{1}:{2}）", "Localization",
        DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor PlaceholderMismatch = new(
        "LOC003", "Localization placeholder mismatch", "占位符与英文原文不一致：{0}（{1}:{2}）", "Localization",
        DiagnosticSeverity.Error, true);

    private static readonly DiagnosticDescriptor PlaceholderSpecMismatch = new(
        "LOC004", "Localization format specifier mismatch", "格式说明符与英文原文不一致：{0}（{1}:{2}）", "Localization",
        DiagnosticSeverity.Warning, true);

    /// <summary>初始化生成器</summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var files = context.AdditionalTextsProvider
            .Where(static f => f.Path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) && IsLocalePath(f.Path))
            .Select(static (f, ct) => new LocaleFile(Path.GetFileNameWithoutExtension(f.Path), f.Path, f.GetText(ct)?.ToString() ?? String.Empty));

        var input = files.Collect()
            .Combine(context.CompilationProvider.Select(static (c, _) =>
            {
                // 只跳过“当前编译可见”的 NewLife.L：源码自带、引用程序集里 public、或 internal + IVT 可见的 L。
                // Core 程序集里的 internal L 对下游不可见，不能被误判为“下游已自带 L”
                var ownL = c.GetTypeByMetadataName("NewLife.L");
                var hasL = ownL != null && (
                    SymbolEqualityComparer.Default.Equals(ownL.ContainingAssembly, c.Assembly)
                    || ownL.DeclaredAccessibility == Accessibility.Public
                    || (ownL.DeclaredAccessibility == Accessibility.Internal && ownL.ContainingAssembly.GivesAccessTo(c.Assembly)));

                // 只把“当前编译可见”的 ModuleInitializerAttribute 视为可用。
                // 依赖包里 internal 的 polyfill 对下游不可见，若误判为可用会生成 [ModuleInitializer] 并触发 CS0122。
                var ownModuleInitializer = c.GetTypeByMetadataName("System.Runtime.CompilerServices.ModuleInitializerAttribute");
                var hasModuleInitializer = ownModuleInitializer != null && (
                    SymbolEqualityComparer.Default.Equals(ownModuleInitializer.ContainingAssembly, c.Assembly)
                    || ownModuleInitializer.DeclaredAccessibility == Accessibility.Public
                    || (ownModuleInitializer.DeclaredAccessibility == Accessibility.Internal && ownModuleInitializer.ContainingAssembly.GivesAccessTo(c.Assembly)));

                return new ProjectInfo(
                    c.AssemblyName,
                    hasModuleInitializer,
                    hasL,
                    c.Options.OutputKind is OutputKind.ConsoleApplication or OutputKind.WindowsApplication);
            }))
            .Combine(context.AnalyzerConfigOptionsProvider.Select(static (p, _) => new BuildOptions(
                p.GlobalOptions.TryGetValue("build_property.NewLifeLocalizationGenerateL", out var value)
                && String.Equals(value, "false", StringComparison.OrdinalIgnoreCase),
                GetOverwrite(p.GlobalOptions))));

        context.RegisterSourceOutput(input, static (spc, source) => Emit(spc, source.Left.Left, source.Left.Right, source.Right));
    }

    private static Boolean? GetOverwrite(AnalyzerConfigOptions options)
    {
        if (!options.TryGetValue("build_property.NewLifeLocalizationOverwrite", out var value) || String.IsNullOrEmpty(value)) return null;

        return String.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static Boolean IsLocalePath(String path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/Locales/", StringComparison.OrdinalIgnoreCase) || normalized.StartsWith("Locales/", StringComparison.OrdinalIgnoreCase);
    }

    private static void Emit(SourceProductionContext context, System.Collections.Immutable.ImmutableArray<LocaleFile> files, ProjectInfo project, BuildOptions options)
    {
        if (files.IsEmpty) return;
        var tables = new SortedDictionary<String, Dictionary<String, String>>(StringComparer.Ordinal);

        foreach (var file in files)
        {
            if (String.IsNullOrEmpty(file.Culture)) continue;

            var lines = file.Text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var table = new Dictionary<String, String>(StringComparer.Ordinal);

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                if (!TryParseEntry(line, out var key, out var value, out var error))
                {
                    context.ReportDiagnostic(Diagnostic.Create(SyntaxError, CreateLocation(file.Path, i), error, file.Culture, i + 1));
                    continue;
                }

                if (table.ContainsKey(key))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DuplicateKey, CreateLocation(file.Path, i), key, file.Culture, i + 1));
                    continue;
                }

                if (!GetPlaceholders(key).SetEquals(GetPlaceholders(value)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(PlaceholderMismatch, CreateLocation(file.Path, i), key, file.Culture, i + 1));
                    continue;
                }

                var enSpecs = GetPlaceholderSpecs(key);
                var trSpecs = GetPlaceholderSpecs(value);
                foreach (var spec in enSpecs)
                {
                    if (trSpecs.TryGetValue(spec.Key, out var text) && text != spec.Value)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(PlaceholderSpecMismatch, CreateLocation(file.Path, i), key, file.Culture, i + 1));
                        break;
                    }
                }

                table[key] = value;
            }

            tables[file.Culture] = table;
        }

        // 应用默认覆盖，类库只补充新 key
        var overwrite = options.Overwrite ?? project.IsExecutable;

        var source = String.Equals(project.AssemblyName, "NewLife.Core", StringComparison.Ordinal)
            ? BuildCoreSource(tables)
            : BuildDownstreamSource(tables, project.HasModuleInitializer, project.HasL || options.SkipL, overwrite);
        context.AddSource("Localization.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private static String BuildCoreSource(IDictionary<String, Dictionary<String, String>> tables)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Globalization;");
        sb.AppendLine();
        sb.AppendLine("namespace NewLife;");
        sb.AppendLine();
        sb.AppendLine("public static partial class Localization");
        sb.AppendLine("{");
        var keys = GetKeyOrder(tables);
        AppendKeyArray(sb, keys);
        foreach (var item in tables)
        {
            AppendByteArray(sb, GetTableField(item.Key), CompressTable(item.Value, keys));
        }
        sb.AppendLine("    private static Dictionary<String, String>? CreateTable(String key)");
        sb.AppendLine("    {");
        sb.AppendLine("        switch (key)");
        sb.AppendLine("        {");
        foreach (var item in tables)
        {
            sb.Append("            case \"").Append(Escape(item.Key)).Append("\": return DecodeTable(_keys, ").Append(GetTableField(item.Key)).AppendLine(");");
        }
        sb.AppendLine("            default: return null;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private static String? GetCultureKey(CultureInfo culture)");
        sb.AppendLine("    {");
        sb.AppendLine("        var name = culture.Name;");
        sb.AppendLine("        if (String.IsNullOrEmpty(name)) return null;");
        sb.AppendLine();
        sb.AppendLine("        if (name.StartsWith(\"zh\", StringComparison.OrdinalIgnoreCase))");
        sb.AppendLine("        {");
        sb.AppendLine("            var isHant = name.IndexOf(\"Hant\", StringComparison.OrdinalIgnoreCase) >= 0");
        sb.AppendLine("                || name.EndsWith(\"-TW\", StringComparison.OrdinalIgnoreCase)");
        sb.AppendLine("                || name.EndsWith(\"-HK\", StringComparison.OrdinalIgnoreCase)");
        sb.AppendLine("                || name.EndsWith(\"-MO\", StringComparison.OrdinalIgnoreCase);");
        sb.AppendLine("            return isHant ? \"zh-Hant\" : \"zh-Hans\";");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        var lang = name;");
        sb.AppendLine("        var index = lang.IndexOf('-');");
        sb.AppendLine("        if (index > 0) lang = lang.Substring(0, index);");
        sb.AppendLine("        return lang.ToLowerInvariant();");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static List<String> GetKeyOrder(IDictionary<String, Dictionary<String, String>> tables)
    {
        var keys = new List<String>();
        var seen = new HashSet<String>(StringComparer.Ordinal);
        foreach (var table in tables.Values)
        {
            foreach (var key in table.Keys)
            {
                if (seen.Add(key)) keys.Add(key);
            }
        }
        return keys;
    }

    private static String GetTableField(String culture) => "_t_" + culture.Replace('-', '_');

    private static Byte[] CompressTable(IDictionary<String, String> table, IList<String> keys)
    {
        var values = new List<Byte[]>(keys.Count);
        var lengths = new Byte[keys.Count * 4];
        var size = 0;
        for (var i = 0; i < keys.Count; i++)
        {
            var length = -1;
            if (table.TryGetValue(keys[i], out var value))
            {
                var data = Encoding.UTF8.GetBytes(value ?? String.Empty);
                values.Add(data);
                length = data.Length;
                size += data.Length;
            }

            lengths[i * 4] = (Byte)length;
            lengths[i * 4 + 1] = (Byte)(length >> 8);
            lengths[i * 4 + 2] = (Byte)(length >> 16);
            lengths[i * 4 + 3] = (Byte)(length >> 24);
        }

        using var ms = new MemoryStream(lengths.Length + size);
        using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, true))
        {
            deflate.Write(lengths, 0, lengths.Length);
            foreach (var item in values) deflate.Write(item, 0, item.Length);
        }
        return ms.ToArray();
    }

    private static void AppendKeyArray(StringBuilder sb, IList<String> keys)
    {
        sb.AppendLine("    private static readonly String[] _keys =");
        sb.AppendLine("    {");
        foreach (var key in keys)
        {
            sb.Append("        \"").Append(Escape(key)).AppendLine("\",");
        }
        sb.AppendLine("    };");
        sb.AppendLine();
    }

    private static void AppendByteArray(StringBuilder sb, String name, Byte[] data)
    {
        sb.Append("    private static readonly Byte[] ").Append(name).AppendLine(" = new Byte[]");
        sb.AppendLine("    {");
        for (var i = 0; i < data.Length; i++)
        {
            if (i > 0) sb.Append(',');
            if (i % 32 == 0) sb.AppendLine().Append("        ");
            sb.Append(data[i]);
        }
        sb.AppendLine();
        sb.AppendLine("    };");
        sb.AppendLine();
    }

    private static void AppendTables(StringBuilder sb, IDictionary<String, Dictionary<String, String>> tables, String indent)
    {
        var valueIndent = indent + "    ";
        foreach (var item in tables)
        {
            sb.Append(indent).Append("[\"").Append(Escape(item.Key)).AppendLine("\"] = new(StringComparer.Ordinal)");
            sb.Append(indent).AppendLine("{");
            foreach (var pair in item.Value)
            {
                sb.Append(valueIndent).Append("[\"").Append(Escape(pair.Key)).Append("\"] = \"").Append(Escape(pair.Value)).AppendLine("\",");
            }
            sb.Append(indent).AppendLine("},");
        }
    }

    private static String BuildDownstreamSource(IDictionary<String, Dictionary<String, String>> tables, Boolean hasModuleInitializer, Boolean hasL, Boolean overwrite)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Globalization;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine();

        if (!hasModuleInitializer)
        {
            sb.AppendLine("namespace System.Runtime.CompilerServices");
            sb.AppendLine("{");
            sb.AppendLine("    [AttributeUsage(AttributeTargets.Method, Inherited = false)]");
            sb.AppendLine("    internal sealed class ModuleInitializerAttribute : Attribute { }");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        sb.AppendLine("namespace NewLife");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>当前程序集的多语言表</summary>");
        sb.AppendLine("    internal static class LocalizationTable");
        sb.AppendLine("    {");
        sb.AppendLine("        private static readonly Dictionary<String, Dictionary<String, String>> _tables = new(StringComparer.Ordinal)");
        sb.AppendLine("        {");
        AppendTables(sb, tables, "            ");
        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        [ModuleInitializer]");
        sb.AppendLine("        internal static void RegisterTables()");
        sb.AppendLine("        {");
        sb.AppendLine("            foreach (var item in _tables)");
        sb.AppendLine("            {");
        sb.AppendLine("                // 不变全球化（InvariantGlobalization=true）下具名区域性不可用，跳过注册即可，运行时按英文回退");
        sb.AppendLine("                // 应用默认覆盖 Core 同名译文；类库默认不覆盖，可用 MSBuild 属性 NewLifeLocalizationOverwrite 开启");
        sb.AppendLine($"                try {{ Localization.Register(new CultureInfo(item.Key), item.Value, overwrite: {(overwrite ? "true" : "false")}); }}");
        sb.AppendLine("                catch (Exception ex) when (ex is CultureNotFoundException || ex is ArgumentException) { }");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");

        // 组件自带 NewLife.L 时不重复生成，避免类型冲突
        if (!hasL)
        {
            sb.AppendLine();
            sb.AppendLine("    /// <summary>本地化翻译快捷入口</summary>");
            sb.AppendLine("    internal static class L");
            sb.AppendLine("    {");
            sb.AppendLine("        /// <summary>翻译指定文本</summary>");
            sb.AppendLine("        public static String T(String template) => Localization.Translate(template);");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>翻译并格式化指定文本</summary>");
            sb.AppendLine("        public static String T(String template, params Object?[]? args) => Localization.Translate(template, args);");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>按日志语言翻译指定文本</summary>");
            sb.AppendLine("        public static String TLog(String template) => Localization.TranslateForLog(template);");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>按日志语言翻译并格式化指定文本</summary>");
            sb.AppendLine("        public static String TLog(String template, params Object?[]? args) => Localization.TranslateForLog(template, args);");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }
    private static Boolean TryParseEntry(String line, out String key, out String value, out String error)
    {
        key = String.Empty;
        value = String.Empty;
        error = String.Empty;

        var index = 0;
        if (!TryReadQuoted(line, ref index, out key, out error)) return false;

        SkipWhiteSpace(line, ref index);
        if (index >= line.Length || line[index] != ':')
        {
            error = "缺少冒号分隔符";
            return false;
        }
        index++;

        SkipWhiteSpace(line, ref index);
        if (!TryReadQuoted(line, ref index, out value, out error)) return false;

        SkipWhiteSpace(line, ref index);
        if (index < line.Length && line[index] != '#')
        {
            error = "值之后存在多余内容";
            return false;
        }

        return true;
    }

    private static Boolean TryReadQuoted(String line, ref Int32 index, out String value, out String error)
    {
        value = String.Empty;
        error = String.Empty;

        if (index >= line.Length || line[index] != '"')
        {
            error = "字符串必须以双引号开始";
            return false;
        }
        index++;

        var sb = new StringBuilder();
        while (index < line.Length)
        {
            var ch = line[index++];
            if (ch == '"')
            {
                value = sb.ToString();
                return true;
            }
            if (ch != '\\')
            {
                sb.Append(ch);
                continue;
            }
            if (index >= line.Length)
            {
                error = "转义字符不完整";
                return false;
            }

            var esc = line[index++];
            switch (esc)
            {
                case '\\': sb.Append('\\'); break;
                case '"': sb.Append('"'); break;
                case 'n': sb.Append('\n'); break;
                case 'r': sb.Append('\r'); break;
                case 't': sb.Append('\t'); break;
                case 'u':
                    if (index + 4 > line.Length || !Int32.TryParse(line.Substring(index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                    {
                        error = "\\u 转义格式错误";
                        return false;
                    }
                    sb.Append((Char)code);
                    index += 4;
                    break;
                default:
                    error = "不支持的转义字符：\\" + esc;
                    return false;
            }
        }

        error = "字符串缺少结束引号";
        return false;
    }

    private static void SkipWhiteSpace(String line, ref Int32 index)
    {
        while (index < line.Length && Char.IsWhiteSpace(line[index])) index++;
    }

    private static HashSet<Int32> GetPlaceholders(String text)
    {
        var set = new HashSet<Int32>();
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '{') continue;
            if (i + 1 < text.Length && text[i + 1] == '{')
            {
                i++;
                continue;
            }

            var j = i + 1;
            while (j < text.Length && Char.IsDigit(text[j])) j++;
            // 逗号后是对齐宽度（如 {0,7:n0}），也属于该占位符，不能当作普通文本跳过
            if (j > i + 1 && j < text.Length && (text[j] == '}' || text[j] == ':' || text[j] == ','))
            {
                if (Int32.TryParse(text.Substring(i + 1, j - i - 1), out var number)) set.Add(number);
            }
            i = j;
        }
        return set;
    }

    private static Dictionary<Int32, String> GetPlaceholderSpecs(String text)
    {
        var map = new Dictionary<Int32, String>();
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '{') continue;
            if (i + 1 < text.Length && text[i + 1] == '{')
            {
                i++;
                continue;
            }

            var j = i + 1;
            while (j < text.Length && Char.IsDigit(text[j])) j++;
            if (j > i + 1 && j < text.Length && (text[j] == '}' || text[j] == ':' || text[j] == ','))
            {
                var end = text.IndexOf('}', j);
                if (end >= j && Int32.TryParse(text.Substring(i + 1, j - i - 1), out var number))
                    map[number] = text.Substring(j, end - j);
                if (end >= 0)
                {
                    i = end;
                    continue;
                }
            }
            i = j;
        }
        return map;
    }

    private static Location CreateLocation(String path, Int32 line)
    {
        var position = new LinePosition(line, 0);
        return Location.Create(path, new TextSpan(0, 0), new LinePositionSpan(position, position));
    }

    private static String Escape(String value) => value
        .Replace("\\", "\\\\")
        .Replace("\"", "\\\"")
        .Replace("\r", "\\r")
        .Replace("\n", "\\n")
        .Replace("\t", "\\t");

    private sealed class LocaleFile
    {
        public String Culture { get; }
        public String Path { get; }
        public String Text { get; }

        public LocaleFile(String culture, String path, String text)
        {
            Culture = culture;
            Path = path;
            Text = text;
        }
    }

    private sealed class ProjectInfo
    {
        public String? AssemblyName { get; }
        public Boolean HasModuleInitializer { get; }
        public Boolean HasL { get; }
        public Boolean IsExecutable { get; }

        public ProjectInfo(String? assemblyName, Boolean hasModuleInitializer, Boolean hasL, Boolean isExecutable)
        {
            AssemblyName = assemblyName;
            HasModuleInitializer = hasModuleInitializer;
            HasL = hasL;
            IsExecutable = isExecutable;
        }
    }

    private sealed class BuildOptions
    {
        public Boolean SkipL { get; }
        public Boolean? Overwrite { get; }

        public BuildOptions(Boolean skipL, Boolean? overwrite)
        {
            SkipL = skipL;
            Overwrite = overwrite;
        }
    }
}

