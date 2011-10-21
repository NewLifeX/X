using NewLife.Serialization;
using NewLife.Xml;

namespace XCode.Accessors
{
    class XmlEntityAccessor : SerializationEntityAccessorBase
    {
        protected override IWriter GetWriter() { return new XmlWriterX(); }

        protected override IReader GetReader() { return new XmlReaderX(); }
    }
}