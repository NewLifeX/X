using System;

namespace System
{
    internal class ExecutionContextLightup : Lightup
    {
        public static readonly ExecutionContextLightup Instance = new ExecutionContextLightup(null);

        private Delegate _dispose;

        private Delegate _capture;

        private Delegate _run;

        private Delegate _createCopy;

        private readonly object _executionContext;

        private ExecutionContextLightup(object executionContext) : base(LightupType.ExecutionContext)
        {
            this._executionContext = executionContext;
        }

        protected override object GetInstance()
        {
            return this._executionContext;
        }

        public ExecutionContextLightup Capture()
        {
            object obj;
            if (base.TryCall<object>(ref this._capture, "Capture", out obj) && obj != null)
            {
                return new ExecutionContextLightup(obj);
            }
            return null;
        }

        public ExecutionContextLightup CreateCopy()
        {
            object executionContext = base.Call<object>(ref this._createCopy, "CreateCopy");
            return new ExecutionContextLightup(executionContext);
        }

        public void Run(ExecutionContextLightup executionContext, Action<object> callback, object state)
        {
            if (LightupType.ExecutionContext == null || LightupType.ContextCallback == null)
            {
                throw new PlatformNotSupportedException();
            }
            Delegate dlg = LightupServices.ReplaceWith(callback, LightupType.ContextCallback);
            Type type = typeof(Action<,,>).MakeGenericType(new Type[]
            {
                LightupType.ExecutionContext,
                LightupType.ContextCallback,
                typeof(object)
            });
            Delegate methodAccessor = base.GetMethodAccessor(ref this._run, type, "Run", true);
            methodAccessor.DynamicInvoke(new object[]
            {
                executionContext._executionContext,
                dlg,
                state
            });
        }

        public void Dispose()
        {
            base.Call(ref this._dispose, "Dispose");
        }
    }
}
