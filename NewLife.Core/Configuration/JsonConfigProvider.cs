using NewLife.Serialization;
using NewLife.Serialization.Json;

namespace NewLife.Configuration;

/// <summary>Json文件配置提供者</summary>
/// <remarks>
/// 支持从不同配置文件加载到不同配置模型
/// </remarks>
public class JsonConfigProvider : FileConfigProvider
{
    #region 静态
    /// <summary>加载本地配置文件得到配置提供者</summary>
    /// <param name="fileName">配置文件名，默认appsettings.json</param>
    /// <returns></returns>
    public static JsonConfigProvider LoadAppSettings(String fileName = null)
    {
        if (fileName.IsNullOrEmpty()) fileName = "appsettings.json";

        // 读取本地配置
        var jsonConfig = new JsonConfigProvider { FileName = fileName };

        return jsonConfig;
    }
    #endregion

    /// <summary>初始化</summary>
    /// <param name="value"></param>
    public override void Init(String value)
    {
        // 加上默认后缀
        if (!value.IsNullOrEmpty() && Path.GetExtension(value).IsNullOrEmpty()) value += ".json";

        base.Init(value);
    }

    /// <summary>读取配置文件</summary>
    /// <param name="fileName">文件名</param>
    /// <param name="section">配置段</param>
    protected override void OnRead(String fileName, IConfigSection section)
    {
        var txt = File.ReadAllText(fileName);

        // 预处理注释
        txt = TrimComment(txt);

        var src = txt.DecodeJson();

        Map(src, section);
    }

    /// <summary>获取字符串形式</summary>
    /// <param name="section">配置段</param>
    /// <returns></returns>
    public override String GetString(IConfigSection section = null)
    {
        section ??= Root;

        var rs = new Dictionary<String, Object>();
        Map(section, rs);

        var jw = new JsonWriter
        {
            IgnoreNullValues = false,
            IgnoreComment = false,
            Indented = true,
            //SmartIndented = true,
        };

        jw.Write(rs);

        return jw.GetString();

        //var js = new Json();
        //js.Write(rs);

        //return js.GetBytes().ToStr();
    }

    #region 辅助
    /// <summary>字典映射到配置树</summary>
    /// <param name="src"></param>
    /// <param name="section"></param>
    protected virtual void Map(IDictionary<String, Object> src, IConfigSection section)
    {
        foreach (var item in src)
        {
            var name = item.Key;
            if (name[0] == '#') continue;

            var cfg = section.GetOrAddChild(name);
            var cname = "#" + name;
            if (src.TryGetValue(cname, out var comment) && comment != null) cfg.Comment = comment + "";

            // 支持字典
            if (item.Value is IDictionary<String, Object> dic)
                Map(dic, cfg);
            else if (item.Value is IList<Object> list)
            {
                cfg.Childs = new List<IConfigSection>();
                foreach (var elm in list)
                {
                    // 复杂对象
                    if (elm is IDictionary<String, Object> dic2)
                    {
                        var cfg2 = new ConfigSection();
                        Map(dic2, cfg2);
                        cfg.Childs.Add(cfg2);
                    }
                    // 简单基元类型
                    else
                    {
                        var cfg2 = new ConfigSection
                        {
                            Key = elm?.GetType()?.Name,
                            Value = elm + "",
                        };
                        cfg.Childs.Add(cfg2);
                    }
                }
            }
            else
                cfg.SetValue(item.Value);
        }
    }

    /// <summary>配置树映射到字典</summary>
    /// <param name="section"></param>
    /// <param name="dst"></param>
    protected virtual void Map(IConfigSection section, IDictionary<String, Object> dst)
    {
        foreach (var item in section.Childs)
        {
            //// 注释
            //if (!item.Comment.IsNullOrEmpty()) dst["#" + item.Key] = item.Comment;

            var cs = item.Childs;
            if (cs != null)
            {
                // 数组
                if (cs.Count == 0 || cs.Count > 0 && cs[0].Key == null || cs.Count >= 2 && cs[0].Key == cs[1].Key)
                {
                    // 普通基元类型数组
                    if (cs.Count > 0 && (cs[0].Childs == null || cs[0].Childs.Count == 0))
                    {
                        dst[item.Key] = cs.Select(e => e.Value).ToArray();
                    }
                    else
                    {
                        var list = new List<Object>();
                        foreach (var elm in cs)
                        {
                            var rs = new Dictionary<String, Object>();
                            Map(elm, rs);
                            list.Add(rs);
                        }
                        dst[item.Key] = list;
                    }
                }
                else
                {
                    var rs = new Dictionary<String, Object>();
                    Map(item, rs);

                    dst[item.Key] = rs;
                }
            }
            else
            {
                dst[item.Key] = item.Value;
            }
        }
    }

    /// <summary>
    /// 清理json字符串中的注释，避免json解析错误
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static String TrimComment(String text)
    {
        while (true)
        {
            // 以下处理多行注释 “/**/” 放在一行的情况
            var p = text.IndexOf("/*");
            if (p < 0) break;

            var p2 = text.IndexOf("*/", p + 2);
            if (p2 < 0) break;

            text = text[..p] + text[(p2 + 2)..];
        }

        // 增加 \r以及\n的处理， 处理类似如下json转换时的错误：==>{"key":"http://*:5000" \n /*注释*/}<==
        var lines = text.Split("\r\n", "\n", "\r");
        text = lines
            .Where(e => !e.IsNullOrEmpty() && !e.TrimStart().StartsWith("//"))
            // 没考虑到链接中带双斜杠的，以下导致链接的内容被干掉
            //.Select(e =>
            //{
            //    // 单行注释 “//” 放在最后的情况
            //    var p0 = e.IndexOf("//");
            //    if (p0 > 0) return e.Substring(0, p0);

            //    return e;
            //})
            .Join(Environment.NewLine);

        return text;
    }
    #endregion
}