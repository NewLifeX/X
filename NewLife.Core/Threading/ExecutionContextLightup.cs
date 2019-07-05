using System;

#if NET4
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
            _executionContext = executionContext;
        }

        protected override object GetInstance()
        {
            return _executionContext;
        }

        public ExecutionContextLightup Capture()
        {
            object obj;
            if (base.TryCall<object>(ref _capture, "Capture", out obj) && obj != null)
            {
                return new ExecutionContextLightup(obj);
            }
            return null;
        }

        public ExecutionContextLightup CreateCopy()
        {
            object executionContext = base.Call<object>(ref _createCopy, "CreateCopy");
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
            Delegate methodAccessor = base.GetMethodAccessor(ref _run, type, "Run", true);
            methodAccessor.DynamicInvoke(new object[]
            {
                executionContext._executionContext,
                dlg,
                state
            });
        }

        public void Dispose()
        {
            base.Call(ref _dispose, "Dispose");
        }
    }
}
#endif