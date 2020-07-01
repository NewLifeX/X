using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using NewLife.Log;
using NewLife.Threading;

namespace NewLife.Http
{
    /// <summary>默认HttpClient工厂</summary>
    public class DefaultHttpClientFactory : IHttpClientFactory
    {
        /// <summary>处理器有效时间</summary>
        public TimeSpan HandlerLifetime { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>内部处理器</summary>
        public HttpMessageHandler InnerHandler { get; set; }

        private readonly Func<String, Lazy<ActiveHandlerTrackingEntry>> _entryFactory;

        // Default time of 10s for cleanup seems reasonable.
        // Quick math:
        // 10 distinct named clients * expiry time >= 1s = approximate cleanup queue of 100 items
        //
        // This seems frequent enough. We also rely on GC occurring to actually trigger disposal.
        private readonly TimeSpan DefaultCleanupInterval = TimeSpan.FromSeconds(10);

        // We use a new timer for each regular cleanup cycle, protected with a lock. Note that this scheme
        // doesn't give us anything to dispose, as the timer is started/stopped as needed.
        //
        // There's no need for the factory itself to be disposable. If you stop using it, eventually everything will
        // get reclaimed.
        private TimerX _cleanupTimer;
        private readonly Object _cleanupTimerLock;
        private readonly Object _cleanupActiveLock;

        // Collection of 'active' handlers.
        //
        // Using lazy for synchronization to ensure that only one instance of HttpMessageHandler is created
        // for each name.
        //
        // internal for tests
        internal readonly ConcurrentDictionary<String, Lazy<ActiveHandlerTrackingEntry>> _activeHandlers;

        // Collection of 'expired' but not yet disposed handlers.
        //
        // Used when we're rotating handlers so that we can dispose HttpMessageHandler instances once they
        // are eligible for garbage collection.
        //
        // internal for tests
        internal readonly ConcurrentQueue<ExpiredHandlerTrackingEntry> _expiredHandlers;
        private readonly TimerCallback _expiryCallback;

        /// <summary>实例化</summary>
        public DefaultHttpClientFactory()
        {
            // case-sensitive because named options is.
            _activeHandlers = new ConcurrentDictionary<String, Lazy<ActiveHandlerTrackingEntry>>(StringComparer.Ordinal);
            _entryFactory = (name) =>
            {
                return new Lazy<ActiveHandlerTrackingEntry>(() => CreateHandlerEntry(name), LazyThreadSafetyMode.ExecutionAndPublication);
            };

            _expiredHandlers = new ConcurrentQueue<ExpiredHandlerTrackingEntry>();
            _expiryCallback = ExpiryTimer_Tick;

            _cleanupTimerLock = new Object();
            _cleanupActiveLock = new Object();
        }

        /// <summary>创建HttpClient</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual HttpClient CreateClient(String name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            var handler = CreateHandler(name);
            var client = new HttpClient(handler, disposeHandler: false);

            return client;
        }

        /// <summary>创建处理器</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual HttpMessageHandler CreateHandler(String name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            var entry = _activeHandlers.GetOrAdd(name, _entryFactory).Value;

            StartHandlerEntryTimer(entry);

            return entry.Handler;
        }

        // Internal for tests
        internal ActiveHandlerTrackingEntry CreateHandlerEntry(String name)
        {
            // Wrap the handler so we can ensure the inner handler outlives the outer handler.
            var handler = new LifetimeTrackingHttpMessageHandler(InnerHandler);

            // Note that we can't start the timer here. That would introduce a very very subtle race condition
            // with very short expiry times. We need to wait until we've actually handed out the handler once
            // to start the timer.
            //
            // Otherwise it would be possible that we start the timer here, immediately expire it (very short
            // timer) and then dispose it without ever creating a client. That would be bad. It's unlikely
            // this would happen, but we want to be sure.
            return new ActiveHandlerTrackingEntry(name, handler, HandlerLifetime);
        }

        // Internal for tests
        internal void ExpiryTimer_Tick(Object state)
        {
            var active = (ActiveHandlerTrackingEntry)state;

            // The timer callback should be the only one removing from the active collection. If we can't find
            // our entry in the collection, then this is a bug.
            var removed = _activeHandlers.TryRemove(active.Name, out var found);
            Debug.Assert(removed, "Entry not found. We should always be able to remove the entry");
            Debug.Assert(Object.ReferenceEquals(active, found.Value), "Different entry found. The entry should not have been replaced");

            // At this point the handler is no longer 'active' and will not be handed out to any new clients.
            // However we haven't dropped our strong reference to the handler, so we can't yet determine if
            // there are still any other outstanding references (we know there is at least one).
            //
            // We use a different state object to track expired handlers. This allows any other thread that acquired
            // the 'active' entry to use it without safety problems.
            var expired = new ExpiredHandlerTrackingEntry(active);
            _expiredHandlers.Enqueue(expired);

            //Log.HandlerExpired(_logger, active.Name, active.Lifetime);

            StartCleanupTimer();
        }

        // Internal so it can be overridden in tests
        internal virtual void StartHandlerEntryTimer(ActiveHandlerTrackingEntry entry)
        {
            entry.StartExpiryTimer(_expiryCallback);
        }

        // Internal so it can be overridden in tests
        internal virtual void StartCleanupTimer()
        {
            lock (_cleanupTimerLock)
            {
                if (_cleanupTimer == null)
                {
                    //_cleanupTimer = NonCapturingTimer.Create(_cleanupCallback, this, DefaultCleanupInterval, Timeout.InfiniteTimeSpan);
                    _cleanupTimer = TimerX.Delay(CleanupTimer_Tick, (Int32)DefaultCleanupInterval.TotalMilliseconds);
                }
            }
        }

