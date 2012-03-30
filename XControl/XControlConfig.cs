using NewLife.Configuration;
namespace XControl
{
    internal class XControlConfig
    {
        private static bool? _Debug;

        /// <summary>控件的Debug开关,配置项的名称是XControl.Debug</summary>
        public static bool Debug
        {
            get
            {
                if (_Debug == null)
                {
                    _Debug = Config.GetConfig<bool>("XControl.Debug", false);
                }
                return _Debug.Value;
            }
        }
    }
}