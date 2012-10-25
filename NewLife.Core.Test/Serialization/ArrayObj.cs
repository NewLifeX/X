using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NewLife.Serialization;

namespace NewLife.Core.Test.Serialization
{
    class ArrayObj : Obj
    {
        private SimpleObj[] _Objs;
        /// <summary>属性说明</summary>
        public SimpleObj[] Objs { get { return _Objs; } set { _Objs = value; } }

        public ArrayObj()
        {
            var n = Rnd.Next(10);
            Objs = new SimpleObj[n];
            for (int i = 0; i < n; i++)
            {
                // 部分留空
                if (Rnd.Next(2) > 0)
                    Objs[i] = SimpleObj.Create();
            }
        }

        public override void Write(BinaryWriter writer, BinarySettings set)
        {
            var encodeSize = set.EncodeInt || (Int32)set.SizeFormat % 2 == 0;
            if (Objs == null)
            {
                if (!encodeSize)
                    writer.Write((Int32)0);
                else
                    writer.Write((Byte)0);
                return;
            }

            var n = Objs.Length;
            if (!encodeSize)
                writer.Write(n);
            else
                writer.Write(GetEncoded(n));

            foreach (var item in Objs)
            {
                item.Write(writer, set);
            }
        }

        public override bool CompareTo(Obj obj)
        {
            //return base.CompareTo(obj);
            var arr = obj as ArrayObj;
            if (arr == null) return false;

            if ((Objs == null || Objs.Length == 0) && (arr.Objs == null || arr.Objs.Length == 0)) return true;

            if (Objs.Length != arr.Objs.Length) return false;

            for (int i = 0; i < Objs.Length; i++)
            {
                if (!Objs[i].CompareTo(arr.Objs[i])) return false;
            }

            return true;
        }
    }
}
