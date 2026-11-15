using NewLife;
using NewLife.Configuration;
using Xunit;

namespace XUnitTest.Configuration;

/// <summary>配置树映射：数组置空后不应残留旧元素</summary>
public class ConfigArrayResetTests
{
    private class ArrayModel
    {
        public String[]? Urls { get; set; }
    }

    [Fact(DisplayName = "数组属性置空后旧子节点与数组标记被清除")]
    public void NullArrayClearsChilds()
    {
        var model = new ArrayModel { Urls = ["a", "b"] };
        var root = new ConfigSection();
        root.MapFrom(model);

        var section = root.Childs!.First(e => e.Key.EqualIgnoreCase("Urls")) as ConfigSection;
        Assert.NotNull(section);
        Assert.NotEmpty(section!.Childs!);

        // 置空后重新映射
        model.Urls = null;
        root.MapFrom(model);

        section = root.Childs!.First(e => e.Key.EqualIgnoreCase("Urls")) as ConfigSection;
        Assert.NotNull(section);
        Assert.True(section!.Childs == null || section.Childs.Count == 0);
        Assert.False(section.IsArray);
    }
}
