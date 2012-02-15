using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Web;
using System.Web.Configuration;
using NewLife.Log;

namespace XUrlRewrite.Configuration
{
    /// <summary>
    /// 模板配置管理
    /// </summary>
    public class Manager
    {
        private static Dictionary<String, Manager> _Managers = new Dictionary<String, Manager>();
        static readonly String DEFAULT_CONFIG_PATH = "~/UrlRewrite.config";
        static readonly String DEFAULT_CONFIG_TAG = "urlRewriteConfig";

        /// <summary>
        /// 获得模板配置信息
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static UrlRewriteConfig GetConfig(HttpApplication app)
        {
            if (app == null) return null;
            Manager ret = GetConfigManager(app);
            return ret != null ? ret.GetTemplateConfig() : null;
        }

        /// <summary>
        /// 获得模板配置管理器,可以修改配置文件路径,重新加载配置文件,获取配置文件最后修改时间
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static Manager GetConfigManager(HttpApplication app)
        {
            String appPath = app.Server.MapPath("~/");
            Manager ret = null;
            if (_Managers.ContainsKey(appPath))
            {
                ret = _Managers[appPath];
            }
            else
            {
                lock (_Managers)
                {
                    if (_Managers.ContainsKey(appPath))
                    {
                        ret = _Managers[appPath];
                    }
                    else
                    {
                        ret = new Manager(app);
                        _Managers[appPath] = ret;
                    }
                }
            }
            return ret;
        }

        private HttpApplication app;
        private UrlRewriteConfig cfg;

        internal Manager(HttpApplication app)
        {
            this.app = app;
        }

        private ExeConfigurationFileMap ConfigFileMap = new ExeConfigurationFileMap();
        private System.Configuration.Configuration _Configuration;
        /// <summary>
        /// 当模版配置信息重新加载后触发的事件
        /// </summary>
        public event EventHandler LoadConfig;

        /// <summary>
        /// 获得模板配置信息
        /// </summary>
        /// <returns></returns>
        public UrlRewriteConfig GetTemplateConfig()
        {
            if (cfg == null || NeedForReload)
            {
                ConfigFileMap.ExeConfigFilename = AbsoluteConfigFilePath;
                _Configuration = ConfigurationManager.OpenMappedExeConfiguration(ConfigFileMap, ConfigurationUserLevel.None);
                if (!_Configuration.HasFile)
                {
                    throw new Exception(String.Format("Url重写配置文件{0}不存在", ConfigFileMap.ExeConfigFilename));
                }
                try
                {
                    cfg = (UrlRewriteConfig)_Configuration.GetSection(ConfigSectionName);
                    LastConfigFileWriteTime = File.GetLastWriteTime(ConfigFileMap.ExeConfigFilename);
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        String.Format(@"模板映射配置文件格式不正确,或者configSections段内没有{0}的定义", ConfigSectionName), ex);
                }
                if (LoadConfig != null) LoadConfig(this, EventArgs.Empty);
            }
            return cfg;
        }

        private String _ConfigFilePath;

        /// <summary>
        /// 配置文件路径
        /// </summary>
        internal String ConfigFilePath
        {
            get
            {
                if (_ConfigFilePath == null)
                {
                    try
                    {
                        _ConfigFilePath = ConfigurationManager.AppSettings["XUrlRewrite.ConfigFile"];
                        if (_ConfigFilePath == null)
                        {
                            _ConfigFilePath = WebConfigurationManager.AppSettings["XUrlRewrite.ConfigFile"];
                        }
                        if (_ConfigFilePath == null)
                        {
                            _ConfigFilePath = DEFAULT_CONFIG_PATH;
                        }
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteLine("读取XUrlRewrite配置文件失败!\r\n{0}", ex.Message);
                    }
                }
                return _ConfigFilePath;
            }
        }

        private String _AbsoluteConfigFilePath = null;

        internal String AbsoluteConfigFilePath
        {
            get
            {
                if (_AbsoluteConfigFilePath == null)
                {
                    _AbsoluteConfigFilePath = ConfigFilePath.StartsWith("~/") ? app.Server.MapPath(ConfigFilePath) : ConfigFilePath;
                }
                return _AbsoluteConfigFilePath;
            }
        }

        /// <summary>
        /// 配置标签名称
        /// </summary>
        internal String ConfigSectionName
        {
            get
            {
                return DEFAULT_CONFIG_TAG;
            }
        }

        private DateTime LastConfigFileWriteTime = DateTime.MinValue;

        /// <summary>
        /// 获取配置文件最后的修改时间
        /// </summary>
        public DateTime LastWriteTime
        {
            get
            {
                return LastConfigFileWriteTime;
            }
        }

        private DateTime LastNeedForReloadCheckTime = DateTime.MinValue;

        private TimeSpan LastNeedForReloadInterval = TimeSpan.FromSeconds(10); //需要重新读取配置文件的间隔时间
        /// <summary>
        /// 获取或设置是否需要重新读取配置文件
        /// </summary>
        public Boolean NeedForReload
        {
            get
            {
                DateTime n = DateTime.Now;
                Boolean ret = false;
                if (n - LastNeedForReloadInterval > LastNeedForReloadCheckTime)
                {
                    LastNeedForReloadCheckTime = n;
                    if (ConfigFilePath != null && File.GetLastWriteTime(AbsoluteConfigFilePath) > LastConfigFileWriteTime)
                    {
                        ret = true;
                    }
                }
                return ret;
            }
            set
            {
                LastConfigFileWriteTime = LastNeedForReloadCheckTime = value ? DateTime.MinValue : DateTime.Now;
            }
        }

        /// <summary>
        /// 保存对模板配置节点的修改
        /// </summary>
        public void Save()
        {
            if (_Configuration != null)
            {
                _Configuration.Save(ConfigurationSaveMode.Modified, true);
            }
        }

        /// <summary>
        /// 令存对模板配置节点的修改
        /// </summary>
        /// <param name="filename"></param>
        public void SaveAs(String filename)
        {
            if (_Configuration != null)
            {
                _Configuration.SaveAs(filename);
            }
        }

        private static bool? _Debug;

        /// <summary>
        /// 是否调试状态的开关
        /// </summary>
        public static bool Debug
        {
            get
            {
                if (_Debug != null) return _Debug.Value;
                String str = ConfigurationManager.AppSettings["XUrlRewrite.Debug"];
                if (String.IsNullOrEmpty(str)) str = ConfigurationManager.AppSettings["Debug"];
                if (String.IsNullOrEmpty(str)) return false;
                if (str == "1") return true;
                if (str == "0") return false;
                if (str.Equals(Boolean.FalseString, StringComparison.OrdinalIgnoreCase)) return false;
                if (str.Equals(Boolean.TrueString, StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }
            set
            {
                _Debug = value;
            }
        }
    }
}