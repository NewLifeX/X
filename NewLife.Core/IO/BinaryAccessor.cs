using System;
using NewLife.Reflection;

namespace NewLife.IO
{
    /// <summary>
    /// 二进制数据访问器
    /// </summary>
    [Serializable]
    public class BinaryAccessor : FastIndexAccessor, IBinaryAccessor
    {
        #region 读写
        /// <summary>
        /// 从读取器中读取数据到对象
        /// </summary>
        /// <param name="reader">读取器</param>
        public virtual void Read(BinaryReaderX reader)
        {
            //Read(this, reader, true, true, false);
            Object value = null;
            reader.TryReadObject(this, TypeX.Create(this.GetType()), null, true, false, false, out value, ReadMember);
        }

        /// <summary>
        /// 尝试读取目标对象指定成员的值，处理基础类型、特殊类型、基础类型数组、特殊类型数组，通过委托方法处理成员
        /// </summary>
        /// <remarks>
        /// 简单类型在value中返回，复杂类型直接填充target；
        /// </remarks>
        /// <param name="reader">读取器</param>
        /// <param name="target">目标对象</param>
        /// <param name="member">成员</param>
        /// <param name="type">成员类型，以哪一种类型读取</param>
        /// <param name="encodeInt">是否编码整数</param>
        /// <param name="allowNull">是否允许空</param>
        /// <param name="isProperty">是否处理属性</param>
        /// <param name="value">成员值</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        protected virtual Boolean ReadMember(BinaryReaderX reader, Object target, MemberInfoX member, Type type, Boolean encodeInt, Boolean allowNull, Boolean isProperty, out Object value, BinaryReaderX.ReadCallback callback)
        {
            if (type == null) type = member.Type;

            // 接口支持
            if (typeof(IBinaryAccessor).IsAssignableFrom(type))
            {
                // 读取对象
                value = member.GetValue(target);

                // 实例化对象
                if (value == null)
                {
                    //value = Activator.CreateInstance(member.Type);
                    value = TypeX.CreateInstance(type);
                    //member.SetValue(target, value);
                }
                if (value == null) return false;

                // 调用接口
                IBinaryAccessor accessor = value as IBinaryAccessor;
                accessor.Read(reader);

                return true;
            }

            return reader.TryReadObject(target, member, type, encodeInt, true, isProperty, out value, callback);
        }

        /// <summary>
        /// 创建指定类型的实例
        /// </summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        protected virtual Object CreateInstance(Type type)
        {
            //return Activator.CreateInstance(type);
            return TypeX.CreateInstance(type);
        }

        /// <summary>
        /// 把对象数据写入到写入器
        /// </summary>
        /// <param name="writer">写入器</param>
        public virtual void Write(BinaryWriterX writer)
        {
            //Write(this, writer, true, true, false);
            writer.WriteObject(this, TypeX.Create(this.GetType()), true, false, false, WriteMember);
        }

        /// <summary>
        /// 把对象写入数据流，处理基础类型、特殊类型、基础类型数组、特殊类型数组，通过委托方法处理成员
        /// </summary>
        /// <param name="writer">写入器</param>
        /// <param name="target">目标对象</param>
        /// <param name="member">成员</param>
        /// <param name="encodeInt">是否编码整数</param>
        /// <param name="allowNull">是否允许空</param>
        /// <param name="isProperty">是否处理属性</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        protected virtual Boolean WriteMember(BinaryWriterX writer, Object target, MemberInfoX member, Boolean encodeInt, Boolean allowNull, Boolean isProperty, BinaryWriterX.WriteCallback callback)
        {
            Type type = member.Type;
            Object value = member.IsType ? target : member.GetValue(target);

            if (value != null) type = value.GetType();

            // 接口支持
            if (value != null && typeof(IBinaryAccessor).IsAssignableFrom(type))
            {
                // 调用接口
                IBinaryAccessor accessor = value as IBinaryAccessor;
                accessor.Write(writer);
                return true;
            }

            return writer.WriteObject(target, member, encodeInt, true, isProperty, callback);
        }
        #endregion
    }
}