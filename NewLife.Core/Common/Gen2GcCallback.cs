using System.ComponentModel;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace NewLife;

/// <summary>Gen2垃圾回收回调</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public class Gen2GcCallback : CriticalFinalizerObject
{
    private readonly Func<Boolean> _callback0;

    private readonly Func<Object, Boolean> _callback1;

    private GCHandle _weakTargetObj;

    private Gen2GcCallback(Func<Boolean> callback) => _callback0 = callback;

    private Gen2GcCallback(Func<Object, Boolean> callback, Object targetObj)
    {
        _callback1 = callback;
        _weakTargetObj = GCHandle.Alloc(targetObj, GCHandleType.Weak);
    }

    /// <summary>
    /// Registers a callback to be invoked during Gen2 garbage collection.
    /// </summary>
    /// <param name="callback">The callback function to be invoked.</param>
    public static void Register(Func<Boolean> callback)
    {
        new Gen2GcCallback(callback);
    }

    /// <summary>
    /// Registers a callback to be invoked during Gen2 garbage collection with a target object.
    /// </summary>
    /// <param name="callback">The callback function to be invoked.</param>
    /// <param name="targetObj">The target object associated with the callback.</param>
    public static void Register(Func<Object, Boolean> callback, Object targetObj)
    {
        new Gen2GcCallback(callback, targetObj);
    }

    ~Gen2GcCallback()
    {
        if (_weakTargetObj.IsAllocated)
        {
            var target = _weakTargetObj.Target;
            if (target == null)
            {
                _weakTargetObj.Free();
                return;
            }
            try
            {
                if (!_callback1(target))
                {
                    _weakTargetObj.Free();
                    return;
                }
            }
            catch { }
        }
        else
        {
            try
            {
                if (!_callback0())
                {
                    return;
                }
            }
            catch { }
        }
        GC.ReRegisterForFinalize(this);
    }
}