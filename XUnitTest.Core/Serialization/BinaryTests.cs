using System;
using System.IO;
using NewLife;
using NewLife.Data;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Serialization
{
    public class BinaryTests
    {
        [Fact]
        public void Normal()
        {
            var model = new MyModel { Code = 1234, Name = "Stone" };
            var bn = new Binary();
            Assert.Equal(5, bn.Handlers.Count);

            bn.Write(model);

            var pk = bn.GetPacket();
            Assert.Equal(10, pk.Total);
            Assert.Equal("000004D20553746F6E65", pk.ToHex());

            var bn2 = new Binary { Stream = pk.GetStream() };
            var model2 = bn2.Read<MyModel>();
            Assert.Equal(model.Code, model2.Code);
            Assert.Equal(model.Name, model2.Name);
        }

        [Fact]
        public void EncodeInt()
        {
            var model = new MyModel { Code = 1234, Name = "Stone" };
            var bn = new Binary { EncodeInt = true, };
            Assert.Equal(5, bn.Handlers.Count);

            bn.Write(model);

            var pk = bn.GetPacket();
            Assert.Equal(8, pk.Total);
            Assert.Equal("D2090553746F6E65", pk.ToHex());

            var bn2 = new Binary { Stream = pk.GetStream(), EncodeInt = true };
            var model2 = bn2.Read<MyModel>();
            Assert.Equal(model.Code, model2.Code);
            Assert.Equal(model.Name, model2.Name);
        }

        [Fact]
        public void IsLittleEndian()
        {
            var model = new MyModel { Code = 1234, Name = "Stone" };
            var bn = new Binary { IsLittleEndian = true, };
            Assert.Equal(5, bn.Handlers.Count);

            bn.Write(model);

            var pk = bn.GetPacket();
            Assert.Equal(10, pk.Total);
            Assert.Equal("D20400000553746F6E65", pk.ToHex());

            var bn2 = new Binary { Stream = pk.GetStream(), IsLittleEndian = true };
            var model2 = bn2.Read<MyModel>();
            Assert.Equal(model.Code, model2.Code);
            Assert.Equal(model.Name, model2.Name);
        }

        [Fact]
        public void UseFieldSize()
        {
            var model = new MyModelWithFieldSize { Name = "Stone" };
            var bn = new Binary { UseFieldSize = true, };
            Assert.Equal(5, bn.Handlers.Count);

            bn.Write(model);

            var pk = bn.GetPacket();
            Assert.Equal(6, pk.Total);
            Assert.Equal("0553746F6E65", pk.ToHex());

            var bn2 = new Binary { Stream = pk.GetStream(), UseFieldSize = true };
            var model2 = bn2.Read<MyModelWithFieldSize>();
            Assert.Equal(model.Length, model2.Length);
            Assert.Equal(model.Name, model2.Name);
        }

        [Fact]
        public void SizeWidth()
        {
            var model = new MyModel { Code = 1234, Name = "Stone" };
            var bn = new Binary { SizeWidth = 2, };
            Assert.Equal(5, bn.Handlers.Count);

            bn.Write(model);

            var pk = bn.GetPacket();
            Assert.Equal(11, pk.Total);
            Assert.Equal("000004D2000553746F6E65", pk.ToHex());

            var bn2 = new Binary { Stream = pk.GetStream(), SizeWidth = 2 };
            var model2 = bn2.Read<MyModel>();
            Assert.Equal(model.Code, model2.Code);
            Assert.Equal(model.Name, model2.Name);
        }

        [Fact]
        public void UseProperty()
        {
            var model = new MyModel { Code = 1234, Name = "Stone" };
            var bn = new Binary { UseProperty = false, };
            Assert.Equal(5, bn.Handlers.Count);

            bn.Write(model);

            var pk = bn.GetPacket();
            Assert.Equal(10, pk.Total);
            Assert.Equal("000004D20553746F6E65", pk.ToHex());

            var bn2 = new Binary { Stream = pk.GetStream(), UseProperty = false };
            var model2 = bn2.Read<MyModel>();
            Assert.Equal(model.Code, model2.Code);
            Assert.Equal(model.Name, model2.Name);
        }

        [Fact]
        public void IgnoreMembers()
        {
            var model = new MyModel { Code = 1234, Name = "Stone" };
            var bn = new Binary();
            bn.IgnoreMembers.Add("Code");
            Assert.Equal(5, bn.Handlers.Count);

            bn.Write(model);

            var pk = bn.GetPacket();
            Assert.Equal(6, pk.Total);
            Assert.Equal("0553746F6E65", pk.ToHex());

            var bn2 = new Binary { Stream = pk.GetStream() };
            bn2.IgnoreMembers.Add("Code");
            var model2 = bn2.Read<MyModel>();
            Assert.Equal(0, model2.Code);
            Assert.Equal(model.Name, model2.Name);
        }

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

        [Fact]
        public void Accessor()
        {
            var model = new MyModelWithAccessor { Code = 1234, Name = "Stone" };
            var pk = Binary.FastWrite(model);
            Assert.Equal(10, pk.Total);
            Assert.Equal("D20400000553746F6E65", pk.ToHex());
            Assert.Equal("0gQAAAVTdG9uZQ==", pk.ToArray().ToBase64());

            var model2 = Binary.FastRead<MyModelWithAccessor>(pk.GetStream());
            Assert.Equal(model.Code, model2.Code);
            Assert.Equal(model.Name, model2.Name);

            var ms = new MemoryStream();
            Binary.FastWrite(model, ms);
            Assert.Equal("D20400000553746F6E65", ms.ToArray().ToHex());
        }

        private class MyModelWithAccessor : IAccessor
        {
            public Int32 Code { get; set; }

            public String Name { get; set; }

            public Boolean Read(Stream stream, Object context)
            {
                var reader = new BinaryReader(stream);
                Code = reader.ReadInt32();
                Name = reader.ReadString();

                return true;
            }

            public Boolean Write(Stream stream, Object context)
            {
                var writer = new BinaryWriter(stream);
                writer.Write(Code);
                writer.Write(Name);

                return true;
            }
        }

        [Fact]
        public void MemberAccessor()
        {
            var model = new MyModelWithMemberAccessor { Code = 1234, Name = "Stone" };
            var pk = Binary.FastWrite(model);
            Assert.Equal(8, pk.Total);
            Assert.Equal("D2090553746F6E65", pk.ToHex());
            Assert.Equal("0gkFU3RvbmU=", pk.ToArray().ToBase64());

            var model2 = Binary.FastRead<MyModelWithMemberAccessor>(pk.GetStream());
            Assert.Equal(model.Code, model2.Code);
            Assert.Equal(model.Name, model2.Name);

            var ms = new MemoryStream();
            Binary.FastWrite(model, ms);
            Assert.Equal("D2090553746F6E65", ms.ToArray().ToHex());
        }

        private class MyModelWithMemberAccessor : IMemberAccessor
        {
            public Int32 Code { get; set; }

            public String Name { get; set; }

            public Boolean Read(IFormatterX formatter, AccessorContext context)
            {
                var bn = formatter as Binary;

                switch (context.Member.Name)
                {
                    case "Code": Code = bn.Read<Int32>(); break;
                    case "Name": Name = bn.Read<String>(); break;
                }
                //Code = bn.Read<Int32>();
                //Name = bn.Read<String>();

                return true;
            }
            public Boolean Write(IFormatterX formatter, AccessorContext context)
            {
                var bn = formatter as Binary;

                switch (context.Member.Name)
                {
                    case "Code": bn.Write(Code); break;
                    case "Name": bn.Write(Name); break;
                }

                return true;
            }
        }

        [Fact]
        public void FieldSize()
        {
            var model = new MyModelWithFieldSize { Name = "Stone" };
            Assert.Equal(0, model.Length);

            var bn = new Binary { EncodeInt = true, UseFieldSize = true };
            bn.Write(model);
            var pk = new Packet(bn.GetBytes());
            Assert.Equal(5, model.Length);
            Assert.Equal(6, pk.Total);
            Assert.Equal("0553746F6E65", pk.ToHex());

            var bn2 = new Binary { Stream = pk.GetStream(), EncodeInt = true, UseFieldSize = true };
            var model2 = bn2.Read<MyModelWithFieldSize>();
            Assert.Equal(model.Length, model2.Length);
            Assert.Equal(model.Name, model2.Name);
        }

        private class MyModelWithFieldSize
        {
            public Byte Length { get; set; }

            [FieldSize(nameof(Length))]
            public String Name { get; set; }
        }

        [Fact]
        public void MemberAccessorAtt()
        {
            var model = new MyModelWithMemberAccessorAtt { Code = 1234, Name = "Stone" };
            var pk = Binary.FastWrite(model);
            Assert.Equal(8, pk.Total);
            Assert.Equal("D2090553746F6E65", pk.ToHex());

            var model2 = Binary.FastRead<MyModelWithMemberAccessorAtt>(pk.GetStream());
            Assert.Equal(model.Code, model2.Code);
            Assert.Equal(model.Name, model2.Name);
        }

        private class MyModelWithMemberAccessorAtt
        {
            public Int32 Code { get; set; }

            [MemberAccessor(typeof(MyAccessor))]
            public String Name { get; set; }
        }

        private class MyAccessor : IMemberAccessor
        {
            public Boolean Read(IFormatterX formatter, AccessorContext context)
            {
                Assert.Equal("Name", context.Member.Name);

                var v = context.Value as MyModelWithMemberAccessorAtt;

                var bn = formatter as Binary;
                v.Name = bn.Read<String>();

                return true;
            }

            public Boolean Write(IFormatterX formatter, AccessorContext context)
            {
                Assert.Equal("Name", context.Member.Name);

                var v = context.Value as MyModelWithMemberAccessorAtt;

                var bn = formatter as Binary;
                bn.Write(v.Name);

                return true;
            }
        }
    }
}