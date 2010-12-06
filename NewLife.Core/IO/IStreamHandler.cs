using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using NewLife.Log;

namespace NewLife.IO
{
    /// <summary>
    /// 数据流处理器接口
    /// </summary>
    public interface IStreamHandler : ICloneable
    {
        /// <summary>
        /// 处理数据流
        /// </summary>
        /// <param name="stream">待处理数据流</param>
        /// <returns>转发给下一个处理器的数据流，如果不想让后续处理器处理，返回空</returns>
        Stream Process(Stream stream);

        /// <summary>
        /// 是否可以重用。
        /// </summary>
        Boolean IsReusable { get; }

        ///// <summary>
        ///// 下一个数据处理器
        ///// </summary>
        //IStreamHandler Next { get; set; }
    }

    /// <summary>
    /// 数据流处理器
    /// </summary>
    public abstract class StreamHandler : IStreamHandler
    {
        #region 接口
        /// <summary>
        /// 处理数据流
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>转发给下一个处理器的数据流，如果不想让后续处理器处理，返回空</returns>
        public abstract Stream Process(Stream stream);

        /// <summary>
        /// 是否可以重用
        /// </summary>
        public virtual Boolean IsReusable { get { return false; } }

        Object ICloneable.Clone()
        {
            return MemberwiseClone();
        }
        #endregion

        #region 构造
        static StreamHandler()
        {
            LoadConfig();
        }
        #endregion

        #region 映射
        static Dictionary<String, LinkedList<IStreamHandler>> maps = new Dictionary<String, LinkedList<IStreamHandler>>();
        /// <summary>
        /// 注册数据流处理器。
        /// 数据流到达时将进入指定通道的每一个处理器。
        /// 不同通道名称的处理器互不干扰。
        /// 不提供注册到指定位置的功能，如果需要，再以多态方式实现。
        /// </summary>
        /// <param name="name">通道名称，用于区分数据流总线</param>
        /// <param name="handler">数据流处理器</param>
        /// <param name="cover">是否覆盖原有同类型处理器</param>
        public static void Register(String name, IStreamHandler handler, Boolean cover)
        {
            LinkedList<IStreamHandler> list = null;

            // 在字典中查找
            if (!maps.ContainsKey(name))
            {
                lock (maps)
                {
                    if (!maps.ContainsKey(name))
                    {
                        list = new LinkedList<IStreamHandler>();
                        maps.Add(name, list);
                    }
                    else
                    {
                        list = maps[name];
                    }
                }
            }
            else
            {
                list = maps[name];
            }

            // 修改处理器链表
            lock (list)
            {
                if (list.Contains(handler))
                {
                    if (cover)
                    {
                        // 一个处理器，只用一次，如果原来使用过，需要先移除。
                        // 一个处理器的多次注册，可用于改变处理顺序，使得自己排在更前面。
                        list.Remove(handler);
                        list.AddFirst(handler);
                    }
                }
                else
                {
                    list.AddFirst(handler);
                }
            }
        }

        /// <summary>
        /// 查询注册，返回指定通道的处理器数组。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IStreamHandler[] QueryRegister(String name)
        {
            if (maps == null || maps.Count < 1 || !maps.ContainsKey(name)) return null;
            lock (maps)
            {
                if (!maps.ContainsKey(name)) return null;

                LinkedList<IStreamHandler> list = maps[name];
                IStreamHandler[] fs = new IStreamHandler[list.Count];
                list.CopyTo(fs, 0);
                return fs;
            }
        }
        #endregion

        #region 配置
        const String handlerKey = "NewLife.StreamHandler_";

        /// <summary>
        /// 获取配置文件指定的处理器
        /// </summary>
        /// <returns></returns>
        static Dictionary<String, List<Type>> GetHandler()
        {
            NameValueCollection nvcs = ConfigurationManager.AppSettings;
            if (nvcs == null || nvcs.Count < 1) return null;

            Dictionary<String, List<Type>> dic = new Dictionary<String, List<Type>>();
            // 遍历设置项
            foreach (String appName in nvcs)
            {
                // 必须以指定名称开始
                if (!appName.StartsWith(handlerKey, StringComparison.Ordinal)) continue;

                // 总线通道名称
                String name = appName.Substring(handlerKey.Length + 1);

                String str = ConfigurationManager.AppSettings[appName];
                if (String.IsNullOrEmpty(str)) continue;

                String[] ss = str.Split(new Char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                List<Type> list = dic.ContainsKey(name) ? dic[name] : new List<Type>();
                foreach (String item in ss)
                {
                    Type type = Type.GetType(item);
                    list.Add(type);
                }
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
                Dictionary<String, List<Type>> ts = GetHandler();
                if (ts == null || ts.Count < 1) return;

                foreach (String item in ts.Keys)
                {
                    // 倒序。后注册的处理器先处理，为了迎合写在前面的处理器优先处理，故倒序！
                    for (int i = ts[item].Count - 1; i >= 0; i--)
                    {
                        IStreamHandler handler = Activator.CreateInstance(ts[item][i]) as IStreamHandler;
                        Register(item, handler, true);
                    }
                }
            }
            catch (Exception ex)
            {
                XTrace.WriteLine("从配置文件加载数据流处理器出错！" + ex.ToString());
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
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
            if (stream == null) throw new ArgumentNullException("stream");

            IStreamHandler[] fs = QueryRegister(name);
            if (fs == null || fs.Length < 1) throw new InvalidOperationException("没有找到" + name + "的处理器！");

            foreach (IStreamHandler item in fs)
            {
                IStreamHandler handler = item;
                if (!handler.IsReusable) handler = item.Clone() as IStreamHandler;
                stream = handler.Process(stream);
                if (stream == null) break;
            }
            //// 倒序遍历工厂，后来者优先
            //for (int i = fs.Length - 1; i >= 0; i--)
            //{
            //    // 把数据流分给每一个工厂，看看谁有能力处理数据流，有能力者返回数据流处理器
            //    handler = fs[i].GetHandler(stream);
            //    if (handler != null) break;
            //}
            //if (handler == null) return;

            //// 由该处理器处理数据流
            //handler.Process(stream);
        }
        #endregion
    }
}