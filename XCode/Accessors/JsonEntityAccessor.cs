using NewLife.Serialization;

namespace XCode.Accessors
{
    class JsonEntityAccessor : SerializationEntityAccessorBase
    {
        /// <summary>种类</summary>
        public override EntityAccessorTypes Kind { get { return EntityAccessorTypes.Json; } }

        protected override IWriter GetWriter() { return new JsonWriter(); }

        protected override IReader GetReader() { return new JsonReader(); }
    }
}