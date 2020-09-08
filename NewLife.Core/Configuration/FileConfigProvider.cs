using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Log;

namespace NewLife.Configuration
{
    /// <summary>文件配置提供者</summary>
    /// <remarks>
    /// 每个提供者实例对应一个配置文件，支持热更新
    /// </remarks>
    public abstract class FileConfigProvider : ConfigProvider
    {
        #region 属性
        /// <summary>文件名。最高优先级，优先于模型特性指定的文件名</summary>
        public String FileName { get; set; }

        /// <summary>是否新的配置文件</summary>
        public Boolean IsNew { get; set; }
        #endregion

        #region 构造
        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            _watcher.TryDispose();
        }
        #endregion

        #region 方法
        /// <summary>初始化</summary>
        /// <param name="value"></param>
        public override void Init(String value)
        {
            base.Init(value);

            // 加上文件名
            if (FileName.IsNullOrEmpty() && !value.IsNullOrEmpty())
            {
                // 加上配置目录
                var str = value;
                if (!str.StartsWithIgnoreCase("Config/", "Config\\")) str = "Config".CombinePath(str);

                FileName = str;
            }
        }

        /// <summary>加载配置</summary>
        public override Boolean LoadAll()
        {
            // 准备文件名
            var fileName = FileName;
            if (fileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(FileName));

            fileName = fileName.GetBasePath();

            IsNew = true;

            //if (!File.Exists(fileName)) throw new FileNotFoundException("找不到文件", fileName);
            if (!File.Exists(fileName)) return false;

            // 读取文件，换个对象，避免数组元素在多次加载后重叠
            var section = new ConfigSection { };
            OnRead(fileName, section);
            Root = section;

            IsNew = false;

            return true;
        }

        /// <summary>读取配置文件</summary>
        /// <param name="fileName">文件名</param>
        /// <param name="section">配置段</param>
        protected abstract void OnRead(String fileName, IConfigSection section);

        /// <summary>保存配置树到数据源</summary>
        public override Boolean SaveAll()
        {
            // 准备文件名
            var fileName = FileName;
            if (fileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(FileName));

            fileName = fileName.GetBasePath();
            fileName.EnsureDirectory(true);

            // 写入文件
            OnWrite(fileName, Root);

            // 首次建立配置文件时，由于文件不存在无法监听目录，改到这里延迟监听
            if (_models.Count > 0 && _watcher == null) InitWatcher();

            return true;
        }

        /// <summary>保存模型实例</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="nameSpace">命名空间。配置树位置</param>
        public override Boolean Save<T>(T model, String nameSpace = null)
        {
            // 文件存储，直接覆盖Root
            Root.Childs.Clear();
            MapFrom(Root, model);

            return SaveAll();
        }

        /// <summary>写入配置文件</summary>
        /// <param name="fileName">文件名</param>
        /// <param name="section">配置段</param>
        protected virtual void OnWrite(String fileName, IConfigSection section)
        {
            var str = GetString(section);
            var old = "";
            if (File.Exists(fileName)) old = File.ReadAllText(fileName);

            if (str != old)
            {
                XTrace.WriteLine("保存配置 {0}", fileName);

                File.WriteAllText(fileName, str);
            }
        }

        /// <summary>获取字符串形式</summary>
        /// <param name="section">配置段</param>
        /// <returns></returns>
        public virtual String GetString(IConfigSection section = null) => null;
        #endregion

        #region 绑定
        /// <summary>绑定模型，使能热更新，配置存储数据改变时同步修改模型属性</summary>
        /// <typeparam name="T">模型</typeparam>
        /// <param name="model">模型实例</param>
        /// <param name="autoReload">是否自动更新。默认true</param>
        /// <param name="nameSpace">命名空间。配置树位置，配置中心等多对象混合使用时</param>
        public override void Bind<T>(T model, Boolean autoReload = true, String nameSpace = null)
        {
            base.Bind<T>(model, autoReload, nameSpace);

            if (autoReload && !_models.ContainsKey(model))
            {
                _models.Add(model, nameSpace);

                InitWatcher();
            }
        }

        private void InitWatcher()
        {
            // 文件监视
            var fileName = FileName.GetBasePath();
            var dir = Path.GetDirectoryName(fileName);
            if (Directory.Exists(dir))
            {
                if (_watcher != null) _watcher.TryDispose();
                _watcher = new FileSystemWatcher(dir, Path.GetFileName(fileName))
                {
                    NotifyFilter = NotifyFilters.LastWrite
                };
                _watcher.Changed += Watch_Changed;
                _watcher.EnableRaisingEvents = true;
            }
        }

        private readonly IDictionary<Object, String> _models = new Dictionary<Object, String>();
        private FileSystemWatcher _watcher;
        private DateTime _nextRead;
        private Boolean _reading;
        private void Watch_Changed(Object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed || _reading) return;

            // 短时间内不要重复
            if (_nextRead > DateTime.Now) return;
            _nextRead = DateTime.Now.AddSeconds(1);

            var fileName = FileName.GetBasePath();
            if (File.Exists(fileName))
            {
                XTrace.WriteLine("配置文件改变，重新加载 {0}", fileName);

                _reading = true;
                try
                {
                    var section = new ConfigSection { };
                    OnRead(fileName, section);
                    Root = section;

                    foreach (var item in _models)
                    {
                        base.Bind(item.Key, false, item.Value);
                    }
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
                finally
                {
                    _reading = false;
                }
            }
        }
        #endregion
    }
}