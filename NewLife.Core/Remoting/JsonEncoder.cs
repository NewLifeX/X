using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Serialization;

namespace NewLife.Remoting
{
    class JsonEncoder : IEncoder
    {
        public T Decode<T>(Byte[] data)
        {
            return data.ToStr().ToJsonEntity<T>();
        }

        public Byte[] Encode(Object obj)
        {
            return obj.ToJson().GetBytes();
        }
    }
}