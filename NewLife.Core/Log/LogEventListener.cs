using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace NewLife.Log;

/// <summary>日志事件监听器。用于监听内置事件并写入日志</summary>
public class LogEventListener : EventListener
{
    private readonly HashSet<String> _hash = [];
    private readonly HashSet<String> _hash2 = [];

    /// <summary>实例化</summary>
    /// <param name="sources"></param>
    public LogEventListener(String[] sources)
    {
        foreach (var item in sources)
        {
            _hash.Add(item);
        }
    }

    /// <summary>创建事件源。此时决定要不要跟踪</summary>
    /// <param name="eventSource"></param>
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (_hash.Contains(eventSource.Name))
        {
            var log = XTrace.Log;

            var level = log.Level switch
            {
                LogLevel.All => EventLevel.LogAlways,
                LogLevel.Debug => EventLevel.Verbose,
                LogLevel.Info => EventLevel.Informational,
                LogLevel.Warn => EventLevel.Warning,
                LogLevel.Error => EventLevel.Error,
                LogLevel.Fatal => EventLevel.Critical,
                LogLevel.Off => throw new NotImplementedException(),
                _ => EventLevel.Informational,
            };

            EnableEvents(eventSource, level);
        }
        else if (!_hash2.Contains(eventSource.Name))
        {
            _hash2.Add(eventSource.Name);

            XTrace.WriteLine("Source={0}", eventSource.Name);
        }
    }

    /// <summary>写入事件。监听器拦截，并写入日志</summary>
    /// <param name="eventData"></param>
    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        var log = XTrace.Log;

        var level = eventData.Level switch
        {
            EventLevel.Informational => LogLevel.Info,
            EventLevel.LogAlways => LogLevel.All,
            EventLevel.Critical => LogLevel.Fatal,
            EventLevel.Error => LogLevel.Error,
            EventLevel.Warning => LogLevel.Warn,
            EventLevel.Verbose => LogLevel.Debug,
            _ => LogLevel.Info,
        };

#if NET45
        XTrace.WriteLine("#{0} ID = {1}", eventData.EventSource?.Name, eventData.EventId);
        for (var i = 0; i < eventData.Payload.Count; i++)
        {
            XTrace.WriteLine("\tValue = \"{0}\"", eventData.Payload[i]);
        }
#elif NETFRAMEWORK || NETSTANDARD2_0
        XTrace.WriteLine("#{0} ID = {1} Name = {2}", eventData.EventSource?.Name, eventData.EventId, eventData.EventName);
        for (var i = 0; i < eventData.Payload.Count; i++)
        {
            XTrace.WriteLine("\tName = \"{0}\" Value = \"{1}\"", eventData.PayloadNames[i], eventData.Payload[i]);
        }
#else
        XTrace.WriteLine("#{0} ThreadID = {1} ID = {2} Name = {3}", eventData.EventSource?.Name, eventData.OSThreadId, eventData.EventId, eventData.EventName);
        var names = eventData.PayloadNames;
        if (eventData.Payload != null && names != null)
        {
            for (var i = 0; i < eventData.Payload.Count && i < names.Count; i++)
            {
                XTrace.WriteLine("\tName = \"{0}\" Value = \"{1}\"", names[i], eventData.Payload[i]);
            }
        }
#endif

        if (!eventData.Message.IsNullOrEmpty()) log.Write(level, eventData.Message);
    }
}