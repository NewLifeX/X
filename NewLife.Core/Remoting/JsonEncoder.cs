using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Serialization;

namespace NewLife.Remoting
{
    class JsonEncoder : IEncoder
    {
        public T Decode<T>(Byte[] data)
        {
            var json = data.ToStr();

            XTrace.WriteLine("<={0}", json);

            return json.ToJsonEntity<T>();
        }

        public Byte[] Encode(Object obj)
        {
            var json = obj.ToJson();

            XTrace.WriteLine("=>{0}", json);

            return json.GetBytes();
        }
    }
}