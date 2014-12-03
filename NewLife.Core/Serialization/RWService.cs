using NewLife.Xml;

namespace NewLife.Serialization
{
    /// <summary>读写器服务。将来可以改为对象容器支持</summary>
    static class RWService
    {
        public static IReader CreateReader(RWKinds kind)
        {
            switch (kind)
            {
                case RWKinds.Binary:
                    return new BinaryReaderX();
                case RWKinds.Xml:
                    return new XmlReaderX();
                case RWKinds.Json:
                    return new JsonReader();
                default:
                    break;
            }
            return null;
        }

        public static IWriter CreateWriter(RWKinds kind)
        {
            switch (kind)
            {
                case RWKinds.Binary:
                    return new BinaryWriterX();
                case RWKinds.Xml:
                    return new XmlWriterX();
                case RWKinds.Json:
                    return new JsonWriter();
                default:
                    break;
            }
            return null;
        }

        public static RWKinds GetKind(this IReaderWriter rw)
        {
            var type = rw.GetType();
            if (type == typeof(BinaryReaderX)) return RWKinds.Binary;
            if (type == typeof(BinaryWriterX)) return RWKinds.Binary;
            if (type == typeof(XmlReaderX)) return RWKinds.Xml;
            if (type == typeof(XmlWriterX)) return RWKinds.Xml;
            if (type == typeof(JsonReader)) return RWKinds.Json;
            if (type == typeof(JsonWriter)) return RWKinds.Json;

            throw new XException("未识别的读写器类型{0}！", type);
        }
    }
}