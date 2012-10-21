using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NewLife.Serialization;

namespace NewLife.Core.Test.Serialization
{
    class DictionaryObj : Obj
    {
        private Dictionary<Int32, SimpleObj> _Objs;
        /// <summary>属性说明</summary>
        public Dictionary<Int32, SimpleObj> Objs { get { return _Objs; } set { _Objs = value; } }

        public DictionaryObj()
        {
            var n = Rnd.Next(100);
            Objs = new Dictionary<Int32, SimpleObj>();
            for (int i = 0; i < n; i++)
            {
                Objs.Add(i, SimpleObj.Create());
            }
        }

        public override void Write(BinaryWriter writer, BinarySettings set)
        {
            if (Objs == null) return;

            var n = Objs.Count;
            if (!set.EncodeInt && set.SizeFormat != TypeCode.UInt16 && set.SizeFormat != TypeCode.UInt32 && set.SizeFormat != TypeCode.UInt64)
                writer.Write(n);
            else
                writer.Write(WriteEncoded(n));

            foreach (var item in Objs)
            {
                if (!set.EncodeInt)
                    writer.Write(item.Key);
                else
                    writer.Write(WriteEncoded(item.Key));

                item.Value.Write(writer, set);
            }
        }
    }
}
