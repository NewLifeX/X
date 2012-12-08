
namespace XCode.Accessors
{
    /// <summary>实体访问器设置选项</summary>
    public enum EntityAccessorOptions
    {
        /// <summary>是否所有字段</summary>
        AllFields,

        /// <summary>最大文件大小，默认10M</summary>
        MaxLength,

        /// <summary>控件容器</summary>
        Container,

        /// <summary>
        /// 是否在子窗体中查询
        /// 这里泛指Form嵌套Form
        /// </summary>
        IsFindChildForm,

        /// <summary>前缀</summary>
        ItemPrefix,

        /// <summary>数据流</summary>
        Stream,

        /// <summary>编码</summary>
        Encoding
    }
}