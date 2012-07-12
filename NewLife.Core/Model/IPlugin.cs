using System;
using System.Collections.Generic;
using NewLife.Linq;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Model
{
    /// <summary>通用插件接口</summary>
    /// <remarks>
    /// 为了方便构建一个简单通用的插件系统，先规定如下：
    /// 1，负责加载插件的宿主，在加载插件后会进行插件实例化，此时可在插件构造函数中做一些事情，但不应该开始业务处理，因为宿主的准备工作可能尚未完成
    /// 2，宿主一切准备就绪后，会顺序调用插件的Init方法，并将宿主标识传入，插件通过标识区分是否自己的目标宿主。插件的Init应尽快完成。
    /// 3，如果插件实现了<see cref="IDisposable"/>接口，宿主最后会清理资源。
    /// </remarks>
    public interface IPlugin
    {
        /// <summary>初始化</summary>
        /// <param name="identity">插件宿主标识</param>
        /// <param name="provider">服务提供者</param>
        /// <returns>返回初始化是否成功。如果当前宿主不是所期待的宿主，这里返回false</returns>
        Boolean Init(String identity, IServiceProvider provider);
    }

    /// <summary>插件特性。用于判断某个插件实现类是否支持某个宿主</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PluginAttribute : Attribute
    {
        private String _Identity;
        /// <summary>插件宿主标识</summary>
        public String Identity { get { return _Identity; } set { _Identity = value; } }

        /// <summary>实例化</summary>
        /// <param name="identity"></param>
        public PluginAttribute(String identity) { Identity = identity; }
    }

    /// <summary>插件管理器</summary>
    public class PluginManager : DisposeBase, IServiceProvider
    {
        #region 属性
        private String _Identity;
        /// <summary>宿主标识，用于供插件区分不同宿主</summary>
        public String Identity { get { return _Identity; } set { _Identity = value; } }

        private IServiceProvider _Provider;
        /// <summary>宿主服务提供者</summary>
        public IServiceProvider Provider { get { return _Provider; } set { _Provider = value; } }

        private List<IPlugin> _Plugins;
        /// <summary>插件集合</summary>
        public List<IPlugin> Plugins { get { return _Plugins ?? (_Plugins = new List<IPlugin>()); } }
        #endregion

        #region 构造
        /// <summary>实例化一个插件管理器</summary>
        public PluginManager() { }

        /// <summary>使用宿主对象实例化一个插件管理器</summary>
        /// <param name="host"></param>
        public PluginManager(Object host)
        {
            if (host != null)
            {
                Identity = host.ToString();
                Provider = host as IServiceProvider;
            }
        }

        /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）。
        /// 因为该方法只会被调用一次，所以该参数的意义不太大。</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (disposing)
            {
                var ps = _Plugins;
                _Plugins = null;
                if (ps != null && ps.Count > 0)
                {
                    foreach (var item in ps)
                    {
                        if (item is IDisposable) (item as IDisposable).Dispose();
                    }
                }
            }
        }
        #endregion

        #region 方法
        /// <summary>加载插件。此时是加载所有插件，无法识别哪些是需要的</summary>
        public void Load()
        {
            var list = new List<IPlugin>();
            // 此时是加载所有插件，无法识别哪些是需要的
            foreach (var item in LoadPlugins())
            {
                if (item != null)
                {
                    list.Add(TypeX.CreateInstance(item) as IPlugin);
                }
            }
            _Plugins = list;
        }

        IList<Type> pluginTypes;
        IEnumerable<Type> LoadPlugins()
        {
            if (pluginTypes != null) return pluginTypes;

            var list = new List<Type>();
            // 此时是加载所有插件，无法识别哪些是需要的
            foreach (var item in AssemblyX.FindAllPlugins(typeof(IPlugin), true))
            {
                if (item != null)
                {
                    // 如果有插件特性，并且所有特性都不支持当前宿主，则跳过
                    var atts = item.GetCustomAttributes<PluginAttribute>(true);
                    if (atts != null && atts.Any(a => a.Identity != Identity)) continue;

                    list.Add(item);
                }
            }
            return pluginTypes = list;
        }

        /// <summary>开始初始化。初始化之后，不属于当前宿主的插件将会被过滤掉</summary>
        public void Init()
        {
            var ps = Plugins;
            if (ps == null || ps.Count < 1) return;

            for (int i = ps.Count - 1; i >= 0; i--)
            {
                try
                {
                    if (!ps[i].Init(Identity, Provider)) ps.RemoveAt(i);
                }
                catch (Exception ex)
                {
                    XTrace.WriteExceptionWhenDebug(ex);

                    ps.RemoveAt(i);
                }
            }
        }
        #endregion

        #region IServiceProvider 成员
        object IServiceProvider.GetService(Type serviceType)
        {
            if (serviceType == typeof(PluginManager)) return this;

            if (Provider != null) Provider.GetService(serviceType);

            return null;
        }
        #endregion
    }
}