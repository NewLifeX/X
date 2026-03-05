using Xunit;

namespace XUnitTest.Extension;

public class ListExtensionTests
{
    [Fact(DisplayName = "Find在List上搜索")]
    public void Find_OnList()
    {
        IList<Int32> list = new List<Int32> { 1, 2, 3, 4, 5 };

        var result = list.Find(x => x == 3);

        Assert.Equal(3, result);
    }

    [Fact(DisplayName = "Find未找到返回默认值")]
    public void Find_NotFound()
    {
        IList<String> list = new List<String> { "a", "b", "c" };

        var result = list.Find(x => x == "z");

        Assert.Null(result);
    }

    [Fact(DisplayName = "Find在非List实现上搜索")]
    public void Find_OnNonList()
    {
        IList<Int32> list = new Int32[] { 10, 20, 30 };

        var result = list.Find(x => x == 20);

        Assert.Equal(20, result);
    }

    [Fact(DisplayName = "FindAll在List上搜索")]
    public void FindAll_OnList()
    {
        IList<Int32> list = new List<Int32> { 1, 2, 3, 4, 5 };

        var result = list.FindAll(x => x > 3);

        Assert.Equal(2, result.Count);
        Assert.Contains(4, result);
        Assert.Contains(5, result);
    }

    [Fact(DisplayName = "FindAll未找到返回空列表")]
    public void FindAll_NotFound()
    {
        IList<String> list = new List<String> { "a", "b" };

        var result = list.FindAll(x => x == "z");

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact(DisplayName = "FindAll在非List实现上搜索")]
    public void FindAll_OnNonList()
    {
        IList<Int32> list = new Int32[] { 1, 2, 3, 4, 5 };

        var result = list.FindAll(x => x % 2 == 0);

        Assert.Equal(2, result.Count);
        Assert.Contains(2, result);
        Assert.Contains(4, result);
    }

    [Fact(DisplayName = "Find返回第一个匹配")]
    public void Find_ReturnsFirst()
    {
        IList<Int32> list = new List<Int32> { 1, 2, 2, 3 };

        var result = list.Find(x => x == 2);

        Assert.Equal(2, result);
    }
}
