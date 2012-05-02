using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Xml;

namespace NewLife.Serialization
{
    class RWService
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
    }
}