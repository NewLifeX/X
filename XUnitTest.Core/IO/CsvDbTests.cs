using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NewLife;
using NewLife.Data;
using NewLife.IO;
using NewLife.Security;
using Xunit;

namespace XUnitTest.IO;

public class CsvDbTests
{
    protected virtual CsvDb<GeoArea> GetDb(String name)
    {
        var file = $"data/{name}.csv".GetFullPath();
        if (File.Exists(file)) File.Delete(file);

        var db = new CsvDb<GeoArea>((x, y) => x.Code == y.Code)
        {
            FileName = file
        };
        return db;
    }

    private GeoArea GetModel()
    {
        var model = new GeoArea
        {
            Code = Rand.Next(),
            Name = Rand.NextString(14),
        };

        return model;
    }

    private String[] GetHeaders()
    {
        var pis = typeof(GeoArea).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return pis.Select(e => e.Name).ToArray();
    }

    private Object[] GetValue(GeoArea model)
    {
        var pis = typeof(GeoArea).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        //return pis.Select(e => e.GetValue(model, null)).ToArray();
        var arr = new Object[pis.Length];
        for (var i = 0; i < pis.Length; i++)
        {
            arr[i] = pis[i].GetValue(model, null);
            if (pis[i].PropertyType == typeof(Boolean))
                arr[i] = (Boolean)arr[i] ? "1" : "0";
            else if (pis[i].Name == "Code" && arr[i].ToString().Length > 9)
                arr[i] = "\t" + arr[i];
        }
        return arr;
    }

    [Fact]
    public void InsertTest()
    {
        var db = GetDb("Insert");

        var model = GetModel();
        db.Add(model);

        db.Dispose();

        // 把文件读出来
        var lines = File.ReadAllLines(db.FileName.GetFullPath());
        Assert.Equal(2, lines.Length);

        Assert.Equal(GetHeaders().Join(","), lines[0]);
        Assert.Equal(GetValue(model).Join(","), lines[1]);
    }

    [Fact]
    public void InsertsTest()
    {
        var db = GetDb("Inserts");

        var list = new List<GeoArea>();
        var count = Rand.Next(2, 100);
        for (var i = 0; i < count; i++)
        {
            list.Add(GetModel());
        }

        db.Add(list);

        db.Dispose();

        // 把文件读出来
        var lines = File.ReadAllLines(db.FileName.GetFullPath());
        Assert.Equal(list.Count + 1, lines.Length);

        Assert.Equal(GetHeaders().Join(","), lines[0]);
        for (var i = 0; i < list.Count; i++)
        {
            Assert.Equal(GetValue(list[i]).Join(","), lines[i + 1]);
        }
    }

    [Fact]
    public void GetAllTest()
    {
        var db = GetDb("GetAll");

        var list = new List<GeoArea>();
        var count = Rand.Next(2, 100);
        for (var i = 0; i < count; i++)
        {
            list.Add(GetModel());
        }

        db.Add(list);

        // 把文件读出来
        var list2 = db.FindAll();
        Assert.Equal(list.Count, list2.Count);

        for (var i = 0; i < list.Count; i++)
        {
            Assert.Equal(GetValue(list[i]).Join(","), GetValue(list2[i]).Join(","));
        }

        // 高级查找
        var list3 = db.Query(e => e.Code is >= 100 and < 1000);
        var list4 = list.Where(e => e.Code is >= 100 and < 1000).ToList();
        Assert.Equal(list4.Select(e => e.Code), list3.Select(e => e.Code));
    }

    [Fact]
    public void GetCountTest()
    {
        var db = GetDb("GetCount");

        var list = new List<GeoArea>();
        var count = Rand.Next(2, 100);
        for (var i = 0; i < count; i++)
        {
            list.Add(GetModel());
        }

        db.Add(list);

        db.Dispose();

        // 把文件读出来
        var lines = File.ReadAllLines(db.FileName.GetFullPath());
        Assert.Equal(list.Count + 1, lines.Length);
        Assert.Equal(list.Count, db.FindCount());
    }

    [Fact]
    public void LargeInsertsTest()
    {
        var db = GetDb("LargeInserts");

        var list = new List<GeoArea>();
        var count = 100_000;
        for (var i = 0; i < count; i++)
        {
            list.Add(GetModel());
        }

        db.Add(list);

        db.Dispose();

        // 把文件读出来
        var lines = File.ReadAllLines(db.FileName.GetFullPath());
        Assert.Equal(list.Count + 1, lines.Length);

        Assert.Equal(GetHeaders().Join(","), lines[0]);
        for (var i = 0; i < list.Count; i++)
        {
            Assert.Equal(GetValue(list[i]).Join(","), lines[i + 1]);
        }
    }

