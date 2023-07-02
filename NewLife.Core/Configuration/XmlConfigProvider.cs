﻿using System.Text;
using System.Xml;

namespace NewLife.Configuration;

/// <summary>Xml文件配置提供者</summary>
/// <remarks>
/// 支持从不同配置文件加载到不同配置模型
/// </remarks>
public class XmlConfigProvider : FileConfigProvider
{
    /// <summary>根元素名称</summary>
    public String RootName { get; set; } = "Root";

    /// <summary>初始化</summary>
    /// <param name="value"></param>
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
                var dic = new Dictionary<String, String>();
                reader.MoveToFirstAttribute();
                do
                {
                    //var cfg2 = cfg.AddChild(reader.Name);
                    //cfg2.Value = reader.Value;
                    dic[reader.Name] = reader.Value;
                } while (reader.MoveToNextAttribute());

                // 如果只有一个Value属性，可能是基元类型数组
                if (dic.Count == 1 && dic.TryGetValue("Value", out var val))
                    cfg.Value = val;
                else
                {
                    foreach (var item in dic)
                    {
                        var cfg2 = cfg.AddChild(item.Key);
                        cfg2.Value = item.Value;
                    }
                }
            }
            else
                reader.ReadStartElement();
            while (reader.NodeType == XmlNodeType.Whitespace) reader.Skip();

            // 遇到下一层节点
            if (reader.NodeType is XmlNodeType.Element or XmlNodeType.Comment)
                ReadNode(reader, cfg);
            else if (reader.NodeType == XmlNodeType.Text)
                cfg.Value = reader.ReadContentAsString();

            if (reader.NodeType == XmlNodeType.Attribute) reader.Read();
            if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();
            while (reader.NodeType == XmlNodeType.Whitespace) reader.Skip();
        }
    }

    /// <summary>获取字符串形式</summary>
    /// <param name="section">配置段</param>
    /// <returns></returns>
    public override String GetString(IConfigSection section = null)
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

    private void WriteNode(XmlWriter writer, String name, IConfigSection section)
    {
        writer.WriteStartElement(name);

        foreach (var item in section.Childs.ToArray())
        {
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
                        WriteAttributeNode(writer, elm.Key, elm);
                    }
                    writer.WriteEndElement();
                }
                else
                    WriteNode(writer, item.Key, item);
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

    private void WriteAttributeNode(XmlWriter writer, String name, IConfigSection section)
    {
        writer.WriteStartElement(name);
        //writer.WriteStartAttribute(name);

        if (section != null && section.Childs != null)
        {
            foreach (var item in section.Childs.ToArray())
            {
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
}