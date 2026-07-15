using NewLife;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Serialization;

public class JsonReaderTests
{
    private readonly JsonReader _reader = new();

    [Fact]
    public void Read_String()
    {
        var json = "{\"Name\":\"hello\"}";
        var result = _reader.Read<Person>(json);

        Assert.NotNull(result);
        Assert.Equal("hello", result.Name);
    }

    [Fact]
    public void Read_Int32()
    {
        var json = "{\"value\":42}";
        var result = _reader.Read<IntHolder>(json);

        Assert.NotNull(result);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Read_Boolean()
    {
        var json = "{\"flag\":true}";
        var result = _reader.Read<FlagHolder>(json);

        Assert.NotNull(result);
        Assert.True(result.Flag);
    }

    [Fact]
    public void Read_Null()
    {
        var json = "{\"Name\":null}";
        var result = _reader.Read<Person>(json);

        Assert.NotNull(result);
        Assert.Null(result.Name);
    }

    [Fact]
    public void Read_SimpleObject()
    {
        var json = "{\"Name\":\"test\",\"Age\":30}";
        var result = _reader.Read<Person>(json);

        Assert.NotNull(result);
        Assert.Equal("test", result.Name);
        Assert.Equal(30, result.Age);
    }

    [Fact]
    public void Read_NestedObject()
    {
        var json = "{\"Title\":\"Engineer\",\"Person\":{\"Name\":\"Alice\",\"Age\":28}}";
        var result = _reader.Read<Job>(json);

        Assert.NotNull(result);
        Assert.Equal("Engineer", result.Title);
        Assert.NotNull(result.Person);
        Assert.Equal("Alice", result.Person.Name);
        Assert.Equal(28, result.Person.Age);
    }

    [Fact]
    public void Read_Array()
    {
        var json = "[1,2,3,4,5]";
        var result = _reader.Read<Int32[]>(json);

        Assert.NotNull(result);
        Assert.Equal(5, result.Length);
        Assert.Equal(1, result[0]);
        Assert.Equal(5, result[4]);
    }

    [Fact]
    public void Read_EmptyObject()
    {
        var json = "{}";
        var result = _reader.Read<Person>(json);

        Assert.NotNull(result);
        Assert.Null(result.Name);
        Assert.Equal(0, result.Age);
    }

    [Fact]
    public void Read_TypeOverload()
    {
        var json = "{\"Name\":\"hello\"}";
        var result = _reader.Read(json, typeof(Person)) as Person;

        Assert.NotNull(result);
        Assert.Equal("hello", result.Name);
    }

    [Fact]
    public void Read_InvalidJson()
    {
        Assert.Throws<XException>(() => _reader.Read<String>("not json"));
    }

    public class Person
    {
        public String? Name { get; set; }
        public Int32 Age { get; set; }
    }

    public class Job
    {
        public String? Title { get; set; }
        public Person? Person { get; set; }
    }

    public class IntHolder
    {
        public Int32 Value { get; set; }
    }

    public class FlagHolder
    {
        public Boolean Flag { get; set; }
    }
}
