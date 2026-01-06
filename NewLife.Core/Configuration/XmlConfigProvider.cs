using System.Text;
using System.Xml;

namespace NewLife.Configuration;

/// <summary>Xml文件配置提供者</summary>
/// <remarks>支持从不同配置文件加载到不同配置模型，支持注释和多级嵌套</remarks>
public class XmlConfigProvider : FileConfigProvider
{
    #region 属性
    /// <summary>根元素名称。默认 Root，初始化时会自动设置为配置文件名</summary>
    public String RootName { get; set; } = "Root";
    #endregion

    #region 方法
    /// <summary>初始化</summary>
    /// <param name="value">配置文件名</param>
    public override void Init(String value)
    {
        if ((RootName.IsNullOrEmpty() || RootName == "Root") && !value.IsNullOrEmpty()) RootName = Path.GetFileNameWithoutExtension(value);

        // 加上默认后缀
        if (!value.IsNullOrEmpty() && Path.GetExtension(value).IsNullOrEmpty()) value += ".config";

        base.Init(value);
    }

    /// <summary>读取配置文件</summary>
    /// <param name="fileName">文件名</param>
    /// <param name="section">配置段</param>
    protected override void OnRead(String fileName, IConfigSection section)
    {
        using var fs = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = XmlReader.Create(fs);

        // 移动到第一个元素
        while (reader.NodeType != XmlNodeType.Element) reader.Read();

        if (!reader.Name.IsNullOrEmpty()) RootName = reader.Name;

        reader.ReadStartElement();
        while (reader.NodeType == XmlNodeType.Whitespace) reader.Skip();

        ReadNode(reader, section);

        if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();
    }

    /// <summary>递归读取Xml节点到配置树</summary>
    /// <param name="reader">Xml读取器</param>
    /// <param name="section">目标配置节</param>
    private void ReadNode(XmlReader reader, IConfigSection section)
    {
        while (true)
        {
            var remark = "";
            if (reader.NodeType == XmlNodeType.Comment) remark = reader.Value;
            while (reader.NodeType is XmlNodeType.Comment or XmlNodeType.Whitespace) reader.Skip();
            if (reader.NodeType != XmlNodeType.Element) break;

            var name = reader.Name;
            var cfg = section.AddChild(name);
            // 前一行是注释
            if (!remark.IsNullOrEmpty()) cfg.Comment = remark;

            // 读取属性值
            if (reader.HasAttributes)
            {
                ReadAttributes(reader, cfg);
            }
            else
            {
                reader.ReadStartElement();
            }
            while (reader.NodeType == XmlNodeType.Whitespace) reader.Skip();

            // 遇到下一层节点
            if (reader.NodeType is XmlNodeType.Element or XmlNodeType.Comment)
            {
                ReadNode(reader, cfg);
            }
            else if (reader.NodeType == XmlNodeType.Text)
            {
                cfg.Value = reader.ReadContentAsString();
            }

            if (reader.NodeType == XmlNodeType.Attribute) reader.Read();
            if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();
            while (reader.NodeType == XmlNodeType.Whitespace) reader.Skip();
        }
    }

    /// <summary>读取元素属性到配置节</summary>
    /// <param name="reader">Xml读取器</param>
    /// <param name="cfg">目标配置节</param>
    private void ReadAttributes(XmlReader reader, IConfigSection cfg)
    {
        var dic = new Dictionary<String, String>();
        reader.MoveToFirstAttribute();
        do
        {
            dic[reader.Name] = reader.Value;
        } while (reader.MoveToNextAttribute());

        // 如果只有一个Value属性，可能是基元类型数组
        if (dic.Count == 1 && dic.TryGetValue("Value", out var val))
        {
            cfg.Value = val;
        }
        else
        {
            foreach (var item in dic)
            {
                var cfg2 = cfg.AddChild(item.Key);
                cfg2.Value = item.Value;
            }
        }
    }

    /// <summary>获取字符串形式</summary>
    /// <param name="section">配置段</param>
    /// <returns>Xml格式的配置字符串</returns>
    public override String GetString(IConfigSection? section = null)
    {
        section ??= Root;

        var set = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            CloseOutput = true,
        };

        using var ms = new MemoryStream();
        using var writer = XmlWriter.Create(ms, set);

        writer.WriteStartDocument();
        WriteNode(writer, RootName, section);
        writer.WriteEndDocument();

        writer.Flush();
        ms.Position = 0;

        return ms.ToStr();
    }

    /// <summary>递归写入配置树到Xml节点</summary>
    /// <param name="writer">Xml写入器</param>
    /// <param name="name">节点名称</param>
    /// <param name="section">源配置节</param>
    private void WriteNode(XmlWriter writer, String name, IConfigSection section)
    {
        if (section.Childs == null) return;

        writer.WriteStartElement(name);

        foreach (var item in section.Childs.ToArray())
        {
            if (item.Key.IsNullOrEmpty()) continue;

            // 写注释
            if (!item.Comment.IsNullOrEmpty()) writer.WriteComment(item.Comment);

            var cs = item.Childs;
            if (cs != null)
            {
                // 数组
                if (cs.Count >= 2 && cs[0].Key == cs[1].Key)
                {
                    writer.WriteStartElement(item.Key);
                    foreach (var elm in cs)
                    {
                        if (!elm.Key.IsNullOrEmpty()) WriteAttributeNode(writer, elm.Key, elm);
                    }
                    writer.WriteEndElement();
                }
                else
                {
                    WriteNode(writer, item.Key, item);
                }
            }
            else
            {
                // 避免写null时导致xml元素未闭合
                writer.WriteStartElement(item.Key);
                writer.WriteValue(item.Value + "");
                writer.WriteEndElement();
            }
        }

        writer.WriteEndElement();
    }

    /// <summary>写入属性节点</summary>
    /// <param name="writer">Xml写入器</param>
    /// <param name="name">节点名称</param>
    /// <param name="section">源配置节</param>
    private void WriteAttributeNode(XmlWriter writer, String name, IConfigSection section)
    {
        writer.WriteStartElement(name);
        //writer.WriteStartAttribute(name);

        if (section.Childs != null)
        {
            foreach (var item in section.Childs.ToArray())
            {
                if (item.Key.IsNullOrEmpty()) continue;

                writer.WriteAttributeString(item.Key, item.Value + "");
            }
        }
        else
        {
            writer.WriteAttributeString("Value", section.Value + "");
        }

        if (writer.WriteState == WriteState.Attribute)
            writer.WriteEndAttribute();
        else
            writer.WriteEndElement();
    }
    #endregion
}