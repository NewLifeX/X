using System;
using System.IO;
using NewLife.Serialization;

namespace NewLife.Core.Test.Serialization
{
    class FixedSizeObj : Obj
    {
        [FieldSize(8)]
        private Byte[] _Data;
        /// <summary>属性说明</summary>
        public Byte[] Data { get { return _Data; } set { _Data = value; } }

        public FixedSizeObj()
        {
            _Data = new Byte[8];
            Rnd.NextBytes(_Data);
        }

        public override void Write(BinaryWriter writer, BinarySettings set)
        {
            writer.Write(Data, 0, Data.Length);
        }

        public override bool CompareTo(Obj obj)
        {
            var arr = obj as FixedSizeObj;
            if (arr == null) return false;

            return this.Data.CompareTo(arr.Data) == 0;
        }
    }
}