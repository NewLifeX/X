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
        void Init(ETL etl);

        Boolean Processing();
        void Processed();

        void OnFinished(IList<IEntity> list, IExtractSetting set, Int32 success, Double fetchCost, Double processCost);

        void OnError(Object source, IExtractSetting set, Exception ex);
    }

    static class ETLModuleHelper
    {
        public static void Init(this IEnumerable<IETLModule> list, ETL etl)
        {
            foreach (var item in list)
            {
                item.Init(etl);
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