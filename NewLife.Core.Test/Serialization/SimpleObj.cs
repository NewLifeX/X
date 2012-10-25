using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NewLife.Serialization;

namespace NewLife.Core.Test.Serialization
{
    class SimpleObj : Obj
    {
        #region 属性
        private Boolean _B;
        /// <summary>属性说明</summary>
        public Boolean B { get { return _B; } set { _B = value; } }

        private Char _C;
        /// <summary>属性说明</summary>
        public Char C { get { return _C; } set { _C = value; } }

        private SByte _SB;
        /// <summary>属性说明</summary>
        public SByte SB { get { return _SB; } set { _SB = value; } }

        private Byte _Bt;
        /// <summary>属性说明</summary>
        public Byte Bt { get { return _Bt; } set { _Bt = value; } }

        private Int16 _I16;
        /// <summary>属性说明</summary>
        public Int16 I16 { get { return _I16; } set { _I16 = value; } }

        private UInt16 _U16;
        /// <summary>属性说明</summary>
        public UInt16 U16 { get { return _U16; } set { _U16 = value; } }

        private Int32 _I32;
        /// <summary>属性说明</summary>
        public Int32 I32 { get { return _I32; } set { _I32 = value; } }

        private UInt32 _U32;
        /// <summary>属性说明</summary>
        public UInt32 U32 { get { return _U32; } set { _U32 = value; } }

        private Int64 _I64;
        /// <summary>属性说明</summary>
        public Int64 I64 { get { return _I64; } set { _I64 = value; } }

        private UInt64 _U64;
        /// <summary>属性说明</summary>
        public UInt64 U64 { get { return _U64; } set { _U64 = value; } }

        private Single _S;
        /// <summary>属性说明</summary>
        public Single S { get { return _S; } set { _S = value; } }

        private Double _D;
        /// <summary>属性说明</summary>
        public Double D { get { return _D; } set { _D = value; } }

        private Decimal _Dec;
        /// <summary>属性说明</summary>
        public Decimal Dec { get { return _Dec; } set { _Dec = value; } }

        private DateTime _Dt;
        /// <summary>属性说明</summary>
        public DateTime Dt { get { return _Dt; } set { _Dt = value; } }

        private String _Str;
        /// <summary>属性说明</summary>
        public String Str { get { return _Str; } set { _Str = value; } }
        #endregion

        #region 方法
        public static SimpleObj Create()
        {
            var obj = new SimpleObj();
            obj.OnInit();

            return obj;
        }

        protected virtual void OnInit()
        {
            B = Rnd.Next(2) > 0;
            C = (Char)('A' + Rnd.Next(26));
            SB = (SByte)Rnd.Next(SByte.MinValue, SByte.MaxValue);
            Bt = (Byte)Rnd.Next(Byte.MinValue, Byte.MaxValue);
            I16 = (Int16)Rnd.Next(Int16.MinValue, Int16.MaxValue);
            U16 = (UInt16)Rnd.Next(UInt16.MinValue, UInt16.MaxValue);
            I32 = (Int32)Rnd.Next(Int32.MinValue, Int32.MaxValue);
            U32 = (UInt32)Rnd.Next(Int32.MinValue, Int32.MaxValue);
            I64 = (Int64)Rnd.Next(Int32.MinValue, Int32.MaxValue);
            U64 = (UInt64)Rnd.Next(Int32.MinValue, Int32.MaxValue);
            S = (Single)(Rnd.NextDouble() * Rnd.Next(Int32.MinValue, Int32.MaxValue));
            D = (Rnd.NextDouble() * Rnd.Next(Int32.MinValue, Int32.MaxValue));
            Dec = (Decimal)(Rnd.NextDouble() * Rnd.Next(Int32.MinValue, Int32.MaxValue));
            Dt = DateTime.Now.AddSeconds(D);

            if (Rnd.Next(2) > 0) Str = "Design By NewLife \r\n新生命开发团队\r\nhttp://www.NewLifeX.com";
        }

        public override void Write(BinaryWriter writer, BinarySettings set)
        {
            writer.Write(B);
            writer.Write(C);
            writer.Write(SB);
            writer.Write(Bt);

            var encodeInt = set.EncodeInt;
            if (!encodeInt)
            {
                writer.Write(I16);
                writer.Write(U16);
                writer.Write(I32);
                writer.Write(U32);
                writer.Write(I64);
                writer.Write(U64);
            }
            else
            {
                writer.Write(GetEncoded(I16));
                writer.Write(GetEncoded(U16));
                writer.Write(GetEncoded(I32));
                writer.Write(GetEncoded(U32));
                writer.Write(GetEncoded(I64));
                writer.Write(GetEncoded(U64));
            }

            writer.Write(S);
            writer.Write(D);
            if (!encodeInt)
            {
                writer.Write(Dec);
                writer.Write(Dt.Ticks);
            }
            else
            {
                var ns = Decimal.GetBits(Dec);
                for (int i = 0; i < ns.Length; i++)
                {
                    writer.Write(GetEncoded(ns[i]));
                }
                writer.Write(GetEncoded(Dt.Ticks));
            }
            if (Str == null) Str = "";
            //writer.Write(Str);
            var encodeSize = set.EncodeInt || (Int32)set.SizeFormat % 2 == 0;
            var buf = set.Encoding.GetBytes(Str);
            if (!encodeSize)
                writer.Write(buf.Length);
            else
                writer.Write(GetEncoded(buf.Length));
            writer.Write(buf, 0, buf.Length);
        }
        #endregion
    }
}