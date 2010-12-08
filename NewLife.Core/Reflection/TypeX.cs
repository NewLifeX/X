using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using NewLife.Collections;

namespace NewLife.Reflection
{
    /// <summary>
    /// 类型辅助类
    /// </summary>
    public class TypeX
    {
        #region 属性
        private Type _Type;
        /// <summary>类型</summary>
        public Type Type
        {
            get { return _Type; }
            //set { _Type = value; }
        }
        #endregion

        #region 扩展属性
        /// <summary>
        /// 是否系统类型
        /// </summary>
        /// <returns></returns>
        public Boolean IsSystemType
        {
            get
            {
                return Type.Assembly.FullName.EndsWith("PublicKeyToken=b77a5c561934e089");
            }
        }
        #endregion

        #region 构造
        private TypeX(Type type) { _Type = type; }

        private static DictionaryCache<Type, TypeX> cache = new DictionaryCache<Type, TypeX>();
        /// <summary>
        /// 创建类型辅助对象
        /// </summary>
        /// <param name="asm"></param>
        /// <returns></returns>
        public static TypeX Create(Type asm)
        {
            return cache.GetItem(asm, delegate(Type key)
            {
                return new TypeX(key);
            });
        }
        #endregion

        #region 方法
        /// <summary>
        /// 是否指定类型的插件
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Boolean IsPlugin(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            if (!Type.IsAssignableFrom(type)) return false;

            // 继续……
            //为空、不是类、抽象类、泛型类 都不是实体类
            if (!type.IsClass || type.IsAbstract || type.IsGenericType) return false;

            return true;
        }
        #endregion

        #region 辅助方法
        #endregion
    }
}
