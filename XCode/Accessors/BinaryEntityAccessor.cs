using NewLife.Serialization;

namespace XCode.Accessors
{
    class BinaryEntityAccessor : SerializationEntityAccessorBase
    {
        protected override IWriter GetWriter() { return new BinaryWriterX(); }

        protected override IReader GetReader() { return new BinaryReaderX(); }
    }
}