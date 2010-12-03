using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 测试
    /// </summary>
    public static class ProtocolTest
    {
        /// <summary>
        /// 测试
        /// </summary>
        public static void Test()
        {
            MemoryStream stream = new MemoryStream();
            Response response = new Response();
            response.iii = 888;
            response.i64 = Int64.MaxValue / 2;
            response.time = DateTime.Now;
            response.Str = "大石头";
            response.Public = new IPEndPoint(IPAddress.Loopback, 1234);
            response.Private = new List<IPEndPoint>();
            response.Private.Add(response.Public);
            response.Private.Add(new IPEndPoint(IPAddress.Broadcast, 2));
            response.Private.Add(new IPEndPoint(IPAddress.IPv6Any, 33));

            ProtocolFormatter pf = new ProtocolFormatter();
            pf.Head.Config.NoHead = true;
            pf.Head.Config.EncodeInt = true;
            pf.Serialize(stream, response);

            Byte[] buffer = stream.ToArray();
            Console.WriteLine("[{0}] {1}", buffer.Length, BitConverter.ToString(buffer).Replace("-", " "));
            File.WriteAllBytes("Protocol.dat", buffer);

            stream = new MemoryStream(buffer);
            pf = new ProtocolFormatter();
            pf.Head.Config.NoHead = true;
            pf.Head.Config.EncodeInt = true;
            response = new Response();
            pf.Deserialize(stream, response);

            Console.WriteLine(response.Str);
        }

        #region 响应
        /// <summary>
        /// 邀请响应
        /// </summary>
        //[ProtocolUseRefObject]
        public class Response : IProtocolSerializable
        {
            private Int32 _iii;
            /// <summary>属性说明</summary>
            public Int32 iii
            {
                get { return _iii; }
                set { _iii = value; }
            }

            private Int64 _i64;
            /// <summary>属性说明</summary>
            public Int64 i64
            {
                get { return _i64; }
                set { _i64 = value; }
            }

            private DateTime _time;
            /// <summary>属性说明</summary>
            public DateTime time
            {
                get { return _time; }
                set { _time = value; }
            }

            private String _Str;
            /// <summary>属性说明</summary>
            public String Str
            {
                get { return _Str; }
                set { _Str = value; }
            }

            private List<IPEndPoint> _Private;
            /// <summary>对方的私有地址</summary>
            public List<IPEndPoint> Private
            {
                get { return _Private; }
                set { _Private = value; }
            }

            private IPEndPoint _Public;
            /// <summary>我的共有地址</summary>
            public IPEndPoint Public
            {
                get { return _Public; }
                set { _Public = value; }
            }

            #region IProtocolSerializable 成员

            bool IProtocolSerializable.OnSerializing(WriteContext context)
            {
                return true;
            }

            void IProtocolSerializable.OnSerialized(WriteContext context)
            {
            }

            bool IProtocolSerializable.OnDeserializing(ReadContext context)
            {
                return true;
            }

            void IProtocolSerializable.OnDeserialized(ReadContext context)
            {
            }

            object IProtocolSerializable.OnCreateInstance(ReadContext context, Type type)
            {
                if (type == typeof(IPEndPoint)) return new IPEndPoint(IPAddress.Any, 0);

                return null;
            }

            #endregion
        }
        #endregion
    }
}
