using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Threading;

namespace NewLife.Xml
{
    /// <summary>Xml配置文件基类</summary>
    /// <remarks>
    /// 标准用法：TConfig.Current
    /// 
    /// 配置实体类通过<see cref="XmlConfigFileAttribute"/>特性指定配置文件路径以及自动更新时间。
    /// Current将加载配置文件，如果文件不存在或者加载失败，将实例化一个对象返回。
    /// </remarks>
    /// <typeparam name="TConfig"></typeparam>
    [Obsolete("=>Config<TConfig>")]
    public class XmlConfig<TConfig> where TConfig : XmlConfig<TConfig>, new()
    {
        #region 静态
        private static XmlConfigProvider _Provider = new XmlConfigProvider();
        /// <summary>当前使用的提供者</summary>
        public static IConfigProvider Provider => _Provider;

        private static TConfig _Current;
        /// <summary>当前实例。通过置空可以使其重新加载。</summary>
        public static TConfig Current
        {
            get
            {
                if (_Current != null) return _Current;

                var prv = _Provider;
                lock (prv)
                {
                    if (_Current != null) return _Current;

                    if (prv.FileName.IsNullOrEmpty())
                    {
                        var att = typeof(TConfig).GetCustomAttribute<XmlConfigFileAttribute>(true);
                        if (att != null) prv.Init(att.FileName);
                    }

                    var config = new TConfig();

                    if (!prv.LoadAll())
                    {
                        XTrace.WriteLine("初始化{0}的配置文件{1}！", typeof(TConfig).Name, prv.FileName);
                        prv.Save(config);
                    }

                    prv.Bind(config, true);

                    if (prv.IsNew) config.OnNew();
                    config.OnLoaded();

                    return _Current = config;
                }
            }
            set { _Current = value; }
        }
        #endregion

        #region 属性
        /// <summary>是否新的配置文件</summary>
        [XmlIgnore]
        [Obsolete("=>_Provider.IsNew")]
        public Boolean IsNew => _Provider.IsNew;
        #endregion

        #region 成员方法
        /// <summary>从配置文件中读取完成后触发</summary>
        protected virtual void OnLoaded()
        {
            // 如果默认加载后的配置与保存的配置不一致，说明可能配置实体类已变更，需要强制覆盖
            var config = this;
            try
            {
                var cfi = _Provider.FileName.GetBasePath();
                // 新建配置不要检查格式
                var flag = File.Exists(cfi);
                if (!flag) return;

                var xml1 = File.ReadAllText(cfi).Trim();
                var xml2 = _Provider.GetString();
                flag = xml1 == xml2;

                if (!flag)
                {
                    // 异步处理，避免加载日志路径配置时死循环
                    XTrace.WriteLine("配置文件{0}格式不一致，保存为最新格式！", cfi);
                    config.Save();
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        /// <summary>保存到配置文件中去</summary>
        public virtual void Save() => _Provider.Save(this);

        /// <summary>异步保存</summary>
        public virtual void SaveAsync() => ThreadPoolX.QueueUserWorkItem(() => _Provider.Save(this));

        /// <summary>新创建配置文件时执行</summary>
        protected virtual void OnNew() { }
        #endregion
    }
}