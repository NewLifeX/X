namespace NewLife.Queue.Storage
{
    public enum FlushOption
    {
        /// <summary>将数据刷到操作系统的缓存，性能较高
        /// </summary>
        FlushToOS,
        /// <summary>将数据写到磁盘，性能最低
        /// </summary>
        FlushToDisk
    }
}
