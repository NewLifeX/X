using NewLife.Configuration;
using Xunit;

namespace XUnitTest.Configuration;

public class ConfigHelperTests
{
    private static IConfigSection NewRoot()
    {
        return new ConfigSection { Key = "Root", Childs = [] };
    }

    private static IConfigSection NewElement(String key, String? value)
    {
        return new ConfigSection { Key = key, Value = value };
    }

    [Fact]
    public void MapToList_FillsWhenPropertyIsNull()
    {
        // Arrange: Root with two list nodes: Numbers (Int32 values) and Names (String values)
        var root = NewRoot();
        var numbers = root.AddChild("Numbers");
        numbers.Childs =
        [
            NewElement(nameof(Int32), "1"),
            NewElement(nameof(Int32), "2"),
            NewElement(nameof(Int32), "3"),
        ];
        var names = root.AddChild("Names");
        names.Childs =
        [
            NewElement(nameof(String), "Alice"),
            NewElement(nameof(String), "Bob"),
        ];

        var model = new ListModel();
        var provider = new XmlConfigProvider();

        // Act
        root.MapTo(model, provider);

        // Assert
        Assert.NotNull(model.Numbers);
        Assert.Equal(new[] { 1, 2, 3 }, model.Numbers);
        Assert.NotNull(model.Names);
        Assert.Equal(["Alice", "Bob"], model.Names);
    }

    [Fact]
    public void MapFrom_List_SerializesAsAttributeElementsAndResets()
    {
        // Arrange
        var root = NewRoot();
        var model = new ListModel
        {
            Numbers = [3, 4, 5],
            Names = ["A"]
        };

        // Act 1: Map to section
        root.MapFrom(model);

        // Assert structure after first mapping
        var numbers = root.Find("Numbers");
        Assert.NotNull(numbers);
        Assert.NotNull(numbers.Childs);
        Assert.Equal(3, numbers.Childs.Count);
        foreach (var it in numbers.Childs)
        {
            Assert.Equal(nameof(Int32), it.Key);
        }
        Assert.Equal(new[] { "3", "4", "5" }, numbers.Childs.Select(e => e.Value).ToArray());

        var names = root.Find("Names");
        Assert.NotNull(names);
        Assert.NotNull(names.Childs);
        Assert.Single(names.Childs);
        Assert.Equal(nameof(String), names.Childs[0].Key);
        Assert.Equal("A", names.Childs[0].Value);

        // Act 2: Change model and map again (should reset, not append)
        model.Numbers = [7];
        model.Names = ["B", "C"];
        root.MapFrom(model);

        // Assert after second mapping
        numbers = root.Find("Numbers");
        Assert.NotNull(numbers);
        Assert.NotNull(numbers.Childs);
        Assert.Single(numbers.Childs);
        Assert.Equal("7", numbers.Childs[0].Value);

        names = root.Find("Names");
        Assert.NotNull(names);
        Assert.NotNull(names.Childs);
        Assert.Equal(2, names.Childs.Count);
        Assert.Equal(new[] { "B", "C" }, names.Childs.Select(e => e.Value).ToArray());
    }

    [Fact]
    public void SetValue_Boolean_UsesLowerInvariant()
    {
        // Arrange
        var root = NewRoot();
        var model = new BoolModel { Flag = true };

        // Act
        root.MapFrom(model);

        // Assert
        Assert.Equal("true", root["Flag"]);
    }

    [Fact]
    public void Find_CreatesNested_WhenCreateOnMiss()
    {
        // Arrange
        var root = NewRoot();

        // Act
        var leaf = root.Find("A:B:C", true);

        // Assert
        Assert.NotNull(leaf);
        Assert.Equal("C", leaf.Key);
        Assert.NotNull(root.Find("A:B:C"));
    }

    private sealed class ListModel
    {
        public List<Int32> Numbers { get; set; }
        public IList<String> Names { get; set; }
    }

    private sealed class BoolModel
    {
        public Boolean Flag { get; set; }
    }
}
