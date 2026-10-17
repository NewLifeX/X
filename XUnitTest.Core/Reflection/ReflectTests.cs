using System;
using System.Collections;
using System.Collections.Generic;
using NewLife;
using NewLife.Data;
using NewLife.IoT.ThingModels;
using NewLife.Reflection;
using Xunit;

namespace XUnitTest.Reflection;

public class ReflectTests
{
    [Theory]
    [InlineData(typeof(Boolean))]
    [InlineData(typeof(Char))]
    [InlineData(typeof(SByte))]
    [InlineData(typeof(Byte))]
    [InlineData(typeof(Int16))]
    [InlineData(typeof(UInt16))]
    [InlineData(typeof(Int32))]
    [InlineData(typeof(UInt32))]
    [InlineData(typeof(Int64))]
    [InlineData(typeof(UInt64))]
    [InlineData(typeof(Single))]
    [InlineData(typeof(Double))]
    [InlineData(typeof(Decimal))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(String))]
    [InlineData(typeof(Guid))]
    [InlineData(typeof(Byte[]))]
    public void GetTypeExTest(Type type)
    {
        var name = type.Name;
        var t2 = name.GetTypeEx();
        Assert.Equal(type, t2);
    }

    [Theory]
    [InlineData(typeof(Boolean), TypeCode.Boolean)]
    [InlineData(typeof(Char), TypeCode.Char)]
    [InlineData(typeof(SByte), TypeCode.SByte)]
    [InlineData(typeof(Byte), TypeCode.Byte)]
    [InlineData(typeof(Int16), TypeCode.Int16)]
    [InlineData(typeof(UInt16), TypeCode.UInt16)]
    [InlineData(typeof(Int32), TypeCode.Int32)]
    [InlineData(typeof(UInt32), TypeCode.UInt32)]
    [InlineData(typeof(Int64), TypeCode.Int64)]
    [InlineData(typeof(UInt64), TypeCode.UInt64)]
    [InlineData(typeof(Single), TypeCode.Single)]
    [InlineData(typeof(Double), TypeCode.Double)]
    [InlineData(typeof(Decimal), TypeCode.Decimal)]
    [InlineData(typeof(DateTime), TypeCode.DateTime)]
    [InlineData(typeof(String), TypeCode.String)]
    [InlineData(typeof(Guid), TypeCode.Object)]
    [InlineData(typeof(Byte[]), TypeCode.Object)]
    [InlineData(typeof(TimeSpan), TypeCode.Object)]
    [InlineData(typeof(Enum), TypeCode.Object)]
    [InlineData(typeof(ServiceStatus), TypeCode.Int32)]
    public void GetTypeCode(Type type, TypeCode code)
    {
        Assert.Equal(code, Type.GetTypeCode(type));
    }

    [Theory]
    [InlineData(typeof(Boolean))]
    [InlineData(typeof(Char))]
    [InlineData(typeof(SByte))]
    [InlineData(typeof(Byte))]
    [InlineData(typeof(Int16))]
    [InlineData(typeof(UInt16))]
    [InlineData(typeof(Int32))]
    [InlineData(typeof(UInt32))]
    [InlineData(typeof(Int64))]
    [InlineData(typeof(UInt64))]
    [InlineData(typeof(Single))]
    [InlineData(typeof(Double))]
    [InlineData(typeof(Decimal))]
    [InlineData(typeof(DateTime))]
    //[InlineData(typeof(String))]
    [InlineData(typeof(Guid))]
    //[InlineData(typeof(Byte[]))]
    [InlineData(typeof(TimeSpan))]
    //[InlineData(typeof(Enum))]
    [InlineData(typeof(ServiceStatus))]
    public void IsValueType(Type type)
    {
        Assert.True(type.IsValueType);
    }

    [Theory]
    [InlineData(typeof(Boolean))]
    [InlineData(typeof(Char))]
    [InlineData(typeof(SByte))]
    [InlineData(typeof(Byte))]
    [InlineData(typeof(Int16))]
    [InlineData(typeof(UInt16))]
    [InlineData(typeof(Int32))]
    [InlineData(typeof(UInt32))]
    [InlineData(typeof(Int64))]
    [InlineData(typeof(UInt64))]
    [InlineData(typeof(Single))]
    [InlineData(typeof(Double))]
    //[InlineData(typeof(Decimal))]
    //[InlineData(typeof(DateTime))]
    //[InlineData(typeof(String))]
    //[InlineData(typeof(Guid))]
    //[InlineData(typeof(Byte[]))]
    //[InlineData(typeof(TimeSpan))]
    //[InlineData(typeof(Enum))]
    //[InlineData(typeof(ServiceStatus))]
    public void IsPrimitive(Type type)
    {
        Assert.True(type.IsPrimitive);
    }

    [Theory]
    [InlineData(typeof(Boolean))]
    [InlineData(typeof(Char))]
    [InlineData(typeof(SByte))]
    [InlineData(typeof(Byte))]
    [InlineData(typeof(Int16))]
    [InlineData(typeof(UInt16))]
    [InlineData(typeof(Int32))]
    [InlineData(typeof(UInt32))]
    [InlineData(typeof(Int64))]
    [InlineData(typeof(UInt64))]
    [InlineData(typeof(Single))]
    [InlineData(typeof(Double))]
    [InlineData(typeof(Decimal))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(String))]
    [InlineData(typeof(Boolean?))]
    [InlineData(typeof(Char?))]
    [InlineData(typeof(SByte?))]
    [InlineData(typeof(Byte?))]
    [InlineData(typeof(Int16?))]
    [InlineData(typeof(UInt16?))]
    [InlineData(typeof(Int32?))]
    [InlineData(typeof(UInt32?))]
    [InlineData(typeof(Int64?))]
    [InlineData(typeof(UInt64?))]
    [InlineData(typeof(Single?))]
    [InlineData(typeof(Double?))]
    [InlineData(typeof(Decimal?))]
    [InlineData(typeof(DateTime?))]
    //[InlineData(typeof(Guid))]
    //[InlineData(typeof(Byte[]))]
    //[InlineData(typeof(TimeSpan))]
    //[InlineData(typeof(Enum))]
    [InlineData(typeof(ServiceStatus))]
    [InlineData(typeof(ServiceStatus?))]
    public void IsBaseType(Type type)
    {
        Assert.True(type.IsBaseType());
    }

    [Theory]
    [InlineData(typeof(Boolean?))]
    [InlineData(typeof(Char?))]
    [InlineData(typeof(SByte?))]
    [InlineData(typeof(Byte?))]
    [InlineData(typeof(Int16?))]
    [InlineData(typeof(UInt16?))]
    [InlineData(typeof(Int32?))]
    [InlineData(typeof(UInt32?))]
    [InlineData(typeof(Int64?))]
    [InlineData(typeof(UInt64?))]
    [InlineData(typeof(Single?))]
    [InlineData(typeof(Double?))]
    [InlineData(typeof(Decimal?))]
    [InlineData(typeof(DateTime?))]
    //[InlineData(typeof(Guid))]
    //[InlineData(typeof(Byte[]))]
    //[InlineData(typeof(TimeSpan))]
    //[InlineData(typeof(Enum))]
    //[InlineData(typeof(ServiceStatus))]
    [InlineData(typeof(ServiceStatus?))]
    public void IsNullable(Type type)
    {
        Assert.True(type.IsNullable());
    }

    [Theory]
    [InlineData(typeof(Boolean), false)]
    [InlineData(typeof(Char), false)]
    [InlineData(typeof(SByte), true)]
    [InlineData(typeof(Byte), true)]
    [InlineData(typeof(Int16), true)]
    [InlineData(typeof(UInt16), true)]
    [InlineData(typeof(Int32), true)]
    [InlineData(typeof(UInt32), true)]
    [InlineData(typeof(Int64), true)]
    [InlineData(typeof(UInt64), true)]
    [InlineData(typeof(Single), false)]
    [InlineData(typeof(Double), false)]
    [InlineData(typeof(Decimal), false)]
    [InlineData(typeof(DateTime), false)]
    [InlineData(typeof(String), false)]
    [InlineData(typeof(Boolean?), false)]
    [InlineData(typeof(Char?), false)]
    [InlineData(typeof(SByte?), true)]
    [InlineData(typeof(Byte?), true)]
    [InlineData(typeof(Int16?), true)]
    [InlineData(typeof(UInt16?), true)]
    [InlineData(typeof(Int32?), true)]
    [InlineData(typeof(UInt32?), true)]
    [InlineData(typeof(Int64?), true)]
    [InlineData(typeof(UInt64?), true)]
    [InlineData(typeof(Single?), false)]
    [InlineData(typeof(Double?), false)]
    [InlineData(typeof(Decimal?), false)]
    [InlineData(typeof(DateTime?), false)]
    [InlineData(typeof(Enum), false)]
    [InlineData(typeof(ServiceStatus), true)]
    [InlineData(typeof(ServiceStatus?), true)]
    public void IsInt(Type type, Boolean result)
    {
        if (result)
            Assert.True(type.IsInt());
        else
            Assert.False(type.IsInt());
    }

    [Theory]
    [InlineData(typeof(Boolean), false)]
    [InlineData(typeof(Char), false)]
    [InlineData(typeof(SByte), true)]
    [InlineData(typeof(Byte), true)]
    [InlineData(typeof(Int16), true)]
    [InlineData(typeof(UInt16), true)]
    [InlineData(typeof(Int32), true)]
    [InlineData(typeof(UInt32), true)]
    [InlineData(typeof(Int64), true)]
    [InlineData(typeof(UInt64), true)]
    [InlineData(typeof(Single), true)]
    [InlineData(typeof(Double), true)]
    [InlineData(typeof(Decimal), true)]
    [InlineData(typeof(DateTime), false)]
    [InlineData(typeof(String), false)]
    [InlineData(typeof(Boolean?), false)]
    [InlineData(typeof(Char?), false)]
    [InlineData(typeof(SByte?), true)]
    [InlineData(typeof(Byte?), true)]
    [InlineData(typeof(Int16?), true)]
    [InlineData(typeof(UInt16?), true)]
    [InlineData(typeof(Int32?), true)]
    [InlineData(typeof(UInt32?), true)]
    [InlineData(typeof(Int64?), true)]
    [InlineData(typeof(UInt64?), true)]
    [InlineData(typeof(Single?), true)]
    [InlineData(typeof(Double?), true)]
    [InlineData(typeof(Decimal?), true)]
    [InlineData(typeof(DateTime?), false)]
    [InlineData(typeof(Enum), false)]
    [InlineData(typeof(ServiceStatus), true)]
    [InlineData(typeof(ServiceStatus?), true)]
    public void IsNumber(Type type, Boolean result)
    {
        if (result)
            Assert.True(type.IsNumber());
        else
            Assert.False(type.IsNumber());
    }

    [Theory]
    [InlineData("true", typeof(Boolean), true)]
    [InlineData("False", typeof(Boolean), false)]
    [InlineData("1", typeof(Boolean), true)]
    [InlineData("0", typeof(Boolean), false)]
    [InlineData("2", typeof(Boolean), true)]
    [InlineData("-1", typeof(Boolean), true)]
    [InlineData(1, typeof(Boolean), true)]
    [InlineData(0, typeof(Boolean), false)]
    [InlineData(-1, typeof(Boolean), true)]
    [InlineData("1234", typeof(Int16), (Int16)1234)]
    [InlineData("1234", typeof(Int32), 1234)]
    [InlineData("-1234", typeof(Int32), -1234)]
    [InlineData("0", typeof(Int32), 0)]
    [InlineData("-1", typeof(Int32), -1)]
    [InlineData("12.34", typeof(Double), 12.34)]
    [InlineData("-12.34", typeof(Double), -12.34)]
    [InlineData("byte[]", typeof(Type), typeof(Byte[]))]
    public void ChangeTypeTest(Object value, Type targetType, Object target)
    {
        var rs = value.ChangeType(targetType);
        Assert.Equal(target, rs);
    }

    [Fact]
    public void ChangeTypeWithNullable()
    {
        {
            var value = "2025-6-23";
            var rs = value.ChangeType(typeof(DateTime?));
            Assert.Equal(new DateTime(2025, 6, 23), rs);
        }
        {
            var value = "";
            var rs = value.ChangeType(typeof(DateTime?));
            Assert.Equal(DateTime.MinValue, rs);
        }
        {
            Object value = null;
            var rs = value.ChangeType(typeof(DateTime?));
            Assert.Null(rs);
        }
    }

    [Fact]
    public void ChangeTypeWithDecimal()
    {
        {
            var value = "2025.0623";
            var rs = value.ChangeType(typeof(Decimal));
            Assert.Equal(2025.0623m, rs);
        }
        {
            var value = "";
            var rs = value.ChangeType(typeof(Decimal));
            Assert.Equal(0m, rs);
        }
        {
            Object value = null;
            var rs = value.ChangeType(typeof(Decimal));
            Assert.Equal(0m, rs);
        }
    }

    [Fact]
    public void DateTimeOffsetChangeTypeTest()
    {
        var value = "2023/4/5 11:32 +08:00";
        var targetType = typeof(DateTimeOffset);
        var target = new DateTimeOffset(2023, 4, 5, 11, 32, 00, TimeSpan.FromHours(8));

        var rs = value.ChangeType(targetType);
        Assert.Equal(target, rs);
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void DateOnlyChangeTypeTest()
    {
        var value = "2023/4/5";
        var targetType = typeof(DateOnly);
        var target = new DateOnly(2023, 4, 5);

        Assert.Equal(target, value.ChangeType<DateOnly>());

        var rs = value.ChangeType(targetType);
        Assert.Equal(target, rs);
    }

    [Fact]
    public void TimeOnlyChangeTypeTest()
    {
        var value = "11:32";
        var targetType = typeof(TimeOnly);
        var target = new TimeOnly(11, 32, 00);

        Assert.Equal(target, value.ChangeType<TimeOnly>());

        var rs = value.ChangeType(targetType);
        Assert.Equal(target, rs);
    }
#endif

    [Fact]
    public void AsListTest()
    {
        var list = new List<Int32>();
        var type = list.GetType();

        Assert.True(type.As<List<Int32>>());
        Assert.True(type.As<IList<Int32>>());
        Assert.True(type.As<IList>());
        Assert.True(type.As(typeof(List<Int32>)));
        Assert.True(type.As(typeof(IList<Int32>)));
        Assert.True(type.As(typeof(IList<>)));
    }

    [Fact]
    public void AsDictionaryTest()
    {
        var dic = new Dictionary<Int32, String>();
        var type = dic.GetType();

        Assert.True(type.As<Dictionary<Int32, String>>());
        Assert.True(type.As<IDictionary<Int32, String>>());
        Assert.True(type.As<IDictionary>());
        Assert.True(type.As(typeof(Dictionary<Int32, String>)));
        Assert.True(type.As(typeof(IDictionary<Int32, String>)));
        Assert.True(type.As(typeof(IDictionary<,>)));
    }

    [Fact]
    public void CreateInstance()
    {
        var type = typeof(PageParameter);
        var obj = type.CreateInstance();
        Assert.NotNull(obj);
        Assert.IsType<PageParameter>(obj);

        type = typeof(DbTable);
        obj = type.CreateInstance();
        Assert.NotNull(obj);
        Assert.IsType<DbTable>(obj);
    }

    //[Fact]
    //public void CreateInstanceForArray()
    //{
    //    var type = typeof(Int32[]);
    //    var obj = type.CreateInstance();
    //    Assert.NotNull(obj);
    //    Assert.IsType<Int32[]>(obj);

    //    type = typeof(PageParameter[]);
    //    obj = type.CreateInstance();
    //    Assert.NotNull(obj);
    //    Assert.IsType<PageParameter[]>(obj);
    //}

    [Fact]
    public void CreateInstanceForList()
    {
        var type = typeof(List<Int32>);
        var obj = type.CreateInstance();
        Assert.NotNull(obj);
        Assert.IsType<List<Int32>>(obj);

        type = typeof(IList<PageParameter>);
        obj = type.CreateInstance();
        Assert.NotNull(obj);
        Assert.IsType<List<PageParameter>>(obj);

        type = typeof(IList);
        obj = type.CreateInstance();
        Assert.NotNull(obj);
        Assert.IsType<List<Object>>(obj);
    }

    [Fact]
    public void CreateInstanceForDictionary()
    {
        var type = typeof(Dictionary<Int32, String>);
        var obj = type.CreateInstance();
        Assert.NotNull(obj);
        Assert.IsType<Dictionary<Int32, String>>(obj);

        type = typeof(IDictionary<String, PageParameter>);
        obj = type.CreateInstance();
        Assert.NotNull(obj);
        Assert.IsType<Dictionary<String, PageParameter>>(obj);

        type = typeof(IDictionary);
        obj = type.CreateInstance();
        Assert.NotNull(obj);
        Assert.IsType<Dictionary<Object, Object>>(obj);
    }

    #region Copy 对象图
    private class CopyChild
    {
        public String? Name { get; set; }

        public Int32 Value { get; set; }
    }

    private class NestedHolder
    {
        public String? Title { get; set; }

        public CopyChild? Child { get; set; }
    }

    private class ArrayHolder
    {
        public CopyChild[]? Children { get; set; }
    }

    private class MatrixHolder
    {
        public CopyChild[,]? Matrix { get; set; }
    }

    private class DictHolder
    {
        public Dictionary<String, Object?>? Map { get; set; }
    }

    private class ListHolder
    {
        public List<CopyChild>? Items { get; set; }

        public List<String>? Tags { get; set; }
    }

    private class Node
    {
        public String? Name { get; set; }

        public Node? Next { get; set; }
    }

    private class SelfNode
    {
        public String? Name { get; set; }

        public SelfNode? Self { get; set; }
    }

    private class SharedHolder
    {
        public CopyChild? First { get; set; }

        public CopyChild? Second { get; set; }
    }

    private class SharedListHolder
    {
        public List<CopyChild>? First { get; set; }

        public List<CopyChild>? Second { get; set; }
    }

    private class InvokeSample
    {
        public Int32 Value { get; set; }

        public String? Name { get; set; }

        public Int32 Field = 0;

        public static Int32 StaticValue { get; set; }

        public Int32 Add(Int32 a, Int32 b) => a + b;

        public Int32 Zero() => 42;

        public static Int32 StaticAdd(Int32 a, Int32 b) => a + b;

        public void SetName(String? name) => Name = name;
    }

    [Fact(DisplayName = "Copy 浅拷贝：嵌套对象、数组、字典、集合均复制引用")]
    public void Copy_Shallow_KeepsReferences()
    {
        var child = new CopyChild { Name = "c1", Value = 1 };
        var src = new NestedHolder { Title = "t", Child = child };
        var dst = new NestedHolder();
        dst.Copy(src);
        Assert.Equal("t", dst.Title);
        Assert.Same(src.Child, dst.Child);
        Assert.Equal(1, dst.Child!.Value);

        var arraySrc = new ArrayHolder { Children = [child] };
        var arrayDst = new ArrayHolder();
        arrayDst.Copy(arraySrc);
        Assert.Same(arraySrc.Children, arrayDst.Children);

        var matrixSrc = new MatrixHolder { Matrix = new CopyChild[1, 1] { { child } } };
        var matrixDst = new MatrixHolder();
        matrixDst.Copy(matrixSrc);
        Assert.Same(matrixSrc.Matrix, matrixDst.Matrix);

        var map = new Dictionary<String, Object?> { ["child"] = child };
        var dictSrc = new DictHolder { Map = map };
        var dictDst = new DictHolder();
        dictDst.Copy(dictSrc);
        Assert.Same(map, dictDst.Map);

        var items = new List<CopyChild> { child };
        var listSrc = new ListHolder { Items = items, Tags = ["a", "b"] };
        var listDst = new ListHolder();
        listDst.Copy(listSrc);
        Assert.Same(items, listDst.Items);
        Assert.Same(listSrc.Tags, listDst.Tags);
    }

    [Fact(DisplayName = "Copy 浅拷贝：支持忽略成员")]
    public void Copy_Shallow_Excludes()
    {
        var src = new NestedHolder { Title = "t", Child = new CopyChild { Name = "c" } };
        var dst = new NestedHolder();
        dst.Copy(src, false, "Title", "Child");
        Assert.Null(dst.Title);
        Assert.Null(dst.Child);
    }

    [Fact(DisplayName = "Copy 深拷贝：嵌套对象、一维数组、集合、字典均复制值")]
    public void Copy_Deep_CopiesValues()
    {
        var child1 = new CopyChild { Name = "a", Value = 1 };
        var child2 = new CopyChild { Name = "b", Value = 2 };

        var nestedSrc = new NestedHolder { Title = "t", Child = child1 };
        var nestedDst = new NestedHolder();
        nestedDst.Copy(nestedSrc, deep: true);
        Assert.Equal("t", nestedDst.Title);
        Assert.NotSame(nestedSrc.Child, nestedDst.Child);
        Assert.Equal("a", nestedDst.Child!.Name);
        Assert.Equal(1, nestedDst.Child.Value);

        // 数组属性在目标为空时无法通过 CreateInstance 构造，目标需预先分配（见 Copy_Deep_NullArrayThrows）
        var arraySrc = new ArrayHolder { Children = [child1, child2] };
        var arrayDst = new ArrayHolder { Children = new CopyChild[2] };
        arrayDst.Copy(arraySrc, deep: true);
        Assert.NotSame(arraySrc.Children, arrayDst.Children);
        Assert.Equal(2, arrayDst.Children!.Length);
        Assert.NotSame(child1, arrayDst.Children[0]);
        Assert.Equal("a", arrayDst.Children[0].Name);
        Assert.Equal("b", arrayDst.Children[1].Name);

        var listSrc = new ListHolder { Items = [child1, child2], Tags = ["x", "y"] };
        var listDst = new ListHolder();
        listDst.Copy(listSrc, deep: true);
        Assert.Equal(2, listDst.Items!.Count);
        Assert.NotSame(child1, listDst.Items[0]);
        Assert.Equal(1, listDst.Items[0].Value);
        Assert.Equal(new[] { "x", "y" }, listDst.Tags);
    }

    [Fact(DisplayName = "Copy 深拷贝：Dictionary<String,Object> 值对象递归复制")]
    public void Copy_Deep_Dictionary()
    {
        var child = new CopyChild { Name = "a", Value = 1 };
        var map = new Dictionary<String, Object?> { ["child"] = child, ["n"] = 1, ["s"] = "text" };
        var src = new DictHolder { Map = map };

        var shallowDst = new DictHolder();
        shallowDst.Copy(src);
        Assert.Same(map, shallowDst.Map);

        var deepDst = new DictHolder { Map = [] };
        deepDst.Copy(src, deep: true);
        Assert.NotSame(map, deepDst.Map);
        Assert.Equal(3, deepDst.Map!.Count);
        var copied = Assert.IsType<CopyChild>(deepDst.Map["child"]);
        Assert.NotSame(child, copied);
        Assert.Equal("a", copied.Name);
        Assert.Equal(1, copied.Value);
        Assert.Equal(1, deepDst.Map["n"]);
        Assert.Equal("text", deepDst.Map["s"]);
    }

    [Fact(DisplayName = "Copy 字典重载：浅拷贝赋值引用，深拷贝递归对象")]
    public void Copy_FromDictionary()
    {
        var child = new CopyChild { Name = "a", Value = 1 };
        var dic = new Dictionary<String, Object?> { ["Title"] = "t", ["Child"] = child };

        var shallowDst = new NestedHolder();
        shallowDst.Copy(dic);
        Assert.Equal("t", shallowDst.Title);
        Assert.Same(child, shallowDst.Child);

        var deepDst = new NestedHolder();
        deepDst.Copy(dic, deep: true);
        Assert.NotSame(child, deepDst.Child);
        Assert.Equal("a", deepDst.Child!.Name);
    }

    [Fact(DisplayName = "Copy 循环引用：浅拷贝保留引用，深拷贝安全复用副本")]
    public void Copy_CircularReference()
    {
        var a = new Node { Name = "a" };
        var b = new Node { Name = "b", Next = a };
        a.Next = b;

        var dst = new Node();
        dst.Copy(a);

        Assert.Equal("a", dst.Name);
        Assert.Same(b, dst.Next);
        Assert.Same(a, dst.Next!.Next);
        Assert.NotSame(a, dst);

        // 深拷贝：环处复用已构造的副本，不再无限递归
        var deepDst = new Node();
        deepDst.Copy(a, deep: true);

        Assert.Equal("a", deepDst.Name);
        Assert.NotSame(a, deepDst);
        Assert.NotNull(deepDst.Next);
        Assert.NotSame(b, deepDst.Next);
        Assert.Equal("b", deepDst.Next!.Name);
        Assert.Same(deepDst, deepDst.Next.Next);
    }

    [Fact(DisplayName = "Copy 深拷贝：自引用对象复用自身副本")]
    public void Copy_Deep_SelfReference()
    {
        var src = new SelfNode { Name = "root" };
        src.Self = src;

        var dst = new SelfNode();
        dst.Copy(src, deep: true);

        Assert.Equal("root", dst.Name);
        Assert.NotSame(src, dst);
        Assert.Same(dst, dst.Self);
    }

    [Fact(DisplayName = "Copy 深拷贝：同一共享子实例复用同一副本")]
    public void Copy_Deep_SharedReference()
    {
        var child = new CopyChild { Name = "shared", Value = 7 };
        var src = new SharedHolder { First = child, Second = child };

        var dst = new SharedHolder();
        dst.Copy(src, deep: true);

        Assert.NotSame(child, dst.First);
        Assert.Same(dst.First, dst.Second);
        Assert.Equal("shared", dst.First!.Name);
        Assert.Equal(7, dst.First.Value);

        // 共享集合同样复用同一副本
        var items = new List<CopyChild> { child };
        var listSrc = new SharedListHolder { First = items, Second = items };
        var listDst = new SharedListHolder();
        listDst.Copy(listSrc, deep: true);

        Assert.NotSame(items, listDst.First);
        Assert.Same(listDst.First, listDst.Second);
        Assert.NotSame(child, listDst.First![0]);
        Assert.Equal("shared", listDst.First[0].Name);
    }

    [Fact(DisplayName = "Copy 深拷贝：DAG 共享叶节点正常复制")]
    public void Copy_Deep_Dag()
    {
        var leaf = new CopyChild { Name = "leaf", Value = 3 };
        var src = new SharedHolder { First = leaf, Second = leaf };

        var dst = new SharedHolder();
        dst.Copy(src, deep: true);

        Assert.NotNull(dst.First);
        Assert.NotNull(dst.Second);
        Assert.Same(dst.First, dst.Second);
        Assert.Equal("leaf", dst.First!.Name);
        Assert.Equal(3, dst.First.Value);
    }

    [Fact(DisplayName = "Copy 深拷贝：目标数组为空时无法构造数组实例（现状固化）")]
    public void Copy_Deep_NullArrayThrows()
    {
        var src = new ArrayHolder { Children = [new CopyChild { Name = "a" }] };
        var dst = new ArrayHolder();

        // 现状固化：Copy 在目标成员为空时用 CreateInstance 构造属性类型，数组类型没有构造器，包装为 Exception。
        // 已知限制：修复方向是在 ArrayCopy 前按源数组长度创建数组，本用例只锁定行为避免回归。
        var ex = Assert.Throws<Exception>(() => dst.Copy(src, deep: true));
        Assert.Contains("Fail to create object", ex.Message);
    }

    [Fact(DisplayName = "Copy 多维数组：浅拷贝复制引用，深拷贝不受支持")]
    public void Copy_MultiDimArray()
    {
        var src = new MatrixHolder { Matrix = new CopyChild[2, 2] { { new() { Name = "a" }, new() { Name = "b" } }, { new() { Name = "c" }, new() { Name = "d" } } } };
        var dst = new MatrixHolder();
        dst.Copy(src);
        Assert.Same(src.Matrix, dst.Matrix);

        // 现状固化：ArrayCopy 仅按一维索引访问（Array.GetValue(Int32)），多维数组深拷贝抛 ArgumentException。
        var dst2 = new MatrixHolder { Matrix = new CopyChild[2, 2] };
        Assert.Throws<ArgumentException>(() => dst2.Copy(src, deep: true));
    }
    #endregion

    #region Invoke / GetValue / SetValue
    [Fact(DisplayName = "Invoke 公共 API：实例/静态、有参/无参方法")]
    public void Invoke_PublicApi()
    {
        var obj = new InvokeSample();

        Assert.Equal(3, obj.Invoke("Add", 1, 2).ToInt());
        Assert.Equal(42, obj.Invoke("Zero").ToInt());
        Assert.Equal(7, typeof(InvokeSample).Invoke("StaticAdd", 3, 4).ToInt());

        var add = typeof(InvokeSample).GetMethod(nameof(InvokeSample.Add))!;
        var zero = typeof(InvokeSample).GetMethod(nameof(InvokeSample.Zero))!;
        Assert.Equal(5, obj.Invoke(add, 2, 3).ToInt());
        Assert.Equal(42, obj.Invoke(zero).ToInt());

        obj.Invoke("SetName", "n");
        Assert.Equal("n", obj.Name);
    }

    [Fact(DisplayName = "Invoke 提供者接口：实例方法与静态方法")]
    public void Invoke_Provider()
    {
        var obj = new InvokeSample();
        var add = typeof(InvokeSample).GetMethod(nameof(InvokeSample.Add))!;
        var staticAdd = typeof(InvokeSample).GetMethod(nameof(InvokeSample.StaticAdd))!;

        Assert.Equal(5, Reflect.Provider.Invoke(obj, add, [2, 3]).ToInt());
        Assert.Equal(9, Reflect.Provider.Invoke(typeof(InvokeSample), staticAdd, [4, 5]).ToInt());
        Assert.Equal(42, Reflect.Provider.Invoke(obj, typeof(InvokeSample).GetMethod(nameof(InvokeSample.Zero))!, null).ToInt());
    }

    [Fact(DisplayName = "GetValue/SetValue 公共 API：属性、字段、静态成员与类型转换")]
    public void GetValue_SetValue_PublicApi()
    {
        var obj = new InvokeSample { Value = 5 };
        Assert.Equal(5, obj.GetValue("Value"));
        Assert.True(obj.SetValue("Value", 9));
        Assert.Equal(9, obj.Value);

        // SetValueCore 通过 ChangeType 转换数值类型
        Assert.True(obj.SetValue("Value", 12L));
        Assert.Equal(12, obj.Value);

        Assert.True(obj.SetValue("Field", 3));
        Assert.Equal(3, obj.GetValue("Field"));

        typeof(InvokeSample).SetValue(nameof(InvokeSample.StaticValue), 8);
        Assert.Equal(8, InvokeSample.StaticValue);
        Assert.Equal(8, typeof(InvokeSample).GetValue(nameof(InvokeSample.StaticValue)));
    }

    [Fact(DisplayName = "GetValue/SetValue 提供者接口：属性与静态属性")]
    public void GetValue_SetValue_Provider()
    {
        var obj = new InvokeSample { Value = 5 };
        var pi = typeof(InvokeSample).GetProperty(nameof(InvokeSample.Value))!;
        Assert.Equal(5, Reflect.Provider.GetValue(obj, pi));
        Reflect.Provider.SetValue(obj, pi, 6);
        Assert.Equal(6, obj.Value);

        var spi = typeof(InvokeSample).GetProperty(nameof(InvokeSample.StaticValue))!;
        Reflect.Provider.SetValue(typeof(InvokeSample), spi, 11);
        Assert.Equal(11, InvokeSample.StaticValue);
    }
    #endregion
}