using NewLife.Configuration;
using Xunit;

namespace XUnitTest.Configuration;

public class ConfigHelperPrivateTests
{
    private static IConfigSection NewRoot() => new ConfigSection { Key = "Root", Childs = [] };
    private static IConfigSection NewElement(String key, String? value) => new ConfigSection { Key = key, Value = value };

    // 覆盖 MapToObject/MapToArray/MapToList：包含基元、数组、列表、复杂类型
    [Fact]
    public void MapTo_Mixes_Base_Array_List_Complex()
    {
        // Arrange
        var root = NewRoot();
        root.AddChild("BoolProp").Value = "true";

        var intArray = root.AddChild("IntArray");
        intArray.Childs = [
            NewElement("x", "1"),
            NewElement("y", "2")
        ];

        var complexProp = root.AddChild("ComplexProp");
        complexProp.Childs = [
            NewElement("Name", "node"),
            NewElement("Value", "7"),
        ];

        var complexList = root.AddChild("ComplexList");
        complexList.Childs = [
            new ConfigSection
            {
                Key = "item1",
                Childs = [NewElement("Name", "n1"), NewElement("Value", "1")]
            },
            new ConfigSection
            {
                Key = "item2",
                Childs = [NewElement("Name", "n2"), NewElement("Value", "2")]
            },
        ];

        var complexArray = root.AddChild("ComplexArray");
        complexArray.Childs = [
            new ConfigSection
            {
                Key = "node1",
                Childs = [NewElement("Name", "a"), NewElement("Value", "10")]
            },
            new ConfigSection
            {
                Key = "node2",
                Childs = [NewElement("Name", "b"), NewElement("Value", "20")]
            },
        ];

        var model = new ComplexModel
        {
            // 预填充以验证数组长度会被重建、列表会被清空
            IntArray = [9, 9, 9],
            ComplexList = [new Node { Name = "x", Value = 100 }]
        };
        var provider = new XmlConfigProvider();

        // Act
        root.MapTo(model, provider);

        // Assert 基元
        Assert.True(model.BoolProp);

        // Assert 数组（基元）
        Assert.Equal(new[] { 1, 2 }, model.IntArray);

        // Assert 复杂属性（空时自动实例化并递归映射）
        Assert.NotNull(model.ComplexProp);
        Assert.Equal("node", model.ComplexProp.Name);
        Assert.Equal(7, model.ComplexProp.Value);

        // Assert 列表（复杂元素），并验证原内容被清空
        Assert.NotNull(model.ComplexList);
        Assert.Equal(2, model.ComplexList.Count);
        Assert.Equal(["n1", "n2"], model.ComplexList.Select(e => e.Name).ToArray());
        Assert.Equal([1, 2], model.ComplexList.Select(e => e.Value).ToArray());

        // Assert 数组（复杂元素）
        Assert.NotNull(model.ComplexArray);
        Assert.Equal(2, model.ComplexArray.Length);
        Assert.Equal(["a", "b"], model.ComplexArray.Select(e => e.Name).ToArray());
        Assert.Equal([10, 20], model.ComplexArray.Select(e => e.Value).ToArray());
    }

    // 覆盖 MapToArray：当目标数组长度与配置不一致时，按配置重建
    [Fact]
    public void MapToArray_RebuildsLength()
    {
        var root = NewRoot();
        var arrSec = root.AddChild("IntArray");
        arrSec.Childs = [NewElement("x", "10"), NewElement("y", "20")];

        var model = new ComplexModel { IntArray = [1, 2, 3] };
        var provider = new XmlConfigProvider();

        root.MapTo(model, provider);

        Assert.Equal(new[] { 10, 20 }, model.IntArray);
    }

    // 覆盖 MapToList：当已有列表实例时会清空并填充
    [Fact]
    public void MapToList_ClearsExisting()
    {
        var root = NewRoot();
        var numbers = root.AddChild("Numbers");
        numbers.Childs = [NewElement(nameof(Int32), "1"), NewElement(nameof(Int32), "2")];

        var model = new ListModel { Numbers = [100, 200] };
        var provider = new XmlConfigProvider();

        root.MapTo(model, provider);

        Assert.Equal(new[] { 1, 2 }, model.Numbers);
    }

    // 覆盖 MapObject 和 MapArray：从模型到配置，包含基元、列表（基元与复杂）、复杂对象
    [Fact]
    public void MapFrom_Covers_Base_List_Complex_And_Enum()
    {
        var root = NewRoot();
        var model = new ComplexModel
        {
            BoolProp = true,
            EnumProp = DayOfWeek.Friday,
            IntArray = [5, 6], // 注意：MapFrom 针对数组不一定进入 MapArray，这里主要覆盖列表路径
            Numbers = [3, 4],
            ComplexProp = new Node { Name = "p", Value = 9 },
            ComplexList = [new Node { Name = "l1", Value = 1 }, new Node { Name = "l2", Value = 2 }],
        };

        root.MapFrom(model);

        // 基元与枚举（SetValue 走到布尔与枚举分支）
        Assert.Equal("true", root.Find("BoolProp")?.Value);
        Assert.Equal("Friday", root.Find("EnumProp")?.Value);

        // 列表（基元）
        var numbers = root.Find("Numbers");
        Assert.NotNull(numbers);
        Assert.NotNull(numbers!.Childs);
        Assert.Equal(2, numbers.Childs!.Count);
        Assert.All(numbers.Childs!, c => Assert.Equal(nameof(Int32), c.Key));
        Assert.Equal(["3", "4"], numbers.Childs!.Select(e => e.Value).ToArray());

        // 复杂对象
        var cp = root.Find("ComplexProp");
        Assert.NotNull(cp);
        Assert.Equal("p", cp!.Childs!.First(e => e.Key == "Name").Value);
        Assert.Equal("9", cp!.Childs!.First(e => e.Key == "Value").Value);

        // 列表（复杂元素）
        var cl = root.Find("ComplexList");
        Assert.NotNull(cl);
        Assert.NotNull(cl!.Childs);
        Assert.Equal(2, cl.Childs!.Count);
        // 验证 MapArray 使用元素类型名作为子节点 Key（Node）
        Assert.All(cl.Childs!, c => Assert.Equal(nameof(Node), c.Key));
        Assert.Equal(["l1", "l2"], cl.Childs!.Select(e => e.Childs!.First(x => x.Key == "Name").Value).ToArray());
    }

    private sealed class ListModel
    {
        public List<Int32> Numbers { get; set; }
    }

    private sealed class ComplexModel
    {
        public Boolean BoolProp { get; set; }
        public DayOfWeek EnumProp { get; set; }

        public Int32[] IntArray { get; set; }
        public List<Int32> Numbers { get; set; }

        public Node ComplexProp { get; set; }
        public List<Node> ComplexList { get; set; }
        public Node[] ComplexArray { get; set; }
    }

    private sealed class Node
    {
        public String Name { get; set; }
        public Int32 Value { get; set; }
    }
}
