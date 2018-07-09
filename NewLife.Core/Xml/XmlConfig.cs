using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
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
    /// 
    /// 考虑到自动刷新，不提供LoadFile和SaveFile等方法，可通过扩展方法ToXmlFileEntity和ToXmlFile实现。
    /// 
    /// 用户也可以通过配置实体类的静态构造函数修改基类的<see cref="_.ConfigFile"/>和<see cref="_.ReloadTime"/>来动态配置加载信息。
    /// </remarks>
    /// <typeparam name="TConfig"></typeparam>
    public class XmlConfig<TConfig> : DisposeBase where TConfig : XmlConfig<TConfig>, new()
    {
        #region 静态
        private static Boolean _loading;
        private static TConfig _Current;
        /// <summary>当前实例。通过置空可以使其重新加载。</summary>
        public static TConfig Current
        {
            get
            {
                if (_loading) return _Current ?? new TConfig();

                var dcf = _.ConfigFile;
                if (dcf == null) return new TConfig();

                // 这里要小心，避免_Current的null判断完成后，_Current被别人置空，而导致这里返回null
                var config = _Current;
                if (config != null)
                {
                    // 现存有对象，尝试再次加载，可能因为未修改而返回null，这样只需要返回现存对象即可
                    if (!config.IsUpdated) return config;

                    XTrace.WriteLine("{0}的配置文件{1}有更新，重新加载配置！", typeof(TConfig), config.ConfigFile);

                    // 异步更新
                    ThreadPool.QueueUserWorkItem(s => config.Load(dcf));

                    return config;
                }

                // 现在没有对象，尝试加载，若返回null则实例化一个新的
                lock (dcf)
                {
                    if (_Current != null) return _Current;

                    config = new TConfig();
                    _Current = config;
                    if (!config.Load(dcf))
                    {
                        config.ConfigFile = dcf.GetFullPath();
                        config.SetExpire();  // 设定过期时间
                        config.IsNew = true;
                        config.OnNew();

                        config.OnLoaded();

                        // 创建或覆盖
                        var act = File.Exists(dcf.GetFullPath()) ? "加载出错" : "不存在";
                        XTrace.WriteLine("{0}的配置文件{1} {2}，准备用默认配置覆盖！", typeof(TConfig).Name, dcf, act);
                        try
                        {
                            // 根据配置，有可能不保存，直接返回默认
                            if (_.SaveNew) config.Save();
                        }
                        catch (Exception ex)
                        {
                            XTrace.WriteException(ex);
                        }
                    }
                }

                return config;
            }
            set { _Current = value; }
        }

        /// <summary>一些设置。派生类可以在自己的静态构造函数中指定</summary>
        public static class _
        {
            /// <summary>是否调试</summary>
            public static Boolean Debug { get; set; }

            /// <summary>配置文件路径</summary>
            public static String ConfigFile { get; set; }

            /// <summary>重新加载时间。单位：毫秒</summary>
            public static Int32 ReloadTime { get; set; }

            /// <summary>没有配置文件时是否保存新配置。默认true</summary>
            public static Boolean SaveNew { get; set; } = true;

            static _()
            {
                // 获取XmlConfigFileAttribute特性，那里会指定配置文件名称
                var att = typeof(TConfig).GetCustomAttribute<XmlConfigFileAttribute>(true);
                if (att == null || att.FileName.IsNullOrWhiteSpace())
                {
                    // 这里不能着急，派生类可能通过静态构造函数指定配置文件路径
                    //throw new XException("编码错误！请为配置类{0}设置{1}特性，指定配置文件！", typeof(TConfig), typeof(XmlConfigFileAttribute).Name);
#if !__MOBILE__
                    _.ConfigFile = "Config\\{0}.config".F(typeof(TConfig).Name);
                    _.ReloadTime = 10000;
#endif
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
        #endregion

        #region 属性
        /// <summary>配置文件</summary>
        [XmlIgnore]
        public String ConfigFile { get; set; }

        /// <summary>最后写入时间</summary>
        [XmlIgnore]
        private DateTime lastWrite;
        /// <summary>过期时间。如果在这个时间之后再次访问，将检查文件修改时间</summary>
        [XmlIgnore]
        private DateTime expire;

        /// <summary>是否已更新。通过文件写入时间判断</summary>
        [XmlIgnore]
        protected Boolean IsUpdated
        {
            get
            {
                var cf = ConfigFile;
                //if (cf.IsNullOrEmpty() || !File.Exists(cf)) return false;
                // 频繁调用File.Exists的性能损耗巨大
                if (cf.IsNullOrEmpty()) return false;

                var now = TimerX.Now;
                if (_.ReloadTime > 0 && expire < now)
                {
                    var fi = new FileInfo(cf);
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

        /// <summary>设置过期重新加载配置的时间</summary>
        void SetExpire()
        {
            if (_.ReloadTime > 0)
            {
                // 这里必须在加载后即可设置过期时间和最后写入时间，否则下一次访问的时候，IsUpdated会报告文件已更新
                var fi = new FileInfo(ConfigFile);
                if (fi.Exists)
                {
                    fi.Refresh();
                    lastWrite = fi.LastWriteTime;
                }
                else
                    lastWrite = TimerX.Now;
                expire = TimerX.Now.AddMilliseconds(_.ReloadTime);
            }
        }

        /// <summary>是否新的配置文件</summary>
        [XmlIgnore]
        public Boolean IsNew { get; set; }
        #endregion

        #region 构造
        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            _Timer.TryDispose();
        }
        #endregion

        #region 加载
        /// <summary>加载指定配置文件</summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public virtual Boolean Load(String filename)
        {
            if (filename.IsNullOrWhiteSpace()) return false;
            filename = filename.GetFullPath();
            if (!File.Exists(filename)) return false;

            _loading = true;
            try
            {
                var data = File.ReadAllBytes(filename);
                var config = this as TConfig;

                Object obj = config;
                var xml = new Serialization.Xml
                {
                    Stream = new MemoryStream(data),
                    UseAttribute = false,
                    UseComment = true
                };

                if (_.Debug) xml.Log = XTrace.Log;

                if (!xml.TryRead(GetType(), ref obj)) return false;

                config.ConfigFile = filename;
                config.SetExpire();  // 设定过期时间
                config.OnLoaded();

                return true;
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
                return false;
            }
            finally
            {
                _loading = false;
            }
        }
        #endregion

        #region 成员方法
        /// <summary>从配置文件中读取完成后触发</summary>
        protected virtual void OnLoaded()
        {
            // 如果默认加载后的配置与保存的配置不一致，说明可能配置实体类已变更，需要强制覆盖
            var config = this;
            try
            {
                var cfi = ConfigFile;
                // 新建配置不要检查格式
                var flag = File.Exists(cfi);
                if (!flag) return;

                var xml1 = File.ReadAllText(cfi).Trim();
                var xml2 = config.GetXml().Trim();
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
                if (_.Debug) XTrace.WriteException(ex);
            }
        }

        /// <summary>保存到配置文件中去</summary>
        /// <param name="filename"></param>
        public virtual void Save(String filename)
        {
            if (filename.IsNullOrWhiteSpace()) filename = ConfigFile;
            if (filename.IsNullOrWhiteSpace()) throw new XException("未指定{0}的配置文件路径！", typeof(TConfig).Name);
            filename = filename.GetFullPath();

            // 加锁避免多线程保存同一个文件冲突
            lock (filename)
            {
                var xml1 = File.Exists(filename) ? File.ReadAllText(filename).Trim() : null;
                var xml2 = GetXml();

                //if (File.Exists(filename)) File.Delete(filename);
                filename.EnsureDirectory(true);

                if (xml1 != xml2) File.WriteAllText(filename, xml2);
            }
        }

        /// <summary>保存到配置文件中去</summary>
        public virtual void Save() { Save(null); }

        private TimerX _Timer;
        /// <summary>异步保存</summary>
        public virtual void SaveAsync()
        {
            if (_Timer == null)
            {
                lock (this)
                {
                    if (_Timer == null) _Timer = new TimerX(DoSave, null, 1000, 5000)
                    {
                        Async = true,
                        CanExecute = () => _commits > 0,
                    };
                }
            }

            Interlocked.Increment(ref _commits);
        }

        private Int32 _commits;
        private void DoSave(Object state)
        {
            var old = _commits;
            //if (Interlocked.CompareExchange(ref _commits, 0, old) != old) return;
            if (old == 0) return;

            Save(null);

            Interlocked.Add(ref _commits, -old);
        }

        /// <summary>新创建配置文件时执行</summary>
        protected virtual void OnNew() { }

        private String GetXml()
        {
            var xml = new NewLife.Serialization.Xml
            {
                Encoding = Encoding.UTF8,
                UseAttribute = false,
                UseComment = true
            };

            if (_.Debug) xml.Log = XTrace.Log;

            xml.Write(this);

            return xml.GetString();
        }
        #endregion
    }
}