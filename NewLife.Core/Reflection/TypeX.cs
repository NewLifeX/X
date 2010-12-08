using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace NewLife.Reflection
{
    /// <summary>
    /// 类型辅助类
    /// </summary>
    public class TypeX
    {
        #region 属性
        private List<Assembly> _Asms;
        /// <summary>程序集集合</summary>
        public List<Assembly> Asms
        {
            get { return _Asms; }
            set { _Asms = value; }
        }
        #endregion

        #region 静态属性
        private static TypeX _Instance;
        /// <summary>实例</summary>
        public static TypeX Instance
        {
            get { return _Instance ?? (_Instance = new TypeX()); }
            set { _Instance = value; }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 加载程序集
        /// </summary>
        /// <param name="includeSystem">是否包含系统程序集</param>
        /// <returns></returns>
        public List<Assembly> LoadAssembly(Boolean includeSystem)
        {
            List<Assembly> asms = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
            if (!includeSystem)
            {
                for (int i = asms.Count - 1; i >= 0; i--)
                {
                    if (IsSystemAssembly(asms[i])) asms.RemoveAt(i);
                }
            }

            Asms = asms == null || asms.Count < 1 ? null : asms;

            return Asms;
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 是否系统类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Boolean IsSystemType(Type type)
        {
            return type.Assembly.FullName.EndsWith("PublicKeyToken=b77a5c561934e089");
        }

        /// <summary>
        /// 是否系统程序集
        /// </summary>
        /// <param name="asm"></param>
        /// <returns></returns>
        public static Boolean IsSystemAssembly(Assembly asm)
        {
            return asm.FullName.EndsWith("PublicKeyToken=b77a5c561934e089");
        }
        #endregion
    }
}
