using System.ComponentModel;
using System.Data;
using NewLife;
using NewLife.Data;
using Xunit;

namespace XUnitTest.Data;

public class DbTableTests
{
    [Fact]
    public void NomalTest()
    {
        var dt = new DbTable
        {
            Columns = ["Id", "Name", "CreateTime"],
            Rows =
            [
                [123, "Stone", DateTime.Now],
                [456, "NewLife", DateTime.Today]
            ]
        };

        Assert.Equal(123, dt.Get<Int32>(0, "Id"));
        Assert.Equal(456, dt.Get<Int32>(1, "ID"));

        Assert.Equal("NewLife", dt.Get<String>(1, "Name"));
        Assert.Equal(DateTime.Today, dt.Get<DateTime>(1, "CreateTime"));

        // 不存在的字段
        Assert.Equal(DateTime.MinValue, dt.Get<DateTime>(0, "Time"));

        Assert.False(dt.TryGet<DateTime>(1, "Time", out var time));

        var idx = dt.GetColumn("Name");
        Assert.Equal(1, idx);

        idx = dt.GetColumn("Time");
        Assert.Equal(-1, idx);

        // 迭代
        var i = 0;
        foreach (var row in dt)
        {
            if (i == 0)
            {
                Assert.Equal(123, row["ID"]);
                Assert.Equal("Stone", row["name"]);
            }
            else if (i == 1)
            {
                Assert.Equal(456, row["ID"]);
                Assert.Equal("NewLife", row["name"]);
                Assert.Equal(DateTime.Today, row["CreateTime"]);
            }
            i++;
        }
    }

    [Fact]
    public void ToJson()
    {
        var db = new DbTable
        {
            Columns = ["Id", "Name", "CreateTime"],
            Rows =
            [
                [123, "Stone", DateTime.Now],
                [456, "NewLife", DateTime.Today]
            ]
        };

        var json = db.ToJson();
        Assert.NotNull(json);
        Assert.Contains("\"Id\":123", json);
        Assert.Contains("\"Name\":\"Stone\"", json);
        Assert.Contains("\"Id\":456", json);
        Assert.Contains("\"Name\":\"NewLife\"", json);
    }

    [Fact]
    public void ToDictionary()
    {
        var db = new DbTable
        {
            Columns = ["Id", "Name", "CreateTime"],
            Rows =
            [
                [123, "Stone", DateTime.Now],
                [456, "NewLife", DateTime.Today]
            ]
        };

        var list = db.ToDictionary();
        Assert.NotNull(list);
        Assert.Equal(2, list.Count);

        var dic = list[0];
        Assert.Equal(123, dic["Id"]);
        Assert.Equal("Stone", dic["Name"]);
    }

    [Fact]
    public void BinaryTest()
    {
        var file = Path.GetTempFileName();

        var dt = new DbTable
        {
            Columns = ["ID", "Name", "Time"],
            Types = [typeof(Int32), typeof(String), typeof(DateTime)],
            Rows =
            [
                [11, "Stone", DateTime.Now.Trim()],
                [22, "大石头", DateTime.Today],
                [33, "新生命", DateTime.UtcNow.Trim()]
            ]
        };
        dt.SaveFile(file, true);

        Assert.True(File.Exists(file));

        var dt2 = new DbTable();
        dt2.LoadFile(file, true);

        Assert.Equal(3, dt2.Rows.Count);
        for (var i = 0; i < 3; i++)
        {
            var m = dt.Rows[i];
            var n = dt2.Rows[i];
            Assert.Equal(m[0], n[0]);
            Assert.Equal(m[1], n[1]);
            Assert.Equal(m[2], n[2]);
        }
    }

