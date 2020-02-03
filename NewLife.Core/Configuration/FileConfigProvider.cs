using System;
using System.IO;

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

            return true;
        }

        /// <summary>写入配置文件</summary>
        /// <param name="fileName">文件名</param>
        /// <param name="section">配置段</param>
        protected abstract void OnWrite(String fileName, IConfigSection section);

        /// <summary>获取字符串形式</summary>
        /// <param name="section">配置段</param>
        /// <returns></returns>
        public virtual String GetString(IConfigSection section = null) => null;
        #endregion
    }
}