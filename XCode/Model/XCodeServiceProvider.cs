using System;
using NewLife.Configuration;
using NewLife.Model;
using NewLife.Reflection;
using XCode.DataAccessLayer;

namespace XCode.Model
{
    /// <summary>
    /// XCode服务对象提供者
    /// </summary>
    class XCodeServiceProvider : ServiceProvider
    {
        #region 构造
        /// <summary>
        /// 实例化
        /// </summary>
        public XCodeServiceProvider() { }

        /// <summary>
        /// 通过指定一个基础提供者来实例化一个新的提供者，优先基础提供者
        /// </summary>
        /// <param name="provider"></param>
        public XCodeServiceProvider(IServiceProvider provider) : base(provider) { }
        #endregion

        #region 静态当前实例
        private static IServiceProvider _Current;
        /// <summary>默认服务对象提供者</summary>
        public new static IServiceProvider Current
        {
            get
            {
                if (_Current == null)
                {
                    // 从配置文件那默认提供者
                    String name = Config.GetConfig<String>("XCode.ServiceProvider");
                    if (!String.IsNullOrEmpty(name))
                    {
                        Type type = TypeX.GetType(name);
                        if (type != null)
                        {
                            //_Default = TypeX.CreateInstance(type) as IServiceProvider;
                            Object obj = TypeX.CreateInstance(type);
                            // 有可能提供者没有实现IServiceProvider接口，我们用鸭子类型给它处理一下
                            _Current = TypeX.ChangeType<IServiceProvider>(obj);

                            if (type != typeof(XCodeServiceProvider)) _Current = new XCodeServiceProvider(_Current);
                        }
                    }
                    if (_Current == null) _Current = new XCodeServiceProvider();
                }
                return _Current;
            }
            set { _Current = value; }
        }
        #endregion

        #region IServiceProvider 成员
        public override object GetService(Type serviceType)
        {
            Object obj = base.GetService(serviceType);
            if (obj != null) return obj;

            if (serviceType == typeof(IDataTable)) return new XTable();

            return null;
        }
        #endregion
    }
}