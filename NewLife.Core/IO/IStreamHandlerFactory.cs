using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using NewLife.Log;

namespace NewLife.IO
{
    /// <summary>
    /// 数据流处理器工厂接口
    /// </summary>
    public interface IStreamHandlerFactory
    {
        /// <summary>
        /// 获取处理器
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        IStreamHandler GetHandler(Stream stream);
    }

    /// <summary>
    /// 数据流处理器工厂
    /// </summary>
    public abstract class StreamHandlerFactory : IStreamHandlerFactory
    {
        #region 接口
        /// <summary>
        /// 获取处理器
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public abstract IStreamHandler GetHandler(Stream stream);
        #endregion

        #region 构造
        static StreamHandlerFactory()
        {
            LoadConfig();
        }
        #endregion

        #region 工厂
        static Dictionary<String, List<IStreamHandlerFactory>> maps = new Dictionary<String, List<IStreamHandlerFactory>>();
        /// <summary>
        /// 注册数据流处理器工厂。
        /// 数据流到达时将进入指定通道的每一个工厂，直到工厂可以返回数据流处理器为止。
        /// 不同通道名称的工厂互不干扰。
        /// </summary>
        /// <param name="name">通道名称，用于区分数据流总线</param>
        /// <param name="factory"></param>
        public static void RegisterFactory(String name, IStreamHandlerFactory factory)
        {
            lock (maps)
            {
                //if (!maps.Contains(factory)) maps.Add(factory);

                if (!maps.ContainsKey(name)) maps.Add(name, new List<IStreamHandlerFactory>());
                List<IStreamHandlerFactory> list = maps[name];

                // 相同实例或者相同工厂类只能有一个
                foreach (IStreamHandlerFactory item in list)
                {
                    if (item == factory || item.GetType() == factory.GetType()) return;
                }

                list.Add(factory);
            }
        }
        #endregion

        #region 配置
        const String factoryKey = "NewLife.StreamHandlerFactory_";

        /// <summary>
        /// 获取配置文件指定的工厂
        /// </summary>
        /// <returns></returns>
        static Dictionary<String, Type[]> GetFactory()
        {
            NameValueCollection nvcs = ConfigurationManager.AppSettings;
            if (nvcs == null || nvcs.Count < 1) return null;

            Dictionary<String, Type[]> dic = new Dictionary<String, Type[]>();
            // 遍历设置项
            foreach (String appName in nvcs)
            {
                // 必须以指定名称开始
                if (!appName.StartsWith(factoryKey, StringComparison.Ordinal)) continue;

                // 总线通道名称
                String name = appName.Substring(factoryKey.Length + 1);

                String str = ConfigurationManager.AppSettings[appName];
                if (String.IsNullOrEmpty(str)) continue;

                String[] ss = str.Split(new Char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                List<Type> list = new List<Type>();
                foreach (String item in ss)
                {
                    Type type = Type.GetType(item);
                    list.Add(type);
                }

                dic.Add(name, list.ToArray());
            }
            return dic.Count > 0 ? dic : null; ;
        }

        /// <summary>
        /// 从配置文件中加载工厂
        /// </summary>
        static void LoadConfig()
        {
            try
            {
                Dictionary<String, Type[]> ts = GetFactory();
                if (ts == null || ts.Count < 1) return;

                foreach (String item in ts.Keys)
                {
                    foreach (Type type in ts[item])
                    {
                        IStreamHandlerFactory factory = Activator.CreateInstance(type) as IStreamHandlerFactory;
                        RegisterFactory(item, factory);
                    }
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteLine("从配置文件加载数据流工厂出错！" + ex.ToString());
            }
        }
        #endregion

        #region 处理数据流
        /// <summary>
        /// 处理数据流。Http、Tcp、Udp等所有数据流都将到达这里，多种传输方式汇聚于此，由数据流总线统一处理！
        /// </summary>
        /// <param name="name"></param>
        /// <param name="stream"></param>
        public static void Process(String name, Stream stream)
        {
            if (maps == null || maps.Count < 1) return;

            IStreamHandler handler = null;
            IStreamHandlerFactory[] fs = maps[name].ToArray();
            // 倒序遍历工厂，后来者优先
            for (int i = fs.Length - 1; i >= 0; i--)
            {
                // 把数据流分给每一个工厂，看看谁有能力处理数据流，有能力者返回数据流处理器
                handler = fs[i].GetHandler(stream);
                if (handler != null) break;
            }
            if (handler == null) return;

            // 由该处理器处理数据流
            handler.Process(stream);
        }
        #endregion
    }
}