        // Internal so it can be overridden in tests
        internal virtual void StopCleanupTimer()
        {
            lock (_cleanupTimerLock)
            {
                _cleanupTimer.Dispose();
                _cleanupTimer = null;
            }
        }

        // Internal for tests
        internal void CleanupTimer_Tick(Object state)
        {
            // Stop any pending timers, we'll restart the timer if there's anything left to process after cleanup.
            //
            // With the scheme we're using it's possible we could end up with some redundant cleanup operations.
            // This is expected and fine.
            //
            // An alternative would be to take a lock during the whole cleanup process. This isn't ideal because it
            // would result in threads executing ExpiryTimer_Tick as they would need to block on cleanup to figure out
            // whether we need to start the timer.
            StopCleanupTimer();

            if (!Monitor.TryEnter(_cleanupActiveLock))
            {
                // We don't want to run a concurrent cleanup cycle. This can happen if the cleanup cycle takes
                // a long time for some reason. Since we're running user code inside Dispose, it's definitely
                // possible.
                //
                // If we end up in that position, just make sure the timer gets started again. It should be cheap
                // to run a 'no-op' cleanup.
                StartCleanupTimer();
                return;
            }

            try
            {
                var initialCount = _expiredHandlers.Count;
                //Log.CleanupCycleStart(_logger, initialCount);

                //var stopwatch = ValueStopwatch.StartNew();

                var disposedCount = 0;
                for (var i = 0; i < initialCount; i++)
                {
                    // Since we're the only one removing from _expired, TryDequeue must always succeed.
                    _expiredHandlers.TryDequeue(out var entry);
                    Debug.Assert(entry != null, "Entry was null, we should always get an entry back from TryDequeue");

                    if (entry.CanDispose)
                    {
                        try
                        {
                            entry.InnerHandler.Dispose();
                            //entry.Scope?.Dispose();
                            disposedCount++;
                        }
                        catch (Exception ex)
                        {
                            //Log.CleanupItemFailed(_logger, entry.Name, ex);
                            XTrace.WriteException(ex);
                        }
                    }
                    else
                    {
                        // If the entry is still live, put it back in the queue so we can process it
                        // during the next cleanup cycle.
                        _expiredHandlers.Enqueue(entry);
                    }
                }

                //Log.CleanupCycleEnd(_logger, stopwatch.GetElapsedTime(), disposedCount, _expiredHandlers.Count);
            }
            finally
            {
                Monitor.Exit(_cleanupActiveLock);
            }

            // We didn't totally empty the cleanup queue, try again later.
            if (_expiredHandlers.Count > 0)
            {
                StartCleanupTimer();
            }
        }

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; }

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
        #endregion
    }

    // Thread-safety: We treat this class as immutable except for the timer. Creating a new object
    // for the 'expiry' pool simplifies the threading requirements significantly.
    internal class ActiveHandlerTrackingEntry
    {
        private readonly Object _lock;
        private Boolean _timerInitialized;
        private TimerX _timer;
        private TimerCallback _callback;

        public ActiveHandlerTrackingEntry(String name, LifetimeTrackingHttpMessageHandler handler, TimeSpan lifetime)
        {
            Name = name;
            Handler = handler;
            Lifetime = lifetime;

            _lock = new Object();
        }

        public LifetimeTrackingHttpMessageHandler Handler { get; private set; }

        public TimeSpan Lifetime { get; }

        public String Name { get; }

        public void StartExpiryTimer(TimerCallback callback)
        {
            if (Lifetime <= TimeSpan.Zero)
            {
                return; // never expires.
            }

#if NET4
            if (_timerInitialized) return;
#else
            if (Volatile.Read(ref _timerInitialized)) return;
#endif

            StartExpiryTimerSlow(callback);
        }

        private void StartExpiryTimerSlow(TimerCallback callback)
        {
            //Debug.Assert(Lifetime != Timeout.InfiniteTimeSpan);

            lock (_lock)
            {
#if NET4
                if (_timerInitialized) return;
#else
                if (Volatile.Read(ref _timerInitialized)) return;
#endif

                _callback = callback;
                //_timer = NonCapturingTimer.Create(_timerCallback, this, Lifetime, Timeout.InfiniteTimeSpan);
                _timer = TimerX.Delay(Timer_Tick, (Int32)Lifetime.TotalMilliseconds);
                _timerInitialized = true;
            }
        }

        private void Timer_Tick(Object state)
        {
            Debug.Assert(_callback != null);
            Debug.Assert(_timer != null);

            lock (_lock)
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;

                    _callback(this);
                }
            }
        }
    }

    // Thread-safety: This class is immutable
    internal class ExpiredHandlerTrackingEntry
    {
        private readonly WeakReference _livenessTracker;

        // IMPORTANT: don't cache a reference to `other` or `other.Handler` here.
        // We need to allow it to be GC'ed.
        public ExpiredHandlerTrackingEntry(ActiveHandlerTrackingEntry other)
        {
            Name = other.Name;
            //Scope = other.Scope;

            _livenessTracker = new WeakReference(other.Handler);
            InnerHandler = other.Handler.InnerHandler;
        }

        public Boolean CanDispose => !_livenessTracker.IsAlive;

        public HttpMessageHandler InnerHandler { get; }

        public String Name { get; }

        //public IServiceScope Scope { get; }
    }

    // This a marker used to check if the underlying handler should be disposed. HttpClients
    // share a reference to an instance of this class, and when it goes out of scope the inner handler
    // is eligible to be disposed.
    internal class LifetimeTrackingHttpMessageHandler : DelegatingHandler
    {
        public LifetimeTrackingHttpMessageHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override void Dispose(bool disposing)
        {
            // The lifetime of this is tracked separately by ActiveHandlerTrackingEntry
        }
    }
}