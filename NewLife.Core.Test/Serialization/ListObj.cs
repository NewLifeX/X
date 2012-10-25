using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NewLife.Serialization;

namespace NewLife.Core.Test.Serialization
{
    class ListObj : Obj
    {
        private List<SimpleObj> _Objs;
        /// <summary>属性说明</summary>
        public List<SimpleObj> Objs { get { return _Objs; } set { _Objs = value; } }

        public ListObj()
        {
            var n = Rnd.Next(10);
            Objs = new List<SimpleObj>();
            for (int i = 0; i < n; i++)
            {
                // 部分留空
                if (Rnd.Next(2) > 0)
                    Objs.Add(SimpleObj.Create());
                else
                    Objs.Add(null);
            }
        }

        public override void Write(BinaryWriter writer, BinarySettings set)
        {
            var encodeSize = set.EncodeInt || (Int32)set.SizeFormat % 2 == 0;
            if (Objs == null)
            {
                writer.WriteInt((Int32)0, encodeSize);
                return;
            }

            var n = Objs.Count;
            writer.WriteInt(n, encodeSize);

            foreach (var item in Objs)
            {
                item.Write(writer, set);
            }
        }

        public override bool CompareTo(Obj obj)
        {
            //return base.CompareTo(obj);
            var arr = obj as ListObj;
            if (arr == null) return false;

            if ((Objs == null || Objs.Count == 0) && (arr.Objs == null || arr.Objs.Count == 0)) return true;

            if (Objs.Count != arr.Objs.Count) return false;

            for (int i = 0; i < Objs.Count; i++)
            {
                if (!Objs[i].CompareTo(arr.Objs[i])) return false;
            }

            return true;
        }
    }
}