    [Fact]
    public void BinaryVerTest()
    {
        var file = Path.GetTempFileName();

        var dt = new DbTable
        {
            Columns = ["ID", "Name", "Time"],
            Types = [typeof(Int32), typeof(String), typeof(DateTime)],
            Rows =
            [
                [11, "Stone", DateTime.Now.Trim()],
                [22, "大石头", DateTime.Today],
                [33, "新生命", DateTime.UtcNow.Trim()]
            ]
        };
        var pk = dt.ToPacket();

        // 修改版本
        pk[14]++;

        var ex = Assert.Throws<InvalidDataException>(() =>
        {
            var dt2 = new DbTable();
            dt2.Read(pk);
        });

        Assert.Equal("DbTable[ver=3] Unable to support newer versions [4]", ex.Message);
    }

    [Fact]
    public void ModelsTest()
    {
        var list = new List<UserModel>
        {
            new() { ID = 11, Name = "Stone", Time = DateTime.Now },
            new() { ID = 22, Name = "大石头", Time = DateTime.Today },
            new() { ID = 33, Name = "新生命", Time = DateTime.UtcNow }
        };

        var dt = new DbTable();
        dt.WriteModels(list);

        Assert.NotNull(dt.Columns);
        Assert.Equal(3, dt.Columns.Length);
        Assert.Equal(nameof(UserModel.ID), dt.Columns[0]);
        Assert.Equal(nameof(UserModel.Name), dt.Columns[1]);
        Assert.Equal(nameof(UserModel.Time), dt.Columns[2]);

        Assert.NotNull(dt.Types);
        Assert.Equal(3, dt.Types.Length);
        Assert.Equal(typeof(Int32), dt.Types[0]);
        Assert.Equal(typeof(String), dt.Types[1]);
        Assert.Equal(typeof(DateTime), dt.Types[2]);

        Assert.NotNull(dt.Rows);
        Assert.Equal(3, dt.Rows.Count);
        Assert.Equal(11, dt.Rows[0][0]);
        Assert.Equal("大石头", dt.Rows[1][1]);

        var list2 = dt.ReadModels<UserModel2>().ToList();
        Assert.NotNull(list2);
        Assert.Equal(3, list2.Count);
        for (var i = 0; i < list2.Count; i++)
        {
            var m = list[i];
            var n = list2[i];
            Assert.Equal(m.ID, n.ID);
            Assert.Equal(m.Name, n.Name);
            Assert.Equal(m.Time, n.Time);
        }
    }

    private class UserModel
    {
        public Int32 ID { get; set; }

        public String Name { get; set; }

        public DateTime Time { get; set; }
    }

    private class UserModel2
    {
        public Int32 ID { get; set; }

        public String Name { get; set; }

        public DateTime Time { get; set; }
    }

