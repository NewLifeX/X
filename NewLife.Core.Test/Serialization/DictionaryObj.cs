using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Serialization;
using NewLife.Xml;

namespace NewLife.Core.Test.Serialization
{
    public class DictionaryObj : Obj
    {
        private SerializableDictionary<Int32, SimpleObj> _Objs;
        /// <summary>属性说明</summary>
        public SerializableDictionary<Int32, SimpleObj> Objs { get { return _Objs; } set { _Objs = value; } }

        public static DictionaryObj Create()
        {
            var obj = new DictionaryObj();
            obj.OnInit();

            return obj;
        }

        void OnInit()
        {
            var n = Rnd.Next(10);
            Objs = new SerializableDictionary<Int32, SimpleObj>();
            SimpleObj obj = null;
            for (int i = 0; i < n; i++)
            {
                // 部分留空，部分是上一次
                var m = Rnd.Next(3);
                if (m == 0)
                    Objs.Add(i, obj = SimpleObj.Create());
                else if (m == 1)
                    Objs.Add(i, obj);
                else
                    Objs.Add(i, null);
            }
            // 确保有一个引用
            Objs.Add(n, obj);
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

            var bs = new List<SimpleObj>();
            foreach (var kv in Objs)
            {
                writer.WriteInt(kv.Key, set.EncodeInt);

                ////item.Value.Write(writer, set);
                //if (item.Value != null)
                //{
                //    if (set.UseObjRef) writer.WriteInt(idx++, encodeSize);
                //    item.Value.Write(writer, set);
                //}
                //else
                //    writer.WriteInt((Int32)0, encodeSize);

                var item = kv.Value;
                if (item == null)
                {
                    writer.WriteInt((Int32)0, encodeSize);
                    continue;
                }

                if (!set.UseObjRef)
                {
                    item.Write(writer, set);
                    continue;
                }

                var p = bs.IndexOf(item);
                if (p < 0)
                {
                    bs.Add(item);
                    p = bs.Count - 1;

                    writer.WriteInt(idx + p, encodeSize);
                    item.Write(writer, set);
                }
                else
                {
                    writer.WriteInt(idx + p, encodeSize);
                }
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