using NewLife.Serialization;

namespace XCode.Accessors
{
    class JsonEntityAccessor : SerializationEntityAccessorBase
    {
        protected override IWriter GetWriter() { return new JsonWriter(); }

        protected override IReader GetReader() { return new JsonReader(); }
    }
}