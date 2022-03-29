namespace NewLife.Configuration
{
    /// <summary>配置映射接口。用于自定义映射配置树到当前对象</summary>
    /// <remarks>
    /// 整体配置数据改变时触发调用该接口，但不表示当前对象所绑定路径的配置数据有改变，用户需要自己判断所属配置数据是否已改变。
    /// </remarks>
    public interface IConfigMapping
    {
        /// <summary>映射配置树到当前对象</summary>
        /// <param name="provider">配置提供者</param>
        /// <param name="section">配置数据段</param>
        void MapConfig(IConfigProvider provider, IConfigSection section);
    }
}