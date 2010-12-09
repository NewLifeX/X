using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NewLife.Reflection
{
    /// <summary>
    /// 动态生成代码Emit助手。仅提供扩展功能，不封装基本功能
    /// </summary>
    public class EmitHelper
    {
        #region 属性
        private ILGenerator _IL;
        /// <summary>IL代码生成器</summary>
        public ILGenerator IL
        {
            get { return _IL; }
            private set { _IL = value; }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 实例化
        /// </summary>
        /// <param name="il"></param>
        public EmitHelper(ILGenerator il) { IL = il; }
        #endregion

        #region 方法
        /// <summary>
        /// 基于Ldc_I4指令的整数推送，自动选择最合适的指令
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public EmitHelper Ldc_I4(Int32 value)
        {
            switch (value)
            {
                case -1:
                    IL.Emit(OpCodes.Ldc_I4_M1);
                    return this;
                case 0:
                    IL.Emit(OpCodes.Ldc_I4_0);
                    return this;
                case 1:
                    IL.Emit(OpCodes.Ldc_I4_1);
                    return this;
                case 2:
                    IL.Emit(OpCodes.Ldc_I4_2);
                    return this;
                case 3:
                    IL.Emit(OpCodes.Ldc_I4_3);
                    return this;
                case 4:
                    IL.Emit(OpCodes.Ldc_I4_4);
                    return this;
                case 5:
                    IL.Emit(OpCodes.Ldc_I4_5);
                    return this;
                case 6:
                    IL.Emit(OpCodes.Ldc_I4_6);
                    return this;
                case 7:
                    IL.Emit(OpCodes.Ldc_I4_7);
                    return this;
                case 8:
                    IL.Emit(OpCodes.Ldc_I4_8);
                    return this;
            }

            if (value > -129 && value < 128)
                IL.Emit(OpCodes.Ldc_I4_S, (SByte)value);
            else
                IL.Emit(OpCodes.Ldc_I4, value);

            return this;
        }

        /// <summary>
        /// 基于Ldarg指令的参数加载，自动选择最合适的指令
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public EmitHelper Ldarg(Int32 value)
        {
            switch (value)
            {
                case 0:
                    IL.Emit(OpCodes.Ldarg_0);
                    return this;
                case 1:
                    IL.Emit(OpCodes.Ldarg_1);
                    return this;
                case 2:
                    IL.Emit(OpCodes.Ldarg_2);
                    return this;
                case 3:
                    IL.Emit(OpCodes.Ldarg_3);
                    return this;
                default:
                    IL.Emit(OpCodes.Ldarg, value);
                    return this;
            }
        }

        /// <summary>
        /// 基于Stloc指令的弹栈，自动选择最合适的指令
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public EmitHelper Stloc(Int32 value)
        {
            switch (value)
            {
                case 0:
                    IL.Emit(OpCodes.Stloc_0);
                    return this;
                case 1:
                    IL.Emit(OpCodes.Stloc_1);
                    return this;
                case 2:
                    IL.Emit(OpCodes.Stloc_2);
                    return this;
                case 3:
                    IL.Emit(OpCodes.Stloc_3);
                    return this;
                default:
                    IL.Emit(OpCodes.Stloc, value);
                    return this;
            }
        }

        /// <summary>
        /// 基于Ldloc指令的压栈，自动选择最合适的指令
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public EmitHelper Ldloc(Int32 value)
        {
            switch (value)
            {
                case 0:
                    IL.Emit(OpCodes.Ldloc_0);
                    return this;
                case 1:
                    IL.Emit(OpCodes.Ldloc_1);
                    return this;
                case 2:
                    IL.Emit(OpCodes.Ldloc_2);
                    return this;
                case 3:
                    IL.Emit(OpCodes.Ldloc_3);
                    return this;
                default:
                    IL.Emit(OpCodes.Ldloc, value);
                    return this;
            }
        }

        /// <summary>
        /// 将位于指定数组索引处的包含对象引用的元素作为 O 类型（对象引用）加载到计算堆栈的顶部。
        /// </summary>
        /// <returns></returns>
        public EmitHelper Ldelem_Ref()
        {
            IL.Emit(OpCodes.Ldelem_Ref);
            return this;
        }

        /// <summary>
        /// 用计算堆栈上的对象 ref 值（O 类型）替换给定索引处的数组元素。
        /// </summary>
        /// <returns></returns>
        public EmitHelper Stelem_Ref()
        {
            IL.Emit(OpCodes.Stelem_Ref);
            return this;
        }

        /// <summary>
        /// 把一个类型转为指定类型，值类型装箱，引用类型直接Cast
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public EmitHelper CastFromObject(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            if (type != typeof(Object))
            {
                if (type.IsValueType)
                    IL.Emit(OpCodes.Unbox_Any, type);
                else
                    IL.Emit(OpCodes.Castclass, type);
            }

            return this;
        }

        /// <summary>
        /// 装箱
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public EmitHelper Box(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            if (type.IsValueType) IL.Emit(OpCodes.Box, type);
            return this;
        }

        /// <summary>
        /// 调用
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public EmitHelper Call(MethodInfo method)
        {
            if (method.IsStatic || method.DeclaringType.IsValueType || !method.IsVirtual)
                IL.EmitCall(OpCodes.Call, method, null);
            else
                IL.EmitCall(OpCodes.Callvirt, method, null);
            //if (method.IsVirtual)
            //    IL.EmitCall(OpCodes.Callvirt, method, null);
            //else
            //    IL.EmitCall(OpCodes.Call, method, null);

            return this;
        }

        /// <summary>
        /// 返回
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public EmitHelper Ret(MethodInfo method)
        {
            if (method.ReturnType == typeof(void))
                IL.Emit(OpCodes.Ldnull);
            else if (method.ReturnType.IsValueType)
                IL.Emit(OpCodes.Box, method.ReturnType);

            IL.Emit(OpCodes.Ret);

            return this;
        }
        #endregion

        #region 复杂方法
        /// <summary>
        /// 为引用参数声明本地变量
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public EmitHelper CreateLocalsForByRefParams(MethodBase method)
        {
            Int32 firstParamIndex = method.IsStatic ? 0 : 1;
            Int32 refParams = 0;
            ParameterInfo[] ps = method.GetParameters();
            for (Int32 i = 0; i < ps.Length; i++)
            {
                // 处理引用类型参数
                if (!ps[i].ParameterType.IsByRef) continue;

                Type type = ps[i].ParameterType.GetElementType();
                IL.DeclareLocal(type);
                // 处理输出类型
                if (ps[i].IsOut)
                {
                    this.Ldarg(firstParamIndex)
                        .Ldc_I4(i)
                        .Ldelem_Ref()
                        .CastFromObject(type)
                        .Stloc(refParams);
                }
                refParams++;
            }

            return this;
        }

        /// <summary>
        /// 将引用参数赋值到数组
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public EmitHelper AssignByRefParamsToArray(MethodBase method)
        {
            Int32 firstParamIndex = method.IsStatic ? 0 : 1;
            Int32 refParam = 0;
            ParameterInfo[] ps = method.GetParameters();
            for (Int32 i = 0; i < ps.Length; i++)
            {
                // 处理引用类型参数
                if (!ps[i].ParameterType.IsByRef) continue;

                this.Ldarg(firstParamIndex)
                    .Ldc_I4(i)
                    .Ldloc(refParam++)
                    .Box(ps[i].ParameterType.GetElementType())
                    .Stelem_Ref();
            }

            return this;
        }

        /// <summary>
        /// 将参数压栈
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public EmitHelper PushParamsOrLocalsToStack(MethodBase method)
        {
            Int32 firstParamIndex = method.IsStatic ? 0 : 1;
            Int32 refParam = 0;
            ParameterInfo[] ps = method.GetParameters();
            for (Int32 i = 0; i < ps.Length; i++)
            {
                if (ps[i].ParameterType.IsByRef)
                {
                    IL.Emit(OpCodes.Ldloc_S, refParam++);
                }
                else
                {
                    this.Ldarg(firstParamIndex)
                        .Ldc_I4(i)
                        .Ldelem_Ref()
                        .CastFromObject(ps[i].ParameterType);
                }
            }

            return this;
        }

        /// <summary>
        /// 将参数压栈
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public EmitHelper PushParams(MethodBase method)
        {
            Int32 firstParamIndex = method.IsStatic ? 0 : 1;
            ParameterInfo[] ps = method.GetParameters();
            for (Int32 i = 0; i < ps.Length; i++)
            {
                this.Ldarg(firstParamIndex)
                    .Ldc_I4(i)
                    .Ldelem_Ref()
                    .CastFromObject(ps[i].ParameterType);
            }

            return this;
        }
        #endregion
    }
}