using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Model;
using NewLife.Queue.Broker;
using NewLife.Queue.Scheduling;
using NewLife.Queue.Storage.FileNamingStrategies;

namespace NewLife.Queue
{
    class QueueService
    {
        #region 当前静态服务容器
        /// <summary>当前对象容器</summary>
        public static IObjectContainer Container => ObjectContainer.Current;

        #endregion

        static QueueService()
        {
            var container = Container;
            container.Register<ILog, ConsoleLog>()
                .AutoRegister<IScheduleService, ScheduleService>()
                .AutoRegister<IFileNamingStrategy, DefaultFileNamingStrategy>()
                .AutoRegister<IMessageStore, DefaultFileNamingStrategy>();

            
        }


        #region 使用
        /// <summary>日志</summary>
        /// <returns></returns>
        public static ILog Log => Container.Resolve<ILog>();


        /// <summary>计划服务</summary>
        /// <returns></returns>
        public static IScheduleService ScheduleService => Container.ResolveInstance<IScheduleService>();

        #endregion
    }
}
