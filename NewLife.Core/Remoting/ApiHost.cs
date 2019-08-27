﻿using System;
using System.Collections.Generic;
using System.Linq;
using NewLife.Collections;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;
using NewLife.Model;
using NewLife.Net.Handlers;

namespace NewLife.Remoting
{
    /// <summary>Api主机</summary>
    public abstract class ApiHost : DisposeBase, IApiHost, IExtend
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>编码器</summary>
        public IEncoder Encoder { get; set; }

        /// <summary>处理器</summary>
        public IApiHandler Handler { get; set; }

        /// <summary>调用超时时间。请求发出后，等待响应的最大时间，默认15_000ms</summary>
        public Int32 Timeout { get; set; } = 15_000;

        /// <summary>调用统计</summary>
        public ICounter StatInvoke { get; set; }

        /// <summary>处理统计</summary>
        public ICounter StatProcess { get; set; }

        /// <summary>慢追踪。远程调用或处理时间超过该值时，输出慢调用日志，默认5000ms</summary>
        public Int32 SlowTrace { get; set; } = 5_000;

        /// <summary>用户会话数据</summary>
        public IDictionary<String, Object> Items { get; set; } = new NullableDictionary<String, Object>();

        /// <summary>获取/设置 用户会话数据</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual Object this[String key] { get { return Items[key]; } set { Items[key] = value; } }

        /// <summary>启动时间</summary>
        public DateTime StartTime { get; set; } = DateTime.Now;
        #endregion

        #region 控制器管理
        /// <summary>接口动作管理器</summary>
        public IApiManager Manager { get; } = new ApiManager();

        /// <summary>注册服务提供类。该类的所有公开方法将直接暴露</summary>
        /// <typeparam name="TService"></typeparam>
        public void Register<TService>() where TService : class, new() => Manager.Register<TService>();

        /// <summary>注册服务</summary>
        /// <param name="controller">控制器对象</param>
        /// <param name="method">动作名称。为空时遍历控制器所有公有成员方法</param>
        public void Register(Object controller, String method) => Manager.Register(controller, method);

        /// <summary>显示可用服务</summary>
        protected void ShowService()
        {
            var ms = Manager.Services;
            if (ms.Count > 0)
            {
                Log.Info("可用服务{0}个：", ms.Count);
                var max = ms.Max(e => e.Key.Length);
                foreach (var item in ms)
                {
                    Log.Info("\t{0,-" + (max + 1) + "}{1}\t{2}", item.Key, item.Value, item.Value.Type.FullName);
                }
            }
        }
        #endregion

        #region 方法
        /// <summary>获取消息编码器。重载以指定不同的封包协议</summary>
        /// <returns></returns>
        public virtual IHandler GetMessageCodec() => new StandardCodec { Timeout = Timeout, UserPacket = false };
        #endregion

        #region 请求处理
        /// <summary>处理消息</summary>
        /// <param name="session"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        IMessage IApiHost.Process(IApiSession session, IMessage msg)
        {
            if (msg.Reply) return null;

            var action = "";
            Object result = null;
            var code = 0;

            var st = StatProcess;
            var sw = st.StartCount();
            try
            {
                var enc = session["Encoder"] as IEncoder ?? Encoder;

                try
                {
                    if (!enc.Decode(msg, out action, out _, out var args)) return null;

                    result = OnProcess(session, action, args);
                }
                catch (Exception ex)
                {
                    ex = ex.GetTrue();

                    if (ShowError) WriteLog("{0}", ex);

                    // 支持自定义错误
                    if (ex is ApiException aex)
                    {
                        code = aex.Code;
                        result = ex?.Message;
                    }
                    else
                    {
                        code = 500;
                        result = ex?.Message;
                    }
                }

                // 单向请求无需响应
                if (msg.OneWay) return null;

                return enc.CreateResponse(msg, action, code, result);
            }
            finally
            {
                var msCost = st.StopCount(sw) / 1000;
                if (SlowTrace > 0 && msCost >= SlowTrace) WriteLog($"慢处理[{action}]，Code={code}，耗时{msCost:n0}ms");
            }
        }

        /// <summary>执行</summary>
        /// <param name="session"></param>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected virtual Object OnProcess(IApiSession session, String action, Packet args) => Handler.Execute(session, action, args);
        #endregion

        #region 事件
        /// <summary>新会话。服务端收到新连接，客户端每次连接或断线重连后，可用于做登录</summary>
        /// <param name="session">会话</param>
        /// <param name="state">状态。客户端ISocketClient</param>
        public virtual void OnNewSession(IApiSession session, Object state) { }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>编码器日志</summary>
        public ILog EncoderLog { get; set; } = Logger.Null;

        /// <summary>显示调用和处理错误。默认false</summary>
        public Boolean ShowError { get; set; }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args) => Log?.Info(Name + " " + format, args);

        /// <summary>已重载。返回具有本类特征的字符串</summary>
        /// <returns>String</returns>
        public override String ToString() => Name;
        #endregion
    }
}