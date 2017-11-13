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

    //static class ETLModuleHelper
    //{
    //    public static void Start(this IEnumerable<IETLModule> list)
    //    {
    //        foreach (var item in list)
    //        {
    //            item.Start();
    //        }
    //    }

    //    public static void Stop(this IEnumerable<IETLModule> list)
    //    {
    //        foreach (var item in list)
    //        {
    //            item.Stop();
    //        }
    //    }

    //    public static void Init(this IEnumerable<IETLModule> list)
    //    {
    //        foreach (var item in list)
    //        {
    //            item.Init();
    //        }
    //    }

    //    public static Boolean Processing(this IEnumerable<IETLModule> list, DataContext ctx)
    //    {
    //        foreach (var item in list)
    //        {
    //            if (!item.Processing(ctx)) return false;
    //        }

    //        return true;
    //    }

    //    public static void Processed(this IEnumerable<IETLModule> list, DataContext ctx)
    //    {
    //        foreach (var item in list)
    //        {
    //            item.Processed(ctx);
    //        }
    //    }

    //    public static void Fetched(this IEnumerable<IETLModule> es, DataContext ctx)
    //    {
    //        foreach (var item in es)
    //        {
    //            item.Fetched(ctx);
    //        }
    //    }

    //    public static void OnFinished(this IEnumerable<IETLModule> es, DataContext ctx)
    //    {
    //        foreach (var item in es)
    //        {
    //            item.OnFinished(ctx);
    //        }
    //    }

    //    public static void OnError(this IEnumerable<IETLModule> es, DataContext ctx)
    //    {
    //        foreach (var item in es)
    //        {
    //            item.OnError(ctx);
    //        }
    //    }
    //}
}