using System;
using System.IO;
using NewLife.Serialization;

namespace NewLife.Core.Test.Serialization
{
    public class AbstractObj : Obj
    {
        #region 属性
        private Obj _Value;
        /// <summary>属性说明</summary>
        public Obj Value { get { return _Value; } set { _Value = value; } }
        #endregion

        #region 方法
        public override void Write(BinaryWriter writer, BinarySettings set)
        {
            var encodeSize = set.EncodeInt || ((Int32)set.SizeFormat % 2 == 0);
            var idx = 2;

            if (Value == null)
                writer.WriteInt((Int32)0, encodeSize);
            else
            {
                // 写入对象引用
                if (set.UseObjRef) writer.WriteInt(idx++, encodeSize);
                // 写入类型的对象引用
                if (set.UseObjRef) writer.WriteInt(idx++, encodeSize);
                if (set.SplitComplexType) writer.Write((Byte)BinarySettings.TypeKinds.Normal);
                var type = Value.GetType();
                if (set.UseTypeFullName)
                    writer.WriteString(type.AssemblyQualifiedName, set.Encoding, encodeSize);
                else
                    writer.WriteString(type.FullName, set.Encoding, encodeSize);
                (Value as Obj).Write(writer, set);
            }
        }
        #endregion
    }
}