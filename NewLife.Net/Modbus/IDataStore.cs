using System;

namespace NewLife.Net.Modbus
{
    /// <summary>数据存储接口</summary>
    public interface IDataStore
    {
        /// <summary>离散量输入</summary>
        IBitStore Inputs { get; }

        /// <summary>线圈</summary>
        IBitStore Coils { get; }

        /// <summary>输入寄存器</summary>
        IWordStore InputRegisters { get; }

        /// <summary>保持寄存器</summary>
        IWordStore HoldingRegisters { get; }
    }

    /// <summary>位存储接口</summary>
    public interface IBitStore
    {
        /// <summary>数量</summary>
        /// <returns></returns>
        Int32 Count { get; }

        /// <summary>索引器</summary>
        /// <param name="i"></param>
        /// <returns></returns>
        Boolean this[Int32 i] { get; set; }

        /// <summary>读取状态</summary>
        /// <param name="i"></param>
        /// <returns></returns>
        Boolean Read(Int32 i);

        /// <summary>写入状态</summary>
        /// <param name="i"></param>
        /// <param name="flag"></param>
        void Write(Int32 i, Boolean flag);

        /// <summary>数组形式</summary>
        /// <returns></returns>
        Boolean[] ToArray();
    }

    /// <summary>字存储接口</summary>
    public interface IWordStore
    {
        /// <summary>寄存器数</summary>
        /// <returns></returns>
        Int32 Count { get; }

        /// <summary>索引器</summary>
        /// <param name="i"></param>
        /// <returns></returns>
        UInt16 this[Int32 i] { get; set; }

        /// <summary>读取</summary>
        /// <param name="i"></param>
        /// <returns></returns>
        UInt16 Read(Int32 i);

        /// <summary>写入</summary>
        /// <param name="i"></param>
        /// <param name="value">数值</param>
        void Write(Int32 i, UInt16 value);

        /// <summary>数组形式</summary>
        /// <returns></returns>
        UInt16[] ToArray();
    }

    /// <summary>存储类助手</summary>
    public static class StoreHelper
    {
        /// <summary>读取整个UInt32</summary>
        /// <param name="store"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        public static UInt32 ReadUInt32(this IWordStore store, Int32 i)
        {
            return (UInt32)((store.Read(i) << 16) + store.Read(i + 1));
        }

        /// <summary>写入整个UInt32</summary>
        /// <param name="store"></param>
        /// <param name="i"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IWordStore WriteUInt32(this IWordStore store, Int32 i, UInt32 value)
        {
            store.Write(i, (UInt16)(value >> 16));
            store.Write(i + 1, (UInt16)(value & 0xFFFF));

            return store;
        }
    }
}