    DataTable GetTable()
    {
        var xml = """
                <NewDataSet>
                  <Table>
                    <ID>1</ID>
                    <Name>管理员</Name>
                    <Enable>true</Enable>
                    <IsSystem>true</IsSystem>
                    <Ex1>0</Ex1>
                    <Ex2>0</Ex2>
                    <Ex3>0</Ex3>
                    <CreateUserID>0</CreateUserID>
                    <CreateTime>2022-04-24T00:04:27+08:00</CreateTime>
                    <UpdateUser />
                    <UpdateUserID>0</UpdateUserID>
                    <UpdateTime>2022-04-24T00:04:27+08:00</UpdateTime>
                    <Remark>默认拥有全部最高权限，由系统工程师使用，安装配置整个系统</Remark>
                  </Table>
                  <Table>
                    <ID>2</ID>
                    <Name>高级用户</Name>
                    <Enable>true</Enable>
                    <IsSystem>false</IsSystem>
                    <Ex1>0</Ex1>
                    <Ex2>0</Ex2>
                    <Ex3>0</Ex3>
                    <CreateUserID>0</CreateUserID>
                    <CreateTime>2022-04-24T00:04:27+08:00</CreateTime>
                    <UpdateUser />
                    <UpdateUserID>0</UpdateUserID>
                    <UpdateTime>2022-04-24T00:04:27+08:00</UpdateTime>
                    <Remark>业务管理人员，可以管理业务模块，可以分配授权用户等级</Remark>
                  </Table>
                  <Table>
                    <ID>3</ID>
                    <Name>普通用户</Name>
                    <Enable>true</Enable>
                    <IsSystem>false</IsSystem>
                    <Ex1>0</Ex1>
                    <Ex2>0</Ex2>
                    <Ex3>0</Ex3>
                    <CreateUserID>0</CreateUserID>
                    <CreateTime>2022-04-24T00:04:27+08:00</CreateTime>
                    <UpdateUser />
                    <UpdateUserID>0</UpdateUserID>
                    <UpdateTime>2022-04-24T00:04:27+08:00</UpdateTime>
                    <Remark>普通业务人员，可以使用系统常规业务模块功能</Remark>
                  </Table>
                  <Table>
                    <ID>4</ID>
                    <Name>游客</Name>
                    <Enable>true</Enable>
                    <IsSystem>false</IsSystem>
                    <Ex1>0</Ex1>
                    <Ex2>0</Ex2>
                    <Ex3>0</Ex3>
                    <CreateUserID>0</CreateUserID>
                    <CreateTime>2022-04-24T00:04:27+08:00</CreateTime>
                    <UpdateUser />
                    <UpdateUserID>0</UpdateUserID>
                    <UpdateTime>2022-04-24T00:04:27+08:00</UpdateTime>
                    <Remark>新注册默认属于游客</Remark>
                  </Table>
                </NewDataSet>
                """;

        var sch = """
                <?xml version="1.0" encoding="utf-16"?>
                <xs:schema id="NewDataSet" xmlns="" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
                  <xs:element name="NewDataSet" msdata:IsDataSet="true" msdata:UseCurrentLocale="true">
                    <xs:complexType>
                      <xs:choice minOccurs="0" maxOccurs="unbounded">
                        <xs:element name="Table">
                          <xs:complexType>
                            <xs:sequence>
                              <xs:element name="ID" type="xs:long" minOccurs="0" />
                              <xs:element name="Name" type="xs:string" minOccurs="0" />
                              <xs:element name="Enable" type="xs:boolean" minOccurs="0" />
                              <xs:element name="IsSystem" type="xs:boolean" minOccurs="0" />
                              <xs:element name="Permission" type="xs:string" minOccurs="0" />
                              <xs:element name="Ex1" type="xs:int" minOccurs="0" />
                              <xs:element name="Ex2" type="xs:int" minOccurs="0" />
                              <xs:element name="Ex3" type="xs:double" minOccurs="0" />
                              <xs:element name="Ex4" type="xs:string" minOccurs="0" />
                              <xs:element name="Ex5" type="xs:string" minOccurs="0" />
                              <xs:element name="Ex6" type="xs:string" minOccurs="0" />
                              <xs:element name="CreateUser" type="xs:string" minOccurs="0" />
                              <xs:element name="CreateUserID" type="xs:int" minOccurs="0" />
                              <xs:element name="CreateIP" type="xs:string" minOccurs="0" />
                              <xs:element name="CreateTime" type="xs:dateTime" minOccurs="0" />
                              <xs:element name="UpdateUser" type="xs:string" minOccurs="0" />
                              <xs:element name="UpdateUserID" type="xs:int" minOccurs="0" />
                              <xs:element name="UpdateIP" type="xs:string" minOccurs="0" />
                              <xs:element name="UpdateTime" type="xs:dateTime" minOccurs="0" />
                              <xs:element name="Remark" type="xs:string" minOccurs="0" />
                            </xs:sequence>
                          </xs:complexType>
                        </xs:element>
                      </xs:choice>
                    </xs:complexType>
                  </xs:element>
                </xs:schema>
                """;

        var ds = new DataSet();
        //ds.ReadXml(xml);
        ds.ReadXmlSchema(new StringReader(sch));

        using var reader = new StringReader(xml);
        ds.ReadXml(reader);

        return ds.Tables[0];
    }

