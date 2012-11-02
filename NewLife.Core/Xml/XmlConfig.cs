using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using NewLife.Log;
using NewLife.Threading;

namespace NewLife.Xml
{
    /// <summary>Xml配置文件基类</summary>
    /// <typeparam name="TConfig"></typeparam>
    public class XmlConfig<TConfig> where TConfig : XmlConfig<TConfig>, new()
    {
        private static TConfig _Current;
        /// <summary>当前实例。通过置空可以使其重新加载</summary>
        public static TConfig Current
        {
            get
            {
                // 这里要小心，避免_Current的null判断完成后，_Current被别人置空，而导致这里返回null
                var config = _Current;
                if (config != null)
                {
                    // 现存有对象，尝试再次加载，可能因为未修改而返回null，这样只需要返回现存对象即可
                    if (!IsUpdated) return config;
                    var cfg = Load();
                    if (cfg == null) return config;
                    _Current = cfg;
                    return cfg;
                }
                else
                {
                    // 现在没有对象，尝试加载，若返回null则实例化一个新的
                    config = Load();
                    if (config == null) config = new TConfig();
                    _Current = config;
                    return config;
                }
            }
            set { _Current = value; }
        }

        /// <summary>一些设置。派生类可以在自己的静态构造函数中指定</summary>
        public static class _
        {
            private static String _ConfigFile;
            /// <summary>配置文件路径</summary>
            public static String ConfigFile { get { return _ConfigFile; } set { _ConfigFile = value.GetFullPath(); } }

            private static Int32 _ReloadTime;
            /// <summary>重新加载时间。单位：毫秒</summary>
            public static Int32 ReloadTime { get { return _ReloadTime; } set { _ReloadTime = value; } }
        }

        static XmlConfig()
        {
            // 获取XmlConfigFileAttribute特性，那里会指定配置文件名称
            var att = typeof(TConfig).GetCustomAttribute<XmlConfigFileAttribute>(true);
            if (att == null || att.FileName.IsNullOrWhiteSpace())
            {
                // 这里不能着急，派生类可能通过静态构造函数指定配置文件路径
                //throw new XException("编码错误！请为配置类{0}设置{1}特性，指定配置文件！", typeof(TConfig), typeof(XmlConfigFileAttribute).Name);
            }
            else
            {
                _.ConfigFile = att.FileName;
                _.ReloadTime = att.ReloadTime;
            }

            // 实例化一次，用于触发派生类中可能的静态构造函数
            var config = new TConfig();
        }

        #region 检查是否已更新
        /// <summary>最后写入时间</summary>
        private static DateTime lastWrite;
        /// <summary>过期时间。如果在这个时间之后再次访问，将检查文件修改时间</summary>
        private static DateTime expire;

        static Boolean IsUpdated
        {
            get
            {
                var now = DateTime.Now;
                if (_.ReloadTime > 0 && expire < now)
                {
                    var fi = new FileInfo(_.ConfigFile);
                    fi.Refresh();
                    expire = now.AddMilliseconds(_.ReloadTime);

                    if (lastWrite < fi.LastWriteTime)
                    {
                        lastWrite = fi.LastWriteTime;
                        return true;
                    }
                }
                return false;
            }
        }
        #endregion

        static TConfig Load()
        {
            var filename = _.ConfigFile;
            if (filename.IsNullOrWhiteSpace() && !File.Exists(filename)) return null;

            try
            {
                var config = filename.ToXmlFileEntity<TConfig>();
                if (config == null) return null;

                config.OnLoaded();

                //// 第一次加载，建立定时重载定时器
                //if (timer == null && _.ReloadTime > 0) timer = new TimerX(s => Current = null, null, _.ReloadTime * 1000, _.ReloadTime * 1000);

                return config;
            }
            catch (Exception ex) { XTrace.WriteException(ex); return null; }
        }

        /// <summary>从配置文件中读取完成后触发</summary>
        protected virtual void OnLoaded() { }

        /// <summary>保存到配置文件中去</summary>
        public virtual void Save()
        {
            this.ToXmlFile(_.ConfigFile, Encoding.UTF8, null, null, true);
        }
    }
}