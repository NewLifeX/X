using System;
using System.IO;
using NewLife.Exceptions;
using NewLife.Log;

namespace NewLife.Xml
{
    /// <summary>Xml配置文件基类</summary>
    /// <remarks>
    /// 标准用法：TConfig.Current
    /// 
    /// 配置实体类通过<see cref="XmlConfigFileAttribute"/>特性指定配置文件路径以及自动更新时间。
    /// Current将加载配置文件，如果文件不存在或者加载失败，将实例化一个对象返回。
    /// 
    /// 考虑到自动刷新，不提供LoadFile和SaveFile等方法，可通过扩展方法ToXmlFileEntity和ToXmlFile实现。
    /// 
    /// 用户也可以通过配置实体类的静态构造函数修改基类的<see cref="_.ConfigFile"/>和<see cref="_.ReloadTime"/>来动态配置加载信息。
    /// </remarks>
    /// <typeparam name="TConfig"></typeparam>
    public class XmlConfig<TConfig> where TConfig : XmlConfig<TConfig>, new()
    {
        private static TConfig _Current;
        /// <summary>当前实例。通过置空可以使其重新加载。</summary>
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
                    if (config == null)
                    {
                        config = new TConfig();

                        // 创建或覆盖
                        if (XTrace.Debug) XTrace.WriteLine("{0}的配置文件{1}不存在或加载出错，准备用默认配置覆盖！", typeof(TConfig).Name, _.ConfigFile);
                        try
                        {
                            config.Save();
                        }
                        catch (Exception ex)
                        {
                            XTrace.WriteException(ex);
                        }
                    }
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
            public static String ConfigFile { get { return _ConfigFile; } set { _ConfigFile = value; } }

            private static Int32 _ReloadTime;
            /// <summary>重新加载时间。单位：毫秒</summary>
            public static Int32 ReloadTime { get { return _ReloadTime; } set { _ReloadTime = value; } }

            static _()
            {             // 获取XmlConfigFileAttribute特性，那里会指定配置文件名称
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
        }

        static XmlConfig()
        {
            // 实例化一次，用于触发派生类中可能的静态构造函数
            //var config = new TConfig();
            var config = Current;

            var filename = _.ConfigFile.GetFullPath();
            if (!filename.IsNullOrWhiteSpace() && File.Exists(filename))
            {
                // 如果默认加载后的配置与保存的配置不一致，说明可能配置实体类已变更，需要强制覆盖
                try
                {
                    var xml1 = File.ReadAllText(filename);
                    var xml2 = config.ToXml(null, "", "", true, true);
                    if (xml1 != xml2) config.Save();
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
            }
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
                    var fi = new FileInfo(_.ConfigFile.GetFullPath());
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
            var filename = _.ConfigFile.GetFullPath();
            if (filename.IsNullOrWhiteSpace()) return null;
            if (!File.Exists(filename)) return null;

            try
            {
                //var config = filename.ToXmlFileEntity<TConfig>();

                /*
                 * 初步现象：在不带sp的.Net 2.0中，两种扩展方法加泛型的写法都会导致一个诡异异常
                 * System.BadImageFormatException: 试图加载格式不正确的程序
                 * 
                 * 经过多次尝试，不用扩展方法也不行，但是不用泛型可以！
                 */

                TConfig config = null;
                using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    //config = stream.ToXmlEntity<TConfig>();
                    config = stream.ToXmlEntity(typeof(TConfig)) as TConfig;
                }
                if (config == null) return null;

                config.OnLoaded();

                return config;
            }
            catch (Exception ex) { XTrace.WriteException(ex); return null; }
        }

        /// <summary>从配置文件中读取完成后触发</summary>
        protected virtual void OnLoaded() { }

        /// <summary>保存到配置文件中去</summary>
        public virtual void Save()
        {
            var filename = _.ConfigFile;
            if (filename.IsNullOrWhiteSpace()) throw new XException("未指定{0}的配置文件路径！", typeof(TConfig).Name);
            filename = filename.GetFullPath();

            this.ToXmlFile(filename, null, "", "", true, true);
        }
    }
}