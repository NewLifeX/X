using System;
using System.Collections.Generic;

namespace NewLife.Queue.Broker
{
    public class BatchMessageLogRecord
    {
        private readonly IEnumerable<MessageLogRecord> _records;
        private readonly Action<BatchMessageLogRecord, object> _callback;
        private readonly object _parameter;

        public IEnumerable<MessageLogRecord> Records
        {
            get { return _records; }
        }
        public Action<BatchMessageLogRecord, object> Callback
        {
            get { return _callback; }
        }

        public BatchMessageLogRecord(IEnumerable<MessageLogRecord> records, Action<BatchMessageLogRecord, object> callback, object parameter)
        {
            _records = records;
            _callback = callback;
            _parameter = parameter;
        }

        public void OnPersisted()
        {
            if (_callback != null)
            {
                _callback(this, _parameter);
            }
        }
    }
}