    [Fact]
    public void FromDataTable()
    {
        var table = GetTable();

        var dt = new DbTable();
        var rs = dt.Read(table);

        Assert.Equal(4, rs);
        Assert.Equal(4, dt.Rows.Count);
        Assert.Equal("ID,Name,Enable,IsSystem,Permission,Ex1,Ex2,Ex3,Ex4,Ex5,Ex6,CreateUser,CreateUserID,CreateIP,CreateTime,UpdateUser,UpdateUserID,UpdateIP,UpdateTime,Remark", dt.Columns.Join());
        Assert.Equal(typeof(Int64), dt.Types[0]);
        Assert.Equal(typeof(String), dt.Types[1]);
        Assert.Equal(typeof(Boolean), dt.Types[2]);
        Assert.Equal(typeof(DateTime), dt.Types[14]);

        var row = dt.GetRow(3);
        Assert.Equal(4L, dt.Rows[3][0]);
        Assert.Equal(4L, row["id"]);
        Assert.Equal("游客", row["name"]);
        Assert.Equal(false, row["IsSystem"]);
        Assert.Equal("2022-04-24T00:04:27+08:00".ToDateTime(), row["CreateTime"]);
    }

    [Fact]
    public void ToDataTable()
    {
        var table = GetTable();
        var xml = table.DataSet.GetXml();
        var sch = table.DataSet.GetXmlSchema();

        var dt = new DbTable();
        var rs = dt.Read(table);

        var dt2 = dt.Write(new DataTable("Table"));
        var ds = new DataSet();
        ds.Tables.Add(dt2);
        var xml2 = ds.GetXml();
        var sch2 = ds.GetXmlSchema();

        Assert.Equal(xml, xml2);
        Assert.Equal(sch, sch2);
    }

