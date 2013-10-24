using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using NewLife.Log;

namespace NewLife.Reflection
{
    /// <summary>API钩子</summary>
    /// <remarks>
    /// 实现上，是两个方法的非托管指针互换，为了方便后面换回来。
    /// 但是很奇怪，UnHook换回来后，执行的代码还是更换后的，也就是无法复原。
    /// 
    /// 一定要注意，在vs中调试会导致Hook失败，尽管换了指针，也无法变更代码执行流程。
    /// </remarks>
    public class ApiHook : DisposeBase
    {
        #region 属性
        private MethodBase _OriMethod;
        /// <summary>原始方法</summary>
        public MethodBase OriMethod { get { return _OriMethod; } set { _OriMethod = value; } }

        private MethodBase _NewMethod;
        /// <summary>新方法</summary>
        public MethodBase NewMethod { get { return _NewMethod; } set { _NewMethod = value; } }
        #endregion

        #region 构造
        /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）。
        /// 因为该方法只会被调用一次，所以该参数的意义不太大。</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (ishooked) UnHook();
        }
        #endregion

        #region 方法
        private Boolean ishooked;
        //private IntPtr[] addresses;
        //private uint ori;
        //private ulong ori64;

        /// <summary>挂钩</summary>
        public void Hook()
        {
            ishooked = true;
            Exchange("ApiHook");
        }

        /// <summary>取消挂钩</summary>
        public void UnHook()
        {
            if (!ishooked) return;
            ishooked = false;

            Exchange("ApiUnHook");
        }

        void Exchange(String action)
        {
            var adds = GetAddress(OriMethod, NewMethod);
            unsafe
            {
                if (IntPtr.Size == 8)
                {
                    var s = (ulong*)adds[0].ToPointer();
                    var d = (ulong*)adds[1].ToPointer();
                    WriteLog("{4} [{0}.{1}] 0x{2:X8} = 0x{3:X8}", OriMethod.DeclaringType.Name, OriMethod.Name, (ulong)s, *s, action);
                    WriteLog("{4} [{0}.{1}] 0x{2:X8} = 0x{3:X8}", NewMethod.DeclaringType.Name, NewMethod.Name, (ulong)d, *d, action);

                    var ori64 = *s;
                    *s = *((ulong*)adds[1].ToPointer());
                    *d = ori64;
                }
                else
                {
                    var s = (uint*)adds[0].ToPointer();
                    var d = (uint*)adds[1].ToPointer();
                    WriteLog("{4} [{0}.{1}] 0x{2:X8} = 0x{3:X8}", OriMethod.DeclaringType.Name, OriMethod.Name, (uint)s, *s, action);
                    WriteLog("{4} [{0}.{1}] 0x{2:X8} = 0x{3:X8}", NewMethod.DeclaringType.Name, NewMethod.Name, (uint)d, *d, action);

                    var ori = *s;
                    *s = *((uint*)adds[1].ToPointer());
                    *d = ori;
                }
            }
        }
        #endregion

        #region 辅助
        [Conditional("DEBUG")]
        void WriteLog(String format, params Object[] args)
        {
            XTrace.WriteLine(format, args);
        }
        #endregion

