using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Serialization;

using NewLife.Threading;

namespace NewLife.Configuration
{
    /// <summary>配置文件基类</summary>
    /// <remarks>
    /// 标准用法：TConfig.Current
    /// 
    /// 配置实体类通过<see cref="ConfigAttribute"/>特性指定配置文件路径。
    /// Current将加载配置文件，如果文件不存在或者加载失败，将实例化一个对象返回。
    /// </remarks>
    /// <typeparam name="TConfig"></typeparam>
    public class Config<TConfig> where TConfig : Config<TConfig>, new()
    {
        #region 静态
        /// <summary>当前使用的提供者</summary>
        public static IConfigProvider Provider { get; set; }

        private static Boolean _loading;
        private static TConfig _Current;
        /// <summary>当前实例。通过置空可以使其重新加载。</summary>
        public static TConfig Current
        {
            get
            {
                if (_Current != null) return _Current;

                // 如果正在加载中，需要返回默认值。因为在加载配置的过程中，可能循环触发导致再次加载配置
                var config = new TConfig();
                if (_Current == null) _Current = config;
                if (_loading) return _Current;
                _loading = true;

                try
                {
                    var flag = false;
                    var prv = Provider;
                    if (prv == null)
                    {
                        lock (typeof(Config<TConfig>))
                        {
                            prv = Provider;
                            if (prv == null)
                            {
                                // 创建提供者
                                var att = typeof(TConfig).GetCustomAttribute<ConfigAttribute>(true);
                                var value = att?.Name;
                                if (value.IsNullOrEmpty())
                                {
                                    value = typeof(TConfig).Name;
                                    if (value.EndsWith("Config") && value != "Config") value = value.TrimEnd("Config");
                                    if (value.EndsWith("Setting") && value != "Setting") value = value.TrimEnd("Setting");
                                }
                                prv = ConfigProvider.Create(att?.Provider);
                                if (prv is HttpConfigProvider _prv && att is HttpConfigAttribute _att)
                                {
                                    _prv.Server = _att.Server;
                                    _prv.Action = _att.Action;
                                    _prv.AppId = _att.AppId;
                                    _prv.Secret = _att.Secret;
                                    _prv.Scope = _att.Scope;
                                    _prv.CacheLevel = _att.CacheLevel;
                                    _prv.Init(value);
                                }
                                else if (prv is ConfigProvider prv2)
                                {


                                    prv2.Init(value);
                                }

                                Provider = prv;
                                flag = true;
                            }
                        }
                    }

                    if (!flag) return _Current;

                    //// 加载配置数据到提供者
                    //if (!prv.LoadAll())
                    //{
                    //    XTrace.WriteLine("初始化{0}的配置 {1}", typeof(TConfig).FullName, prv);
                    //    //prv.Save(config);
                    //}

                    // 绑定提供者数据到配置对象
                    prv.Bind(config, true);

                    config.OnLoaded();

                    // OnLoad 中可能有变化，存回去
                    prv.Save(config);

                    return _Current = config;
                }
                finally { _loading = false; }
            }
            set { _Current = value; }
        }
        #endregion

        #region 属性
        /// <summary>是否新的配置文件</summary>
        [XmlIgnore, IgnoreDataMember]
        //[Obsolete("=>_Provider.IsNew")]
        public Boolean IsNew => Provider.IsNew;
        #endregion

        #region 成员方法
        /// <summary>从配置文件中读取完成后触发</summary>
        protected virtual void OnLoaded() { }

        /// <summary>保存到配置文件中去</summary>
        //[Obsolete("=>Provider.Save")]
        public virtual void Save() => Provider.Save(this);

        /// <summary>异步保存</summary>
        [Obsolete("=>Provider.Save")]
        public virtual void SaveAsync() => ThreadPoolX.QueueUserWorkItem(() => Provider.Save(this));
        #endregion
    }
}