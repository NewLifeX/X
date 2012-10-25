using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using NewLife.Serialization;
using NewLife.Reflection;
using System.Reflection;

namespace NewLife.Core.Test.Serialization
{
    class ExtendObj : SimpleObj
    {
        #region 属性
        private Byte[] _Bts;
        /// <summary>属性说明</summary>
        public Byte[] Bts { get { return _Bts; } set { _Bts = value; } }

        private Char[] _Cs;
        /// <summary>属性说明</summary>
        public Char[] Cs { get { return _Cs; } set { _Cs = value; } }

        private Guid _G;
        /// <summary>属性说明</summary>
        public Guid G { get { return _G; } set { _G = value; } }

        private IPAddress _Address;
        /// <summary>属性说明</summary>
        public IPAddress Address { get { return _Address; } set { _Address = value; } }

        private IPEndPoint _EndPoint;
        /// <summary>属性说明</summary>
        public IPEndPoint EndPoint { get { return _EndPoint; } set { _EndPoint = value; } }

        private Type _T;
        /// <summary>属性说明</summary>
        public Type T { get { return _T; } set { _T = value; } }
        #endregion

        #region 方法
        public new static ExtendObj Create()
        {
            var obj = new ExtendObj();
            obj.OnInit();

            return obj;
        }

        protected override void OnInit()
        {
            base.OnInit();

            var r = Rnd;
            if (r.Next(10) == 0) return;

            // 减去1，可能出现-1，这样子就做到可能有0字节数组，也可能为null
            var n = r.Next(256) - 1;
            if (n >= 0)
            {
                Bts = new Byte[n];
                r.NextBytes(Bts);
            }

            n = r.Next(10);
            if (n > 0)
            {
                if (Str != null)
                    Cs = Str.ToArray();
                else
                    Cs = new Char[0];
            }

            if (r.Next(10) > 0) G = Guid.NewGuid();

            if (r.Next(10) > 0)
            {
                var buf = new Byte[r.Next(4)];
                r.NextBytes(buf);
                Address = new IPAddress(buf);
                EndPoint = new IPEndPoint(Address, r.Next(65536));
            }

            if (r.Next(10) > 0)
            {
                var ts = typeof(IReaderWriter).Assembly.GetTypes();
                T = ts[r.Next(ts.Length)];
            }
        }

        public override void Write(BinaryWriter writer, BinarySettings set)
        {
            base.Write(writer, set);

            var encodeInt = set.EncodeInt;
            if (Bts == null)
            {
                if (!encodeInt)
                    writer.Write((Int32)0);
                else
                    writer.Write((Byte)0);
            }
            else
            {
                if (!encodeInt)
                    writer.Write(Bts.Length);
                else
                    writer.Write(WriteEncoded(Bts.Length));
                writer.Write(Bts);
            }

            if (Cs == null)
            {
                if (!encodeInt)
                    writer.Write((Int32)0);
                else
                    writer.Write((Byte)0);
            }
            else
            {
                var buf = set.Encoding.GetBytes(Cs);
                if (!encodeInt)
                    writer.Write(buf.Length);
                else
                    writer.Write(WriteEncoded(buf.Length));
                writer.Write(buf);
            }

            writer.Write(G.ToByteArray());

            if (Address == null)
            {
                if (!encodeInt)
                    writer.Write((Int32)0);
                else
                    writer.Write((Byte)0);
            }
            else
            {
                var buf = Address.GetAddressBytes();
                if (!encodeInt)
                    writer.Write(buf.Length);
                else
                    writer.Write(WriteEncoded(buf.Length));
                writer.Write(buf);
            }

            if (EndPoint == null)
            {
                if (!encodeInt)
                    writer.Write((Int32)0);
                else
                    writer.Write((Byte)0);
            }
            else
            {
                var buf = EndPoint.Address.GetAddressBytes();
                if (!encodeInt)
                    writer.Write(buf.Length);
                else
                    writer.Write(WriteEncoded(buf.Length));
                writer.Write(buf);
                if (!encodeInt)
                    writer.Write(EndPoint.Port);
                else
                    writer.Write(WriteEncoded(EndPoint.Port));
            }

            if (T == null)
            {
                if (!encodeInt)
                    writer.Write((Int32)0);
                else
                    writer.Write((Byte)0);
            }
            else
            {
                writer.Write(T.FullName);
            }
        }
        #endregion
    }
}