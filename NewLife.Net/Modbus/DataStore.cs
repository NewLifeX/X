using System;

namespace NewLife.Net.Modbus
{
    /// <summary>默认数据存储</summary>
    public class DataStore : IDataStore
    {
        private IBitStore _Inputs;
        /// <summary>离散量输入</summary>
        public IBitStore Inputs { get { return _Inputs; } }

        private IBitStore _Coils;
        /// <summary>线圈</summary>
        public IBitStore Coils { get { return _Coils; } }

        private IWordStore _InputRegisters;
        /// <summary>输入寄存器</summary>
        public IWordStore InputRegisters { get { return _InputRegisters; } }

        private IWordStore _HoldingRegisters;
        /// <summary>保持寄存器</summary>
        public IWordStore HoldingRegisters { get { return _HoldingRegisters; } }

        /// <summary>默认初始化</summary>
        public DataStore() : this(new BitStore(), new WordStore()) { }

        /// <summary>使用两个存储器初始化，两两共用</summary>
        /// <param name="bit"></param>
        /// <param name="word"></param>
        public DataStore(IBitStore bit, IWordStore word) : this(bit, bit, word, word) { }

        /// <summary>使用四个存储器初始化</summary>
        /// <param name="bitInputs"></param>
        /// <param name="bitCoils"></param>
        /// <param name="wordInput"></param>
        /// <param name="holding"></param>
        public DataStore(IBitStore bitInputs, IBitStore bitCoils, IWordStore wordInput, IWordStore holding)
        {
            _Inputs = bitInputs;
            _Coils = bitCoils;
            _InputRegisters = wordInput;
            _HoldingRegisters = holding;
        }
    }

    /// <summary>默认位存储</summary>
    public class BitStore : IBitStore
    {
        Boolean[] Coils;

        /// <summary>数量</summary>
        /// <returns></returns>
        public Int32 Count { get { return Coils.Length; } }

        /// <summary>索引器，不影响<see cref="OnWrite"/>事件</summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Boolean this[Int32 i] { get { return Coils[i]; } set { Coils[i] = value; } }

        /// <summary>读取状态</summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Boolean Read(Int32 i) { return Coils[i]; }

        /// <summary>写入状态</summary>
        /// <param name="i"></param>
        /// <param name="flag"></param>
        public void Write(Int32 i, Boolean flag)
        {
            Coils[i] = flag;

            if (OnWrite != null) OnWrite(i, flag);
        }

        /// <summary>初始化</summary>
        public BitStore() : this(0) { }

        /// <summary>初始化指定个数存储位</summary>
        /// <param name="n"></param>
        public BitStore(Int32 n)
        {
            if (n <= 0) n = 16;
            Coils = new Boolean[n];
        }

        /// <summary>写入线圈</summary>
        public event WriteCoilHandler OnWrite;
    }

    /// <summary>默认字存储</summary>
    public class WordStore : IWordStore
    {
        UInt16[] Regs;

        /// <summary>数量</summary>
        /// <returns></returns>
        public Int32 Count { get { return Regs.Length; } }

        /// <summary>索引器，不影响<see cref="OnWrite"/>事件</summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public UInt16 this[Int32 i] { get { return Regs[i]; } set { Regs[i] = value; } }

        /// <summary>读取</summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public UInt16 Read(Int32 i) { return Regs[i]; }

        /// <summary>写入</summary>
        /// <param name="i"></param>
        /// <param name="value"></param>
        public void Write(Int32 i, UInt16 value)
        {
            Regs[i] = value;

            if (OnWrite != null) OnWrite(i, value);
        }

        /// <summary>初始化</summary>
        public WordStore() : this(0) { }

        /// <summary>初始化指定个数存储位</summary>
        /// <param name="n"></param>
        public WordStore(Int32 n)
        {
            if (n <= 0) n = 16;
            Regs = new UInt16[n];
        }

        /// <summary>写入寄存器</summary>
        public event WriteRegisterHandler OnWrite;
    }
}