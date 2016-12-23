using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NewLife.Log;

namespace NewLife.Queue.Utilities
{
    public class BufferQueue<TMessage>
    {
        private int _requestsWriteThreshold;
        private ConcurrentQueue<TMessage> _inputQueue;
        private ConcurrentQueue<TMessage> _processQueue;
        private Action<TMessage> _handleMessageAction;
        private readonly string _name;
        private readonly ILog _logger;
        private int _isProcesingMessage;

        public BufferQueue(string name, int requestsWriteThreshold, Action<TMessage> handleMessageAction)
        {
            _name = name;
            _requestsWriteThreshold = requestsWriteThreshold;
            _handleMessageAction = handleMessageAction;
            _inputQueue = new ConcurrentQueue<TMessage>();
            _processQueue = new ConcurrentQueue<TMessage>();
            _logger = QueueService.Log;
        }

        public void EnqueueMessage(TMessage message)
        {
            _inputQueue.Enqueue(message);
            TryProcessMessages();

            if (_inputQueue.Count >= _requestsWriteThreshold)
            {
                Thread.Sleep(1);
            }
        }

        private void TryProcessMessages()
        {
            if (Interlocked.CompareExchange(ref _isProcesingMessage, 1, 0) == 0)
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        if (_processQueue.Count == 0 && _inputQueue.Count > 0)
                        {
                            SwapInputQueue();
                        }

                        if (_processQueue.Count > 0)
                        {
                            var count = 0;
                            TMessage message;
                            while (_processQueue.TryDequeue(out message))
                            {
                                try
                                {
                                    _handleMessageAction(message);
                                }
                                catch (Exception ex)
                                {
                                    var errorMessage = _name + " process message has exception.";
                                    if (_logger != null)
                                    {
                                        _logger.Error(errorMessage, ex);
                                    }
                                }
                                finally
                                {
                                    count++;
                                }
                            }
                            if (_logger.Level== LogLevel.Debug)
                            {
                                _logger.Debug("BufferQueue[name={0}], batch process {1} messages.", _name, count);
                            }
                        }
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _isProcesingMessage, 0);
                        if (_inputQueue.Count > 0)
                        {
                            TryProcessMessages();
                        }
                    }
                });   
            }   
        }
        private void SwapInputQueue()
        {
            var tmp = _inputQueue;
            _inputQueue = _processQueue;
            _processQueue = tmp;
        }
    }
}
