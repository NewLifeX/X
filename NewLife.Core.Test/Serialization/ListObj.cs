using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Serialization;

namespace NewLife.Core.Test.Serialization
{
    public class ListObj : Obj
    {
        private List<SimpleObj> _Objs;
        /// <summary>属性说明</summary>
        public List<SimpleObj> Objs { get { return _Objs; } set { _Objs = value; } }

        public static ListObj Create()
        {
            var obj = new ListObj();
            obj.OnInit();

            return obj;
        }

        void OnInit()
        {
            var n = Rnd.Next(10);
            Objs = new List<SimpleObj>();
            SimpleObj obj = null;
            for (int i = 0; i < n; i++)
            {
                // 部分留空，部分是上一次
                var m = Rnd.Next(3);
                if (m == 0)
                    Objs.Add(obj = SimpleObj.Create());
                else if (m == 1)
                    Objs.Add(obj);
                else
                    Objs.Add(null);
            }
            // 确保有一个引用
            Objs.Add(obj);
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
            foreach (var item in Objs)
            {
                //if (item != null)
                //{
                //    if (set.UseObjRef) writer.WriteInt(idx++, encodeSize);
                //    item.Write(writer, set);
                //}
                //else
                //    writer.WriteInt((Int32)0, encodeSize);
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
            var arr = obj as ListObj;
            if (arr == null) return false;

            if ((Objs == null || Objs.Count == 0) && (arr.Objs == null || arr.Objs.Count == 0)) return true;

            if (Objs.Count != arr.Objs.Count) return false;

            for (int i = 0; i < Objs.Count; i++)
            {
                if (Objs[i] == null)
                {
                    if (arr.Objs[i] == null)
                        continue;
                    else
                        return false;
                }
                if (!Objs[i].CompareTo(arr.Objs[i])) return false;
            }

            return true;
        }
    }
}
