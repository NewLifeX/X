using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>
    /// 反射成员信息
    /// </summary>
    class ReflectMemberInfo : IObjectMemberInfo
    {
        #region 属性
        private MemberInfo _Member;
        /// <summary>成员</summary>
        public MemberInfo Member
        {
            get { return _Member; }
            private set { _Member = value; }
        }

        private MemberInfoX _Mix;
        /// <summary>快速反射</summary>
        private MemberInfoX Mix
        {
            get { return _Mix ?? (_Mix = Member); }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="member"></param>
        public ReflectMemberInfo(MemberInfo member)
        {
            Member = member;
        }
        #endregion

        #region IObjectMemberInfo 成员
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get { return Member.Name; } }

        /// <summary>
        /// 类型
        /// </summary>
        public Type Type { get { return Mix.Type; } }

        /// <summary>
        /// 对目标对象取值赋值
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public object this[object target]
        {
            get
            {
                return Mix.GetValue(target);
            }
            set
            {
                Mix.SetValue(target, value);
            }
        }

        /// <summary>
        /// 是否可读
        /// </summary>
        public bool CanRead
        {
            get
            {
                if (Member.MemberType == MemberTypes.Field) return true;
                if (Member.MemberType == MemberTypes.Property) return (Member as PropertyInfo).CanRead;
                return false;
            }
        }

        /// <summary>
        /// 是否可写
        /// </summary>
        public bool CanWrite
        {
            get
            {
                if (Member.MemberType == MemberTypes.Field) return true;
                if (Member.MemberType == MemberTypes.Property) return (Member as PropertyInfo).CanWrite;
                return false;
            }
        }
        #endregion

        #region 已重载
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }
        #endregion
    }
}