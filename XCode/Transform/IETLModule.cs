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
        /// <returns></returns>
        Boolean Processing();

        /// <summary>单批数据处理后</summary>
        void Processed();

        /// <summary>实体列表完成后</summary>
        /// <param name="list"></param>
        /// <param name="set"></param>
        /// <param name="success"></param>
        /// <param name="fetchCost"></param>
        /// <param name="processCost"></param>
        void OnFinished(IList<IEntity> list, IExtractSetting set, Int32 success, Double fetchCost, Double processCost);

        /// <summary>出错</summary>
        /// <param name="source"></param>
        /// <param name="set"></param>
        /// <param name="ex"></param>
        void OnError(Object source, IExtractSetting set, Exception ex);
    }

    static class ETLModuleHelper
    {
        public static void Start(this IEnumerable<IETLModule> list)
        {
            foreach (var item in list)
            {
                item.Start();
            }
        }

        public static void Stop(this IEnumerable<IETLModule> list)
        {
            foreach (var item in list)
            {
                item.Stop();
            }
        }

        public static void Init(this IEnumerable<IETLModule> list)
        {
            foreach (var item in list)
            {
                item.Init();
            }
        }

        public static Boolean Processing(this IEnumerable<IETLModule> list)
        {
            foreach (var item in list)
            {
                if (!item.Processing()) return false;
            }

            return true;
        }

        public static void Processed(this IEnumerable<IETLModule> list)
        {
            foreach (var item in list)
            {
                item.Processed();
            }
        }

        public static void OnFinished(this IEnumerable<IETLModule> es, IList<IEntity> list, IExtractSetting set, Int32 success, Double fetchCost, Double processCost)
        {
            foreach (var item in es)
            {
                item.OnFinished(list, set, success, fetchCost, processCost);
            }
        }

        public static void OnError(this IEnumerable<IETLModule> es, Object source, IExtractSetting set, Exception ex)
        {
            foreach (var item in es)
            {
                item.OnError(source, set, ex);
            }
        }
    }
}