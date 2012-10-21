using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace NewLife.Core.Test.Serialization
{
    class SimpleObj
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
            B = true;
            C = 'X';
            SB = SByte.MaxValue;
            Bt = 123;
            I16 = 16;
            U16 = 99;
            I32 = 32;
            U32 = 999;
            I64 = 64;
            U64 = 99999;
            S = 123.456F;
            D = 456.890123456;
            Dec = 99.123456789M;
            Dt = DateTime.Now;
            Str = "Design By NewLife \r\nhttp://www.NewLifeX.com";
        }

        public virtual Stream GetBinaryStream()
        {
            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);
            writer.Write(B);
            writer.Write(C);
            writer.Write(SB);
            writer.Write(Bt);
            writer.Write(I16);
            writer.Write(U16);
            writer.Write(I32);
            writer.Write(U32);
            writer.Write(I64);
            writer.Write(U64);
            writer.Write(S);
            writer.Write(D);
            writer.Write(Dec);
            writer.Write(Dt.Ticks);
            writer.Write(Str);

            ms.Position = 0;

            return ms;
        }
        #endregion
    }
}