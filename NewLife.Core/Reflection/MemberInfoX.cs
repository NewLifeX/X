using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace NewLife.Reflection
{
    /// <summary>
    /// 快速访问成员
    /// </summary>
    public abstract class MemberInfoX
    {
        #region 属性
        private MemberInfo _Member;
        /// <summary>成员</summary>
        public MemberInfo Member
        {
            get { return _Member; }
            set { _Member = value; }
        }
        #endregion

        #region 扩展属性
        /// <summary>
        /// 成员类型
        /// </summary>
        public virtual Type Type
        {
            get
            {
                if (Member == null) return null;
                switch (Member.MemberType)
                {
                    case MemberTypes.Field:
                        return (Member as FieldInfo).FieldType;
                    case MemberTypes.Method:
                        return (Member as MethodInfo).ReturnType;
                    case MemberTypes.Property:
                        return (Member as PropertyInfo).PropertyType;
                    case MemberTypes.TypeInfo:
                        return Member as Type;
                    default:
                        break;
                }
                return null;
            }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="member"></param>
        protected MemberInfoX(MemberInfo member) { Member = member; }
        #endregion

        #region 调用
        /// <summary>
        /// 执行方法
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public virtual Object Invoke(Object obj, Object[] parameters) { throw new NotImplementedException(); }

        /// <summary>
        /// 取值
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual Object GetValue(Object obj) { throw new NotImplementedException(); }

        /// <summary>
        /// 赋值
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public virtual void SetValue(Object obj, Object value) { throw new NotImplementedException(); }
        #endregion

        #region 类型转换
        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator MemberInfo(MemberInfoX obj)
        {
            return obj != null ? obj.Member : null;
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator MemberInfoX(MemberInfo obj)
        {
            if (obj == null) return null;

            switch (obj.MemberType)
            {
                case MemberTypes.All:
                    break;
                case MemberTypes.Constructor:
                    break;
                case MemberTypes.Custom:
                    break;
                case MemberTypes.Event:
                    break;
                case MemberTypes.Field:
                    return FieldInfoX.Create(obj as FieldInfo);
                case MemberTypes.Method:
                    return MethodInfoX.Create(obj as MethodInfo);
                case MemberTypes.NestedType:
                    break;
                case MemberTypes.Property:
                    return PropertyInfoX.Create(obj as PropertyInfo);
                case MemberTypes.TypeInfo:
                    break;
                default:
                    break;
            }
            return null;
        }
        #endregion

        #region 重载
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return (Type != null ? Type.Name : null) + "." + (Member != null ? Member.Name : null);
        }
        #endregion
    }
}
