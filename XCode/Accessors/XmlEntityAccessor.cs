using NewLife.Serialization;
using NewLife.Xml;

namespace XCode.Accessors
{
    class XmlEntityAccessor : SerializationEntityAccessorBase
    {
        /// <summary>种类</summary>
        public override EntityAccessorTypes Kind { get { return EntityAccessorTypes.Xml; } }

        protected override IWriter GetWriter() { return new XmlWriterX(); }

        protected override IReader GetReader() { return new XmlReaderX(); }
    }
}