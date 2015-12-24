using NewLife.Xml;

namespace NewLife.Cube
{
    /// <summary>设置控制器</summary>
    public class ConfigController<TConfig> : ObjectController<TConfig> where TConfig : XmlConfig<TConfig>, new()
    {
        /// <summary>要展现和修改的对象</summary>
        protected override TConfig Value
        {
            get
            {
                return XmlConfig<TConfig>.Current;
            }
            set
            {
                if (value != null)
                {
                    var cfg = XmlConfig<TConfig>.Current;
                    value.ConfigFile = cfg.ConfigFile;
                    value.Save();
                }
                XmlConfig<TConfig>.Current = value;
            }
        }
    }
}