    [Fact]
    public void InsertTwoTimesTest()
    {
        var db = GetDb("InsertTwoTimes");

        // 第一次插入
        var list = new List<GeoArea>();
        {
            var count = Rand.Next(2, 100);
            for (var i = 0; i < count; i++)
            {
                list.Add(GetModel());
            }

            db.Add(list);
        }

        // 第二次插入
        {
            var list2 = new List<GeoArea>();
            var count = Rand.Next(2, 100);
            for (var i = 0; i < count; i++)
            {
                list2.Add(GetModel());
            }

            db.Add(list2);

            list.AddRange(list2);
        }

        db.Dispose();

        // 把文件读出来
        var lines = File.ReadAllLines(db.FileName.GetFullPath());
        Assert.Equal(list.Count + 1, lines.Length);

        Assert.Equal(GetHeaders().Join(","), lines[0]);
        for (var i = 0; i < list.Count; i++)
        {
            Assert.Equal(GetValue(list[i]).Join(","), lines[i + 1]);
        }
    }

    [Fact]
    public void DeletesTest()
    {
        var db = GetDb("Deletes");

        var list = new List<GeoArea>();
        var count = Rand.Next(2, 100);
        for (var i = 0; i < count; i++)
        {
            list.Add(GetModel());
        }

        db.Add(list);

        // 随机删除一个
        var idx = Rand.Next(list.Count);
        var rs = db.Remove(list[idx]);
        Assert.Equal(1, rs);

        list.RemoveAt(idx);
        Assert.Equal(list.Count, db.FindCount());

        // 随机抽几个，删除
        var list2 = new List<GeoArea>();
        for (var i = 0; i < list.Count; i++)
        {
            if (Rand.Next(2) == 1) list2.Add(list[i]);
        }

        var rs2 = db.Remove(list2);
        Assert.Equal(list2.Count, rs2);
        Assert.Equal(list.Count - list2.Count, db.FindCount());
    }

    [Fact]
    public void UpdateTest()
    {
        var db = GetDb("Update");

        var list = new List<GeoArea>();
        var count = Rand.Next(2, 100);
        for (var i = 0; i < count; i++)
        {
            list.Add(GetModel());
        }

        db.Add(list);

        // 随机改一个
        var idx = Rand.Next(list.Count);
        var model = db.Find(list[idx]);
        Assert.NotNull(model);

        model.ParentCode = Rand.Next();
        var rs = db.Update(model);
        Assert.True(rs);

        var model2 = db.Find(list[idx]);
        Assert.NotNull(model2);
        Assert.Equal(model.ParentCode, model2.ParentCode);
    }

    [Fact]
    public void WriteTest()
    {
        var db = GetDb("Write");

        var list = new List<GeoArea>();
        var count = Rand.Next(2, 100);
        for (var i = 0; i < count; i++)
        {
            list.Add(GetModel());
        }

        db.Add(list);

        // 再次覆盖写入
        list.Clear();
        for (var i = 0; i < 10; i++)
        {
            list.Add(GetModel());
        }
        db.Write(list, false);

        // 把文件读出来
        var lines = File.ReadAllLines(db.FileName.GetFullPath());
        Assert.Equal(list.Count + 1, lines.Length);
    }

    [Fact]
    public void ClearTest()
    {
        var db = GetDb("Clear");

        var list = new List<GeoArea>();
        var count = Rand.Next(2, 100);
        for (var i = 0; i < count; i++)
        {
            list.Add(GetModel());
        }

        db.Add(list);

        // 清空
        db.Clear();

        db.Dispose();

        // 把文件读出来
        var lines = File.ReadAllLines(db.FileName.GetFullPath());
        Assert.Single(lines);
    }

    [Fact]
    public void SetTest()
    {
        var db = GetDb("Set");

        var list = new List<GeoArea>();
        var count = Rand.Next(2, 100);
        for (var i = 0; i < count; i++)
        {
            list.Add(GetModel());
        }

        db.Add(list);

        // 设置新的
        var model = GetModel();
        db.Set(model);

        db.Dispose();

        // 把文件读出来
        var lines = File.ReadAllLines(db.FileName.GetFullPath());
        Assert.Equal(list.Count + 1 + 1, lines.Length);
    }
}

public class CsvDbWithTransactionTests : CsvDbTests
{
    protected override CsvDb<GeoArea> GetDb(String name)
    {
        name += "2";
        var db = base.GetDb(name);
        db.BeginTransaction();

        return db;
    }
}