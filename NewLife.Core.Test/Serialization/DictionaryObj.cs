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
            var n = Rnd.Next(10);
            Objs = new Dictionary<Int32, SimpleObj>();
            for (int i = 0; i < n; i++)
            {
                // 部分留空
                if (Rnd.Next(2) > 0)
                    Objs.Add(i, SimpleObj.Create());
                else
                    Objs.Add(i, null);
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
            var idx = 2;
            if (set.UseObjRef) writer.WriteInt(idx++, encodeSize);

            var n = Objs.Count;
            writer.WriteInt(n, encodeSize);

            foreach (var item in Objs)
            {
                writer.WriteInt(item.Key, set.EncodeInt);

                //item.Value.Write(writer, set);
                if (item.Value != null)
                {
                    if (set.UseObjRef) writer.WriteInt(idx++, encodeSize);
                    item.Value.Write(writer, set);
                }
                else
                    writer.WriteInt((Int32)0, encodeSize);
            }
        }

        public override bool CompareTo(Obj obj)
        {
            //return base.CompareTo(obj);
            var arr = obj as DictionaryObj;
            if (arr == null) return false;

            if ((Objs == null || Objs.Count == 0) && (arr.Objs == null || arr.Objs.Count == 0)) return true;

            if (Objs.Count != arr.Objs.Count) return false;

            foreach (var item in Objs)
            {
                SimpleObj sb;
                // 不存在？
                if (!arr.Objs.TryGetValue(item.Key, out sb)) return false;

                // 很小的可能相等，两者可能都是null
                if (sb == null)
                {
                    if (item.Value == null)
                        continue;
                    else
                        return false;
                }

                if (!sb.CompareTo(item.Value)) return false;
            }

            return true;
        }
    }
}
