using System;
using System.Data.Common;

namespace XCode.TDengine
{
    internal class TDengineDataAdapter : DbDataAdapter
    {
        private readonly Boolean disposeSelect = true;

        private static readonly Object _updatingEventPH = new();

        private static readonly Object _updatedEventPH = new();

        private Boolean disposed;

        public event EventHandler<RowUpdatingEventArgs> RowUpdating
        {
            add
            {
                CheckDisposed();
                var eventHandler = (EventHandler<RowUpdatingEventArgs>)base.Events[_updatingEventPH];
                if (eventHandler != null && value.Target is DbCommandBuilder)
                {
                    var eventHandler2 = (EventHandler<RowUpdatingEventArgs>)FindBuilder(eventHandler);
                    if (eventHandler2 != null)
                    {
                        base.Events.RemoveHandler(_updatingEventPH, eventHandler2);
                    }
                }
                base.Events.AddHandler(_updatingEventPH, value);
            }
            remove
            {
                CheckDisposed();
                base.Events.RemoveHandler(_updatingEventPH, value);
            }
        }

        public event EventHandler<RowUpdatedEventArgs> RowUpdated
        {
            add
            {
                CheckDisposed();
                base.Events.AddHandler(_updatedEventPH, value);
            }
            remove
            {
                CheckDisposed();
                base.Events.RemoveHandler(_updatedEventPH, value);
            }
        }

        public TDengineDataAdapter()
        {
        }

        public TDengineDataAdapter(TDengineCommand cmd)
        {
            SelectCommand = cmd;
            disposeSelect = false;
        }

        private void CheckDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(typeof(TDengineDataAdapter).Name);
        }

        protected override void Dispose(Boolean disposing)
        {
            try
            {
                if (!disposed && disposing)
                {
                    if (disposeSelect && SelectCommand != null)
                    {
                        SelectCommand.Dispose();
                        SelectCommand = null;
                    }
                    if (InsertCommand != null)
                    {
                        InsertCommand.Dispose();
                        InsertCommand = null;
                    }
                    if (UpdateCommand != null)
                    {
                        UpdateCommand.Dispose();
                        UpdateCommand = null;
                    }
                    if (DeleteCommand != null)
                    {
                        DeleteCommand.Dispose();
                        DeleteCommand = null;
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
                disposed = true;
            }
        }

        internal static Delegate FindBuilder(MulticastDelegate mcd)
        {
            if (mcd != null)
            {
                var invocationList = mcd.GetInvocationList();
                for (var i = 0; i < invocationList.Length; i++)
                {
                    if (invocationList[i].Target is DbCommandBuilder)
                    {
                        return invocationList[i];
                    }
                }
            }
            return null;
        }

        protected override void OnRowUpdating(RowUpdatingEventArgs value) => (base.Events[_updatingEventPH] as EventHandler<RowUpdatingEventArgs>)?.Invoke(this, value);

        protected override void OnRowUpdated(RowUpdatedEventArgs value) => (base.Events[_updatedEventPH] as EventHandler<RowUpdatedEventArgs>)?.Invoke(this, value);
    }
}