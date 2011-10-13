using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System;

namespace NewLife.Reflection
{
    /// <summary>IL指令</summary>
    public class ILInstruction
    {
        #region 属性
        private OpCode _Code;
        /// <summary>指令</summary>
        public OpCode Code
        {
            get { return _Code; }
            set { _Code = value; }
        }

        private object _Operand;
        /// <summary>操作</summary>
        public object Operand
        {
            get { return _Operand; }
            set { _Operand = value; }
        }

        private byte[] _OperandData;
        /// <summary>操作数据</summary>
        public byte[] OperandData
        {
            get { return _OperandData; }
            set { _OperandData = value; }
        }

        private int _Offset;
        /// <summary>偏移</summary>
        public int Offset
        {
            get { return _Offset; }
            set { _Offset = value; }
        }
        #endregion

        /// <summary>
        /// 已重载。返回指令的字符串形式
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("L_{0:0000}: {1}", _Offset, _Code);
            if (_Operand == null) return sb.ToString();

            switch (_Code.OperandType)
            {
                case OperandType.InlineField:
                    FieldInfo fOperand = ((FieldInfo)_Operand);
                    sb.Append(" " + FixType(fOperand.FieldType) + " " + FixType(fOperand.ReflectedType) + "::" + fOperand.Name + "");
                    break;
                case OperandType.InlineMethod:
                    try
                    {
                        MethodInfo mOperand = (MethodInfo)_Operand;
                        sb.Append(" ");
                        if (!mOperand.IsStatic) sb.Append("instance ");
                        sb.Append(FixType(mOperand.ReturnType) + " " + FixType(mOperand.ReflectedType) + "::" + mOperand.Name + "()");
                    }
                    catch
                    {
                        try
                        {
                            ConstructorInfo mOperand = (ConstructorInfo)_Operand;
                            sb.Append(" ");
                            if (!mOperand.IsStatic) sb.Append("instance ");
                            sb.Append("void " + FixType(mOperand.ReflectedType) + "::" + mOperand.Name + "()");
                        }
                        catch { }
                    }
                    break;
                case OperandType.ShortInlineBrTarget:
                    sb.Append(" L_" + ((int)_Operand).ToString("0000"));
                    break;
                case OperandType.InlineType:
                    sb.Append(" " + FixType((Type)_Operand));
                    break;
                case OperandType.InlineString:
                    if (_Operand.ToString() == "\r\n")
                        sb.Append(" \"\\r\\n\"");
                    else
                        sb.Append(" \"" + _Operand.ToString() + "\"");
                    break;
                default:
                    sb.Append("不支持");
                    break;
            }

            return sb.ToString();

        }

        /// <summary>
        /// 取得类型的友好名
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static string FixType(Type type)
        {
            string result = type.ToString();
            switch (result)
            {
                case "System.string":
                case "System.String":
                case "String":
                    result = "string"; break;
                case "System.Int32":
                case "Int":
                case "Int32":
                    result = "int"; break;
            }
            return result;
        }
    }
}