    [Fact]
    public void GetXml()
    {
        var table = GetTable();

        var dt = new DbTable();
        var rs = dt.Read(table);

        var xml = dt.GetXml();

        //var xml2 = """
        //        <DbTable>
        //          <Table>
        //            <ID>1</ID>
        //            <Name>管理员</Name>
        //            <Enable>true</Enable>
        //            <IsSystem>true</IsSystem>
        //            <Permission></Permission>
        //            <Ex1>0</Ex1>
        //            <Ex2>0</Ex2>
        //            <Ex3>0</Ex3>
        //            <Ex4></Ex4>
        //            <Ex5></Ex5>
        //            <Ex6></Ex6>
        //            <CreateUser></CreateUser>
        //            <CreateUserID>0</CreateUserID>
        //            <CreateIP></CreateIP>
        //            <CreateTime>2022-04-24T00:04:27+08:00</CreateTime>
        //            <UpdateUser></UpdateUser>
        //            <UpdateUserID>0</UpdateUserID>
        //            <UpdateIP></UpdateIP>
        //            <UpdateTime>2022-04-24T00:04:27+08:00</UpdateTime>
        //            <Remark>默认拥有全部最高权限，由系统工程师使用，安装配置整个系统</Remark>
        //          </Table>
        //          <Table>
        //            <ID>2</ID>
        //            <Name>高级用户</Name>
        //            <Enable>true</Enable>
        //            <IsSystem>false</IsSystem>
        //            <Permission></Permission>
        //            <Ex1>0</Ex1>
        //            <Ex2>0</Ex2>
        //            <Ex3>0</Ex3>
        //            <Ex4></Ex4>
        //            <Ex5></Ex5>
        //            <Ex6></Ex6>
        //            <CreateUser></CreateUser>
        //            <CreateUserID>0</CreateUserID>
        //            <CreateIP></CreateIP>
        //            <CreateTime>2022-04-24T00:04:27+08:00</CreateTime>
        //            <UpdateUser></UpdateUser>
        //            <UpdateUserID>0</UpdateUserID>
        //            <UpdateIP></UpdateIP>
        //            <UpdateTime>2022-04-24T00:04:27+08:00</UpdateTime>
        //            <Remark>业务管理人员，可以管理业务模块，可以分配授权用户等级</Remark>
        //          </Table>
        //          <Table>
        //            <ID>3</ID>
        //            <Name>普通用户</Name>
        //            <Enable>true</Enable>
        //            <IsSystem>false</IsSystem>
        //            <Permission></Permission>
        //            <Ex1>0</Ex1>
        //            <Ex2>0</Ex2>
        //            <Ex3>0</Ex3>
        //            <Ex4></Ex4>
        //            <Ex5></Ex5>
        //            <Ex6></Ex6>
        //            <CreateUser></CreateUser>
        //            <CreateUserID>0</CreateUserID>
        //            <CreateIP></CreateIP>
        //            <CreateTime>2022-04-24T00:04:27+08:00</CreateTime>
        //            <UpdateUser></UpdateUser>
        //            <UpdateUserID>0</UpdateUserID>
        //            <UpdateIP></UpdateIP>
        //            <UpdateTime>2022-04-24T00:04:27+08:00</UpdateTime>
        //            <Remark>普通业务人员，可以使用系统常规业务模块功能</Remark>
        //          </Table>
        //          <Table>
        //            <ID>4</ID>
        //            <Name>游客</Name>
        //            <Enable>true</Enable>
        //            <IsSystem>false</IsSystem>
        //            <Permission></Permission>
        //            <Ex1>0</Ex1>
        //            <Ex2>0</Ex2>
        //            <Ex3>0</Ex3>
        //            <Ex4></Ex4>
        //            <Ex5></Ex5>
        //            <Ex6></Ex6>
        //            <CreateUser></CreateUser>
        //            <CreateUserID>0</CreateUserID>
        //            <CreateIP></CreateIP>
        //            <CreateTime>2022-04-24T00:04:27+08:00</CreateTime>
        //            <UpdateUser></UpdateUser>
        //            <UpdateUserID>0</UpdateUserID>
        //            <UpdateIP></UpdateIP>
        //            <UpdateTime>2022-04-24T00:04:27+08:00</UpdateTime>
        //            <Remark>新注册默认属于游客</Remark>
        //          </Table>
        //        </DbTable>
        //        """;
        //Assert.Equal(xml2, xml);
        Assert.Contains("<ID>1</ID>", xml);
        Assert.Contains("<Name>管理员</Name>", xml);
        Assert.Contains("<Remark>业务管理人员，可以管理业务模块，可以分配授权用户等级</Remark>", xml);
    }

    [Fact]
    [DisplayName("ToPacket与Write(Stream)二进制完全一致 — 基础场景")]
    public void BinaryConsistencyTest_Basic()
    {
        var dt = new DbTable
        {
            Columns = ["id", "name"],
            Types = [typeof(Int64), typeof(String)],
            Rows = [[(Int64)1, "test"]],
        };

        // Write(Stream) 路径（Binary）
        var ms1 = new MemoryStream();
        dt.Write(ms1);
        var bytes1 = ms1.ToArray();

        // ToPacket() 路径
        var pk = dt.ToPacket();
        var bytes2 = pk.ReadBytes();

        // 两路径产出完全相同的字节
        Assert.Equal(bytes1.ToHex(), bytes2.ToHex());

        // 两路径均可正确反序列化
        var dt2 = new DbTable();
        dt2.Read(new MemoryStream(bytes1));
        Assert.Single(dt2.Rows);
        Assert.Equal((Int64)1, dt2.Rows[0][0]);
        Assert.Equal("test", dt2.Rows[0][1]);

        var dt3 = new DbTable();
        var pk2 = dt.ToPacket();
        dt3.Read(pk2);
        Assert.Single(dt3.Rows);
        Assert.Equal((Int64)1, dt3.Rows[0][0]);
        Assert.Equal("test", dt3.Rows[0][1]);
    }

