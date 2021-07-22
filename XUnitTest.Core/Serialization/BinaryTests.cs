using System;
using System.IO;
using NewLife;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Serialization
{
    public class BinaryTests
    {
        [Fact]
        public void Fast()
        {
            var model = new MyModel { Code = 1234, Name = "Stone" };
            var pk = Binary.FastWrite(model);
            Assert.Equal(8, pk.Total);
            Assert.Equal("D2090553746F6E65", pk.ToHex());
            Assert.Equal("0gkFU3RvbmU=", pk.ToArray().ToBase64());

            var model2 = Binary.FastRead<MyModel>(pk.GetStream());
            Assert.Equal(model.Code, model2.Code);
            Assert.Equal(model.Name, model2.Name);

            var ms = new MemoryStream();
            Binary.FastWrite(model, ms);
            Assert.Equal("D2090553746F6E65", ms.ToArray().ToHex());
        }

        private class MyModel
        {
            public Int32 Code { get; set; }

            public String Name { get; set; }
        }
    }
}