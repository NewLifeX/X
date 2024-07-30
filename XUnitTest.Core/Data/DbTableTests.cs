using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NewLife.Data;
using Xunit;
using NewLife;
using System.Data;

namespace XUnitTest.Data;

public class DbTableTests
{
    [Fact]
    public void NomalTest()
    {
        var dt = new DbTable
        {
            Columns = new[] { "Id", "Name", "CreateTime" },
            Rows = new List<Object[]>
            {
                new Object[] { 123, "Stone", DateTime.Now },
                new Object[] { 456, "NewLife", DateTime.Today }
            }
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
            Columns = new[] { "Id", "Name", "CreateTime" },
            Rows = new List<Object[]>
            {
                new Object[] { 123, "Stone", DateTime.Now },
                new Object[] { 456, "NewLife", DateTime.Today }
            }
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
            Columns = new[] { "Id", "Name", "CreateTime" },
            Rows = new List<Object[]>
            {
                new Object[] { 123, "Stone", DateTime.Now },
                new Object[] { 456, "NewLife", DateTime.Today }
            }
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
            Columns = new[] { "ID", "Name", "Time" },
            Types = new[] { typeof(Int32), typeof(String), typeof(DateTime) },
            Rows = new List<Object[]>
            {
                new Object[] { 11, "Stone", DateTime.Now.Trim() },
                new Object[] { 22, "大石头", DateTime.Today },
                new Object[] { 33, "新生命", DateTime.UtcNow.Trim() }
            }
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
            Columns = new[] { "ID", "Name", "Time" },
            Types = new[] { typeof(Int32), typeof(String), typeof(DateTime) },
            Rows = new List<Object[]>
            {
                new Object[] { 11, "Stone", DateTime.Now.Trim() },
                new Object[] { 22, "大石头", DateTime.Today },
                new Object[] { 33, "新生命", DateTime.UtcNow.Trim() }
            }
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
}