using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using NewLife.Collections;

namespace NewLife.Serialization
{
    /// <summary>Xml解析器，得到字典和数组</summary>
    public class XmlParser
    {
        #region 属性
        private readonly XmlReader _reader;
        #endregion

        /// <summary>实例化</summary>
        /// <param name="xml"></param>
        public XmlParser(String xml)
        {
            _reader = XmlReader.Create(new StringReader(xml));
        }

        /// <summary>解码</summary>
        /// <returns></returns>
        public static IDictionary<String, Object> Decode(String xml)
        {
            var parser = new XmlParser(xml);
            return parser.ParseValue();
        }

        private IDictionary<String, Object> ParseValue()
        {
            var reader = _reader;

            // 移动到第一个元素
            while (reader.NodeType != XmlNodeType.Element) reader.Read();

            reader.ReadStartElement();
            while (reader.NodeType == XmlNodeType.Whitespace) reader.Skip();

            var dic = ParseObject();

            if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();

            return dic;
        }

        private IDictionary<String, Object> ParseObject()
        {
            var reader = _reader;
            var dic = new NullableDictionary<String, Object>(StringComparer.OrdinalIgnoreCase);

            while (true)
            {
                while (reader.NodeType == XmlNodeType.Comment || reader.NodeType == XmlNodeType.Whitespace) reader.Skip();
                if (reader.NodeType != XmlNodeType.Element) break;

                var name = reader.Name;

                // 读取属性值
                if (reader.HasAttributes)
                {
                    reader.MoveToFirstAttribute();
                    do
                    {
                        dic[reader.Name] = reader.Value;
                    } while (reader.MoveToNextAttribute());
                }
                else
                    reader.ReadStartElement();
                while (reader.NodeType == XmlNodeType.Whitespace) reader.Skip();

                // 遇到下一层节点
                Object val = null;
                if (reader.NodeType == XmlNodeType.Element || reader.NodeType == XmlNodeType.Comment)
                    val = ParseObject();
                else if (reader.NodeType == XmlNodeType.Text)
                    val = reader.ReadContentAsString();

                // 如果该名字两次或多次出现，则认为是数组
                if (dic.TryGetValue(name, out var val2))
                {
                    if (val2 is IList<Object> list)
                        list.Add(val);
                    else
                        dic[name] = new List<Object> { val2, val };
                }
                else
                    dic[name] = val;

                if (reader.NodeType == XmlNodeType.Attribute) reader.Read();
                if (reader.NodeType == XmlNodeType.EndElement) reader.ReadEndElement();
                while (reader.NodeType == XmlNodeType.Whitespace) reader.Skip();
            }

            return dic;
        }
    }
}