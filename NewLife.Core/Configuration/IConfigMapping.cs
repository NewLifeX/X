namespace NewLife.Configuration
{
    /// <summary>配置映射接口。用于自定义映射配置树到当前对象</summary>
    public interface IConfigMapping
    {
        /// <summary>映射配置树到当前对象</summary>
        /// <param name="provider"></param>
        /// <param name="section"></param>
        void MapConfig(IConfigProvider provider, IConfigSection section);
    }
}