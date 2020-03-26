using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XCode.Transform
{
    /// <summary>数据抽取模块，用于自定义抽取过程中各个环节</summary>
    public interface IETLModule
    {
        /// <summary>开始调度</summary>
        void Start();

        /// <summary>停止调度</summary>
        void Stop();

        /// <summary>首次初始化任务</summary>
        void Init();

        /// <summary>单批数据处理前</summary>
        /// <param name="ctx">数据上下文</param>
        /// <returns></returns>
        Boolean Processing(DataContext ctx);

        /// <summary>单批数据处理后</summary>
        /// <param name="ctx">数据上下文</param>
        void Processed(DataContext ctx);

        /// <summary>抽取完成</summary>
        /// <param name="ctx">数据上下文</param>
        void Fetched(DataContext ctx);

        /// <summary>实体列表完成后</summary>
        /// <param name="ctx">数据上下文</param>
        void OnFinished(DataContext ctx);

        /// <summary>出错</summary>
        /// <param name="ctx">数据上下文</param>
        void OnError(DataContext ctx);
    }
}