        #region JIT方法地址
        /// <summary>获取方法在JIT编译后的地址(JIT Stubs)</summary>
        /// <remarks>
        /// MethodBase.DeclaringType.TypeHandle.Value: 指向该类型方法表(编译后)在 JIT Stubs 的起始位置。
        /// Method.MethodHandle.Value: 表示该方法的索引序号。
        /// CLR 2.0 SP2 (2.0.50727.3053) 及其后续版本中，该地址的内存布局发生了变化。直接用 "Method.MethodHandle.Value + 2" 即可得到编译后的地址。
        /// </remarks>
        /// <param name="method"></param>
        /// <returns></returns>
        unsafe public static IntPtr GetMethodAddress(MethodBase method)
        {
            var isAboveNet2Sp2 = Environment.Version.Major >= 2 && Environment.Version.MinorRevision >= 3053;

            // 处理动态方法
            if (method is DynamicMethod)
            {
                //byte* ptr = (byte*)GetDynamicMethodRuntimeHandle(method).ToPointer();

                //FieldInfo fieldInfo = typeof(DynamicMethod).GetField("m_method", BindingFlags.NonPublic | BindingFlags.Instance);
                //byte* ptr = (byte*)((RuntimeMethodHandle)FieldInfoX.Create(fieldInfo).GetValue(method)).Value.ToPointer();
                var ptr = (byte*)((RuntimeMethodHandle)method.GetValue("m_method")).Value.ToPointer();

                if (isAboveNet2Sp2)
                {
                    // 确保方法已经被编译
                    RuntimeHelpers.PrepareMethod(method.MethodHandle);

                    if (IntPtr.Size == 8)
                        return new IntPtr((ulong*)*(ptr + 5) + 12);
                    else
                        return new IntPtr((uint*)*(ptr + 5) + 12);
                }
                else
                {
                    if (IntPtr.Size == 8)
                        return new IntPtr((ulong*)ptr + 6);
                    else
                        return new IntPtr((uint*)ptr + 6);
                }
            }

            // 确保方法已经被编译
            RuntimeHelpers.PrepareMethod(method.MethodHandle);

            if (isAboveNet2Sp2) return new IntPtr((int*)method.MethodHandle.Value.ToPointer() + 2);

            // 要跳过的
            var skip = 10;

            // 读取方法索引
            var location = (UInt64*)(method.MethodHandle.Value.ToPointer());
            var index = (int)(((*location) >> 32) & 0xFF);

            // 区分处理x86和x64
            if (IntPtr.Size == 8)
            {
                // 获取方法表
                var methodTable = (ulong*)method.DeclaringType.TypeHandle.Value.ToPointer();
                return new IntPtr(methodTable + index + skip);
            }
            else
            {
                // 获取方法表
                var methodTable = (uint*)method.DeclaringType.TypeHandle.Value.ToPointer();
                return new IntPtr(methodTable + index + skip);
            }
        }

        static readonly Type mbroType = typeof(MarshalByRefObject);
        /// <summary>替换方法</summary>
        /// <remarks>
        /// Method Address 处所存储的 Native Code Address 是可以修改的，也就意味着我们完全可以用另外一个具有相同签名的方法来替代它，从而达到偷梁换柱(Injection)的目的。
        /// </remarks>
        /// <param name="src"></param>
        /// <param name="des"></param>
        public static void ReplaceMethod(MethodBase src, MethodBase des)
        {
            var adds = GetAddress(src, des);
            ReplaceMethod(adds[0], adds[1]);
        }

        unsafe static IntPtr[] GetAddress(MethodBase src, MethodBase des)
        {
            var adds = new IntPtr[2];
            if (src.IsStatic && des.IsStatic ||
                !mbroType.IsAssignableFrom(src.DeclaringType) && !mbroType.IsAssignableFrom(des.DeclaringType))
            {
                adds[0] = GetMethodAddress(src);
                adds[1] = GetMethodAddress(des);
            }
            else if (mbroType.IsAssignableFrom(src.DeclaringType) && mbroType.IsAssignableFrom(des.DeclaringType))
            {
                adds[0] = src.MethodHandle.GetFunctionPointer();
                adds[1] = des.MethodHandle.GetFunctionPointer();
            }

            return adds;
        }

        unsafe private static void ReplaceMethod(IntPtr src, IntPtr dest)
        {
            // 区分处理x86和x64
            if (IntPtr.Size == 8)
            {
                var d = (ulong*)src.ToPointer();
                *d = *((ulong*)dest.ToPointer());
            }
            else
            {
                var d = (uint*)src.ToPointer();
                *d = *((uint*)dest.ToPointer());
            }
        }
        #endregion
    }
}