    [Fact]
    [DisplayName("ToPacket与Write(Stream)二进制完全一致 — 多类型含DateTime/Decimal")]
    public void BinaryConsistencyTest_MultiTypes()
    {
        var dt = new DbTable
        {
            Columns = ["id", "name", "score", "amount", "active", "ts"],
            Types = [typeof(Int64), typeof(String), typeof(Double), typeof(Decimal), typeof(Boolean), typeof(DateTime)],
            Rows = [
                [(Int64)1001, "张三", 3.14, 99.99m, true, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)],
                [(Int64)1002, "李四", 2.71, 0.01m, false, new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc)],
            ],
        };

        var ms1 = new MemoryStream();
        dt.Write(ms1);
        var bytes1 = ms1.ToArray();

        var pk = dt.ToPacket();
        var bytes2 = pk.ReadBytes();

        Assert.Equal(bytes1.ToHex(), bytes2.ToHex());

        // 反序列化验证数据正确性
        var dt2 = new DbTable();
        dt2.Read(new MemoryStream(bytes1));
        Assert.Equal(2, dt2.Rows.Count);
        Assert.Equal((Int64)1001, dt2.Rows[0][0]);
        Assert.Equal("李四", dt2.Rows[1][1]);
        Assert.Equal(99.99m, dt2.Rows[0][3]);
        Assert.Equal(true, dt2.Rows[0][4]);
    }

    [Fact]
    [DisplayName("ToPacket与Write(Stream)二进制完全一致 — null值列")]
    public void BinaryConsistencyTest_NullValues()
    {
        var dt = new DbTable
        {
            Columns = ["id", "name", "score"],
            Types = [typeof(Int64), typeof(String), typeof(Double)],
            Rows = [
                [(Int64)1, null, (Double)0],
                [(Int64)2, "has_name", 3.14],
            ],
        };

        var ms1 = new MemoryStream();
        dt.Write(ms1);
        var bytes1 = ms1.ToArray();

        var pk = dt.ToPacket();
        var bytes2 = pk.ReadBytes();

        Assert.Equal(bytes1.ToHex(), bytes2.ToHex());
    }

    [Fact]
    [DisplayName("ReadData 从 IDataReader 读取列并映射 DBNull 为类型默认值")]
    public void ReadData_FromReader_MapsColumnsAndDefaults()
    {
        var types = new[]
        {
            typeof(Boolean), typeof(Char), typeof(SByte), typeof(Byte), typeof(Int16), typeof(UInt16),
            typeof(Int32), typeof(UInt32), typeof(Int64), typeof(UInt64), typeof(Single), typeof(Double),
            typeof(Decimal), typeof(DateTime), typeof(String), typeof(Object),
        };
        var names = types.Select((t, i) => $"C{i}_{t.Name}").ToArray();
        var values = types.Select(_ => (Object?)DBNull.Value).ToArray();
        var reader = new FakeDataReader(names, types, values);

        var dt = new DbTable();
        dt.Read(reader);

        Assert.Equal(names, dt.Columns);
        Assert.Equal(types, dt.Types);
        Assert.Single(dt.Rows);
        Assert.Equal(1, dt.Total);

        var row = dt.Rows[0]!;
        Assert.Equal(false, row[0]);
        Assert.Equal((Char)0, row[1]);
        Assert.Equal((SByte)0, row[2]);
        Assert.Equal((Byte)0, row[3]);
        Assert.Equal((Int16)0, row[4]);
        Assert.Equal((UInt16)0, row[5]);
        Assert.Equal(0, row[6]);
        Assert.Equal((UInt32)0, row[7]);
        Assert.Equal(0L, row[8]);
        Assert.Equal((UInt64)0, row[9]);
        Assert.Equal((Single)0, row[10]);
        Assert.Equal((Double)0, row[11]);
        Assert.Equal((Decimal)0, row[12]);
        Assert.Equal(DateTime.MinValue, row[13]);
        Assert.Null(row[14]);
        Assert.Null(row[15]);
    }

    [Fact]
    [DisplayName("ReadData 按 fields 映射读取指定列")]
    public void ReadData_WithFields_MapsProjection()
    {
        var reader = new FakeDataReader(["Id", "Name"], [typeof(Int32), typeof(String)], [7, "Stone"]);

        var dt = new DbTable
        {
            Columns = ["Name", "Id"],
            Types = [typeof(String), typeof(Int32)],
        };
        dt.ReadData(reader, [1, 0]);

        Assert.Single(dt.Rows);
        Assert.Equal("Stone", dt.Rows[0]![0]);
        Assert.Equal(7, dt.Rows[0]![1]);
        Assert.Equal(1, dt.Total);
    }

    [Fact]
    [DisplayName("Csv 保存与加载往返保留列名与文本值")]
    public void Csv_RoundTrip()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dbtable_csv_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var file = Path.Combine(dir, "data.csv");
            var dt = new DbTable
            {
                Columns = ["Id", "Name", "Remark"],
                Rows =
                [
                    [1, "Stone", "含,逗号"],
                    [2, "大石头", "含\"引号\""],
                    [3, "新生命", "含\n换行"],
                ],
            };

            dt.SaveCsv(file);

            var dt2 = new DbTable();
            dt2.LoadCsv(file);

            Assert.Equal(dt.Columns, dt2.Columns);
            Assert.Equal(dt.Rows.Count, dt2.Rows.Count);
            for (var i = 0; i < dt.Rows.Count; i++)
            {
                Assert.Equal(dt.Rows[i][0] + "", dt2.Rows[i][0]);
                Assert.Equal(dt.Rows[i][1], dt2.Rows[i][1]);
                Assert.Equal(dt.Rows[i][2], dt2.Rows[i][2]);
            }
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    [DisplayName("Binary 文件往返保留列类型与多类型数据")]
    public void Binary_File_RoundTrip()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dbtable_bin_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var file = Path.Combine(dir, "data.bin");
            var time = new DateTime(2025, 1, 2, 3, 4, 5, 678);
            var dt = new DbTable
            {
                Columns = ["Id", "Name", "Amount", "Enable", "Time"],
                Types = [typeof(Int32), typeof(String), typeof(Decimal), typeof(Boolean), typeof(DateTime)],
                Rows =
                [
                    [11, "Stone", 12.34m, true, time],
                    [22, null, 0m, false, DateTime.MinValue],
                ],
            };

            dt.SaveFile(file, false);

            var dt2 = new DbTable();
            dt2.LoadFile(file, false);

            Assert.Equal(dt.Columns, dt2.Columns);
            Assert.Equal(dt.Types, dt2.Types);
            Assert.Equal(dt.Rows.Count, dt2.Rows.Count);
            Assert.Equal(11, dt2.Rows[0][0]);
            Assert.Equal("Stone", dt2.Rows[0][1]);
            Assert.Equal(12.34m, dt2.Rows[0][2]);
            Assert.Equal(true, dt2.Rows[0][3]);
            Assert.Equal(time, dt2.Rows[0][4]);
            // Binary 对 null 字符串按空串写入，读取后为 ""
            Assert.Equal("", dt2.Rows[1][1]);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    [DisplayName("Clone 复制列与行列表但按文档共享行数组")]
    public void Clone_CopiesStructure()
    {
        var dt = new DbTable
        {
            Columns = ["Id", "Name"],
            Types = [typeof(Int32), typeof(String)],
            Rows = [[1, "A"], [2, "B"]],
            Total = 2,
        };

        var clone = dt.Clone();

        Assert.NotSame(dt.Columns, clone.Columns);
        Assert.NotSame(dt.Types, clone.Types);
        Assert.NotSame(dt.Rows, clone.Rows);
        // 文档约定：行数组引用共享
        Assert.Same(dt.Rows[0], clone.Rows[0]);

        clone.Columns[0] = "Changed";
        clone.Types[0] = typeof(String);
        clone.Rows.RemoveAt(0);
        clone.Rows.Add([3, "C"]);

        Assert.Equal("Id", dt.Columns[0]);
        Assert.Equal(typeof(Int32), dt.Types[0]);
        Assert.Equal(2, dt.Rows.Count);
        Assert.Equal(1, dt.Rows[0][0]);
        Assert.Equal(2, dt.Rows[1][0]);

        var clone2 = ((ICloneable)dt).Clone();
        Assert.IsType<DbTable>(clone2);
    }

    /// <summary>最小内存行集 IDataReader，用于驱动 DbTable 的读取路径</summary>
    private sealed class FakeDataReader : IDataReader
    {
        private readonly String[] _names;
        private readonly Type[] _types;
        private readonly List<Object?[]> _rows;
        private Int32 _index = -1;

        public FakeDataReader(String[] names, Type[] types, params Object?[][] rows)
        {
            _names = names;
            _types = types;
            _rows = [.. rows];
        }

        public Int32 FieldCount => _names.Length;
        public Int32 Depth => 0;
        public Boolean IsClosed => false;
        public Int32 RecordsAffected => -1;

        public Object this[Int32 i] => _rows[_index][i] ?? DBNull.Value;
        public Object this[String name] => this[GetOrdinal(name)];

        public Boolean Read() => ++_index < _rows.Count;
        public Boolean NextResult() => false;
        public void Close() { }
        public void Dispose() { }

        public String GetName(Int32 i) => _names[i];
        public Type GetFieldType(Int32 i) => _types[i];
        public String GetDataTypeName(Int32 i) => _types[i].Name;

        public Int32 GetOrdinal(String name)
        {
            var index = Array.FindIndex(_names, e => String.Equals(e, name, StringComparison.OrdinalIgnoreCase));
            if (index < 0) throw new IndexOutOfRangeException(name);

            return index;
        }

        public Object GetValue(Int32 i) => this[i];
        public Boolean IsDBNull(Int32 i) => this[i] is DBNull;

        public Int32 GetValues(Object[] values)
        {
            var count = Math.Min(values.Length, FieldCount);
            for (var i = 0; i < count; i++)
            {
                values[i] = this[i];
            }

            return count;
        }

        public DataTable? GetSchemaTable() => null;
        public IDataReader GetData(Int32 i) => throw new NotSupportedException();

        public Boolean GetBoolean(Int32 i) => Convert.ToBoolean(this[i]);
        public Byte GetByte(Int32 i) => Convert.ToByte(this[i]);
        public Char GetChar(Int32 i) => Convert.ToChar(this[i]);
        public DateTime GetDateTime(Int32 i) => Convert.ToDateTime(this[i]);
        public Decimal GetDecimal(Int32 i) => Convert.ToDecimal(this[i]);
        public Double GetDouble(Int32 i) => Convert.ToDouble(this[i]);
        public Single GetFloat(Int32 i) => Convert.ToSingle(this[i]);
        public Guid GetGuid(Int32 i) => (Guid)this[i];
        public Int16 GetInt16(Int32 i) => Convert.ToInt16(this[i]);
        public Int32 GetInt32(Int32 i) => Convert.ToInt32(this[i]);
        public Int64 GetInt64(Int32 i) => Convert.ToInt64(this[i]);
        public String GetString(Int32 i) => Convert.ToString(this[i])!;

        public Int64 GetBytes(Int32 i, Int64 fieldOffset, Byte[]? buffer, Int32 bufferoffset, Int32 length) => throw new NotSupportedException();
        public Int64 GetChars(Int32 i, Int64 fieldoffset, Char[]? buffer, Int32 bufferoffset, Int32 length) => throw new NotSupportedException();
    }
}