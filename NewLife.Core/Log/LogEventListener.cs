using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace NewLife.Log;

/// <summary>日志事件监听器。用于监听内置事件并写入日志</summary>
public class LogEventListener : EventListener
{
    private readonly HashSet<String> _hash = new();
    private readonly HashSet<String> _hash2 = new();

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

            XTrace.WriteLine($"Source={eventSource.Name}");
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
        XTrace.WriteLine($"#{eventData.EventSource?.Name} ID = {eventData.EventId}");
        for (var i = 0; i < eventData.Payload.Count; i++)
        {
            XTrace.WriteLine($"\tValue = \"{eventData.Payload[i]}\"");
        }
#elif NETFRAMEWORK || NETSTANDARD2_0
        XTrace.WriteLine($"#{eventData.EventSource?.Name} ID = {eventData.EventId} Name = {eventData.EventName}");
        for (var i = 0; i < eventData.Payload.Count; i++)
        {
            XTrace.WriteLine($"\tName = \"{eventData.PayloadNames[i]}\" Value = \"{eventData.Payload[i]}\"");
        }
#else
        XTrace.WriteLine($"#{eventData.EventSource?.Name} ThreadID = {eventData.OSThreadId} ID = {eventData.EventId} Name = {eventData.EventName}");
        var names = eventData.PayloadNames;
        if (eventData.Payload != null && names != null)
        {
            for (var i = 0; i < eventData.Payload.Count && i < names.Count; i++)
            {
                XTrace.WriteLine($"\tName = \"{names[i]}\" Value = \"{eventData.Payload[i]}\"");
            }
        }
#endif

        if (!eventData.Message.IsNullOrEmpty()) log.Write(level, eventData.Message);
    }
}