using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NewLife.Net.Modbus;

namespace NewLife.Net.Test.Modbus
{
    [TestClass]
    public class ModbusTest
    {
        [TestMethod]
        public void TestModbus()
        {
            // 实例化一个Udp从机
            var slave = new ModbusSlave();
            slave.Transport = new UdpTransport(502);
            slave.Listen();

            // 实例化一个Udp主机
            var master = new ModbusMaster();
            master.Transport = new UdpTransport("127.0.0.1", 502);

            // 诊断标识
            Assert.IsTrue(master.Diagnostics(), "诊断错误");

            var ids = master.ReportIdentity();
            Assert.IsNotNull(ids, "标识不能为空");

            // 读写线圈
            {
                var fs = master.ReadCoils(2, 3);
                master.WriteSingleCoil(2, !fs[0]);

                var fs2 = master.ReadCoils(2, 3);

                Assert.AreEqual(!fs[0], fs2[0], "读写线圈WriteSingleCoil失败");

                master.WriteMultipleCoils(2, false, true);

                fs = master.ReadInputs(2, 3);
                Assert.AreEqual(false, fs[0], "读写线圈WriteMultipleCoils失败");
                Assert.AreEqual(true, fs[1], "读写线圈WriteMultipleCoils失败");
            }
            // 读写寄存器
            {
                var ds = master.ReadInputRegisters(5, 3);
                master.WriteSingleRegister(5, 998);

                ds = master.ReadInputRegisters(5, 3);
                Assert.AreEqual(998, ds[0], "WriteSingleRegister失败");

                master.WriteMultipleRegisters(5, 321, 123);

                ds = master.ReadHoldingRegisters(5, 3);
                Assert.AreEqual(321, ds[0], "WriteMultipleRegisters失败");
                Assert.AreEqual(123, ds[1], "WriteMultipleRegisters失败");
            }
        }
    }
}