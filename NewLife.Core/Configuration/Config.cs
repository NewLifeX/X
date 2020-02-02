using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using NewLife.Log;
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
                if (_loading) return config;
                _loading = true;

                try
                {
                    var prv = Provider;
                    if (prv == null)
                    {
                        lock (typeof(Config<TConfig>))
                        {
                            if (prv == null)
                            {
                                // 创建提供者
                                var att = typeof(TConfig).GetCustomAttribute<ConfigAttribute>(true);
                                prv = ConfigProvider.Create(att?.Provider);

                                if (prv is ConfigProvider prv2)
                                {
                                    var value = att?.Name;
                                    if (value.IsNullOrEmpty())
                                    {
                                        value = typeof(TConfig).Name;
                                        if (value.EndsWith("Config") && value != "Config") value = value.TrimEnd("Config");
                                        if (value.EndsWith("Setting") && value != "Setting") value = value.TrimEnd("Setting");
                                    }

                                    prv2.Init(value);
                                }

                                Provider = prv;
                            }
                        }
                    }

                    if (_Current != null) return _Current;

                    // 加载配置数据到提供者
                    if (!prv.LoadAll())
                    {
                        XTrace.WriteLine("初始化{0}的配置 {1}", typeof(TConfig).Name, prv);
                        prv.Save(config);
                    }

                    // 绑定提供者数据到配置对象
                    prv.Bind(config, true);

                    config.OnLoaded();

                    return _Current = config;
                }
                finally { _loading = false; }
            }
            set { _Current = value; }
        }
        #endregion

        #region 属性
        /// <summary>是否新的配置文件</summary>
        [XmlIgnore]
        //[Obsolete("=>_Provider.IsNew")]
        public Boolean IsNew => Provider is FileConfigProvider fprv && fprv.IsNew;
        #endregion

        #region 成员方法
        /// <summary>从配置文件中读取完成后触发</summary>
        protected virtual void OnLoaded()
        {
            // 仅适用于文件提供者
            var fprv = Provider as FileConfigProvider;
            if (fprv == null) return;

            // 如果默认加载后的配置与保存的配置不一致，说明可能配置实体类已变更，需要强制覆盖
            var config = this;
            try
            {
                var cfi = fprv.FileName.GetBasePath();

                // 新建配置不要检查格式
                var flag = File.Exists(cfi);
                if (!flag) return;

                var xml1 = File.ReadAllText(cfi).Trim();
                var xml2 = fprv.GetString();
                flag = xml1 == xml2;

                if (!flag)
                {
                    // 异步处理，避免加载日志路径配置时死循环
                    XTrace.WriteLine("配置文件{0}格式不一致，保存为最新格式！", cfi);
                    Provider.Save(config);
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        /// <summary>保存到配置文件中去</summary>
        //[Obsolete("=>Provider.Save")]
        public virtual void Save() => Provider.Save(this);

        /// <summary>异步保存</summary>
        [Obsolete("=>Provider.Save")]
        public virtual void SaveAsync() => ThreadPoolX.QueueUserWorkItem(() => Provider.Save(this));
        #endregion
    }
}