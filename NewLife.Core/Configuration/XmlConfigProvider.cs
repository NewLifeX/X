using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace NewLife.Configuration
{
    /// <summary>Xml文件配置提供者</summary>
    /// <remarks>
    /// 支持从不同配置文件加载到不同配置模型
    /// </remarks>
    public class XmlConfigProvider : FileConfigProvider
    {
        /// <summary>读取配置文件，得到字典</summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected override IDictionary<String, String> OnRead(String fileName)
        {
            using var fs = File.OpenRead(fileName);
            using var reader = XmlReader.Create(fs);

            var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);

            // 移动到第一个元素
            while (reader.NodeType != XmlNodeType.Element) reader.Read();

            reader.ReadStartElement();
            while (true)
            {
                while (reader.NodeType == XmlNodeType.Comment || reader.NodeType == XmlNodeType.Whitespace) reader.Skip();
                if (reader.NodeType != XmlNodeType.Element) break;

                var name = reader.Name;
                reader.ReadStartElement();
                var value = reader.ReadContentAsString();
                dic[name] = value;

                if (reader.NodeType == XmlNodeType.Attribute) reader.Read();
                if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();
            }
            if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();

            return dic;
        }

        /// <summary>把字典写入配置文件</summary>
        /// <param name="fileName"></param>
        /// <param name="source"></param>
        protected override void OnWrite(String fileName, IDictionary<String, String> source)
        {
            var set = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true
            };

            using var fs = File.OpenWrite(fileName);
            using var writer = XmlWriter.Create(fs, set);

            writer.WriteStartDocument();
            var name = Path.GetFileNameWithoutExtension(fileName);
            writer.WriteStartElement(name);

            foreach (var item in source)
            {
                writer.WriteStartElement(item.Key);
                writer.WriteValue(item.Value);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();

            // 截断文件
            fs.SetLength(fs.Position);
        }
    }
}