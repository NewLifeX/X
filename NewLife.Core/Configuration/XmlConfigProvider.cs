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
        /// <summary>读取配置文件</summary>
        /// <param name="fileName">文件名</param>
        /// <param name="section">配置段</param>
        protected override void OnRead(String fileName, IConfigSection section)
        {
            using var fs = File.OpenRead(fileName);
            using var reader = XmlReader.Create(fs);

            var dic = new Dictionary<String, Object>();

            // 移动到第一个元素
            while (reader.NodeType != XmlNodeType.Element) reader.Read();

            reader.ReadStartElement();
            while (reader.NodeType == XmlNodeType.Comment || reader.NodeType == XmlNodeType.Whitespace) reader.Skip();

            ReadNode(reader, dic);

            if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();

            var rs = new Dictionary<String, ConfigSection>();
            Map(dic, rs, null);
        }

        private void ReadNode(XmlReader reader, IDictionary<String, Object> dic)
        {
            while (true)
            {
                if (reader.NodeType != XmlNodeType.Element) break;

                var name = reader.Name;

                reader.ReadStartElement();
                while (reader.NodeType == XmlNodeType.Comment || reader.NodeType == XmlNodeType.Whitespace) reader.Skip();

                if (reader.NodeType != XmlNodeType.Element)
                    dic[name] = reader.ReadContentAsString();
                else
                {
                    var dic2 = new Dictionary<String, Object>();
                    ReadNode(reader, dic2);

                    dic[name] = dic2;
                }

                if (reader.NodeType == XmlNodeType.Attribute) reader.Read();
                if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();
                while (reader.NodeType == XmlNodeType.Comment || reader.NodeType == XmlNodeType.Whitespace) reader.Skip();
            }
        }

        /// <summary>写入配置文件</summary>
        /// <param name="fileName">文件名</param>
        /// <param name="section">配置段</param>
        protected override void OnWrite(String fileName, IConfigSection section)
        {
            var set = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true
            };

            var rs = new Dictionary<String, Object>();
            //Map(source, rs);

            using var fs = File.OpenWrite(fileName);
            using var writer = XmlWriter.Create(fs, set);

            writer.WriteStartDocument();
            var name = Path.GetFileNameWithoutExtension(fileName);
            WriteNode(writer, name, rs);
            writer.WriteEndDocument();

            // 截断文件
            writer.Flush();
            fs.SetLength(fs.Position);
        }

        private void WriteNode(XmlWriter writer, String name, IDictionary<String, Object> source)
        {
            writer.WriteStartElement(name);

            foreach (var item in source)
            {
                if (item.Value is IDictionary<String, Object> dic)
                    WriteNode(writer, item.Key, dic);
                else
                {
                    writer.WriteStartElement(item.Key);
                    writer.WriteValue(item.Value);
                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();
        }
    }
}