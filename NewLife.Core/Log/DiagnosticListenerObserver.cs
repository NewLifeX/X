#if !NET40
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace NewLife.Log
{
    /// <summary>诊断监听器的观察者</summary>
    public class DiagnosticListenerObserver : IObserver<DiagnosticListener>
    {
        /// <summary>跟踪器</summary>
        public ITracer Tracer { get; set; }

        private readonly Dictionary<String, TraceDiagnosticListener> _listeners = new Dictionary<String, TraceDiagnosticListener>();

        /// <summary>订阅新的监听器</summary>
        /// <param name="listenerName"></param>
        public void Subscribe(String listenerName)
        {
            _listeners.Add(listenerName, new TraceDiagnosticListener
            {
                //StartName = startName,
                //EndName = endName,
                //ErrorName = errorName,
            });
        }

        void IObserver<DiagnosticListener>.OnCompleted() => throw new NotImplementedException();

        void IObserver<DiagnosticListener>.OnError(Exception error) => throw new NotImplementedException();

        void IObserver<DiagnosticListener>.OnNext(DiagnosticListener value)
        {
            if (_listeners.TryGetValue(value.Name, out var listener)) value.Subscribe(listener);
        }
    }

    /// <summary>跟踪诊断监听器</summary>
    public class TraceDiagnosticListener : IObserver<KeyValuePair<String, Object>>
    {
        #region 属性
        //public String StartName { get; set; }

        //public String EndName { get; set; }

        //public String ErrorName { get; set; }

        /// <summary>跟踪器</summary>
        public ITracer Tracer { get; set; }
        #endregion

        void IObserver<KeyValuePair<String, Object>>.OnCompleted() => throw new NotImplementedException();

        void IObserver<KeyValuePair<String, Object>>.OnError(Exception error) => throw new NotImplementedException();

        void IObserver<KeyValuePair<String, Object>>.OnNext(KeyValuePair<String, Object> value)
        {
            if (value.Key.IsNullOrEmpty()) return;

            // 当前活动名字匹配
            var activity = Activity.Current;
            if (activity != null && activity.OperationName + ".Stop" == value.Key)
            {
                XTrace.WriteLine(value.Key);
            }
        }
    }
}
#endif