using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace NewLife.SourceGenerator;

/// <summary>把 Locales/*.yml 生成为静态语言表</summary>
/// <remarks>
/// 文件格式为严格 YAML 子集：每行一条 <c>"英文原文": "译文"</c>，仅支持双引号字符串与
/// <c>\\ \" \n \r \t \uXXXX</c> 转义；空行与 # 注释被忽略。
/// 构建期校验占位符集合、重复键与语法，运行期只做字典查找。
/// </remarks>
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
    /// <param name="context">增量生成上下文</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var files = context.AdditionalTextsProvider
            .Where(static f => f.Path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) && IsLocalePath(f.Path))
            .Select(static (f, ct) => new LocaleFile(Path.GetFileNameWithoutExtension(f.Path), f.Path, f.GetText(ct)?.ToString() ?? String.Empty));

        var input = files.Collect()
            .Combine(context.CompilationProvider.Select(static (c, _) => new ProjectInfo(
                c.AssemblyName,
                c.GetTypeByMetadataName("System.Runtime.CompilerServices.ModuleInitializerAttribute") != null,
                c.GetTypeByMetadataName("NewLife.L") != null)))
            .Combine(context.AnalyzerConfigOptionsProvider.Select(static (p, _) =>
                p.GlobalOptions.TryGetValue("build_property.NewLifeLocalizationGenerateL", out var value)
                && String.Equals(value, "false", StringComparison.OrdinalIgnoreCase)));

        context.RegisterSourceOutput(input, static (spc, source) => Emit(spc, source.Left.Left, source.Left.Right, source.Right));
    }

    private static Boolean IsLocalePath(String path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/Locales/", StringComparison.OrdinalIgnoreCase) || normalized.StartsWith("Locales/", StringComparison.OrdinalIgnoreCase);
    }

    private static void Emit(SourceProductionContext context, System.Collections.Immutable.ImmutableArray<LocaleFile> files, ProjectInfo project, Boolean skipL)
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

        var source = String.Equals(project.AssemblyName, "NewLife.Core", StringComparison.Ordinal)
            ? BuildCoreSource(tables)
            : BuildDownstreamSource(tables, project.HasModuleInitializer, project.HasL || skipL);
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
        // 键表全语言共享，译文按“长度数组 + 连续译文”Deflate 压缩存储；只有真正用到的语言才解压建表
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

    /// <summary>收集全部语言共享的键顺序</summary>
    /// <param name="tables">语言表集合</param>
    /// <returns>按首次出现顺序排列的键表</returns>
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

    /// <summary>语言表字段名，语言键中的连字符替换为下划线</summary>
    /// <param name="culture">语言键</param>
    /// <returns>合法的 C# 标识符</returns>
    private static String GetTableField(String culture) => "_t_" + culture.Replace('-', '_');

    /// <summary>把语言表序列化为“长度数组 + 连续译文”，再 Deflate 压缩</summary>
    /// <param name="table">语言表</param>
    /// <param name="keys">全语言共享的键顺序</param>
    /// <returns>压缩后的译文数据</returns>
    /// <remarks>键表全语言共享，因此只压缩译文；缺失键的长度记为 -1，运行时按英文原文回退。</remarks>
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

    /// <summary>输出全语言共享的键表</summary>
    /// <param name="sb">输出缓冲区</param>
    /// <param name="keys">键表</param>
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

    /// <summary>输出压缩后的字节数组</summary>
    /// <param name="sb">输出缓冲区</param>
    /// <param name="name">字段名</param>
    /// <param name="data">压缩数据</param>
    /// <remarks>逐元素初始化的 Byte[] 由编译器落成 RVA 数据段，体积约等于原始字节数，不会按 IL 指令膨胀。</remarks>
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

    private static String BuildDownstreamSource(IDictionary<String, Dictionary<String, String>> tables, Boolean hasModuleInitializer, Boolean hasL)
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
        sb.AppendLine("    /// <summary>当前程序集的多语言表，由源生成器自动注册</summary>");
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
        sb.AppendLine("                try { Localization.Register(new CultureInfo(item.Key), item.Value); }");
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
            sb.AppendLine("        /// <param name=\"template\">英文原文模板</param>");
            sb.AppendLine("        /// <returns>本地化文本；缺少翻译时返回原文</returns>");
            sb.AppendLine("        public static String T(String template) => Localization.Translate(template);");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>翻译并格式化指定文本</summary>");
            sb.AppendLine("        /// <param name=\"template\">英文原文模板</param>");
            sb.AppendLine("        /// <param name=\"args\">格式化参数</param>");
            sb.AppendLine("        /// <returns>本地化文本；缺少翻译或格式化失败时回退英文</returns>");
            sb.AppendLine("        public static String T(String template, params Object?[]? args) => Localization.Translate(template, args);");
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
            if (j > i + 1 && j < text.Length && (text[j] == '}' || text[j] == ':'))
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

        public ProjectInfo(String? assemblyName, Boolean hasModuleInitializer, Boolean hasL)
        {
            AssemblyName = assemblyName;
            HasModuleInitializer = hasModuleInitializer;
            HasL = hasL;
        }
    }
}

