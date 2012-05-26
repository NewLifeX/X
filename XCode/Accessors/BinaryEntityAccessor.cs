using NewLife.Serialization;

namespace XCode.Accessors
{
    class BinaryEntityAccessor : SerializationEntityAccessorBase
    {
        /// <summary>种类</summary>
        public override EntityAccessorTypes Kind { get { return EntityAccessorTypes.Binary; } }

        protected override IWriter GetWriter() { return new BinaryWriterX(); }

        protected override IReader GetReader() { return new BinaryReaderX(); }
    }
}