using NewLife;
using NewLife.Configuration;
using Xunit;

namespace XUnitTest.Configuration;

public class FileConfigProviderTests
{
    private readonly String _testFile;

    public FileConfigProviderTests()
    {
        _testFile = $"Config/test_fcp_{Guid.NewGuid():n}.json";
    }

    [Fact]
    public void Load_FromFile()
    {
        var json = "{\"Name\":\"test\",\"Count\":99}";
        File.WriteAllText(_testFile, json);

        try
        {
            var prv = new JsonConfigProvider { FileName = _testFile };
            var model = prv.Load<FcpModel>();

            Assert.NotNull(model);
            Assert.Equal("test", model.Name);
            Assert.Equal(99, model.Count);
        }
        finally
        {
            File.Delete(_testFile);
        }
    }

    [Fact]
    public void Save_And_Load_RoundTrip()
    {
        var prv = new JsonConfigProvider { FileName = _testFile };

        try
        {
            var model = new FcpModel { Name = "roundtrip", Count = 42 };
            prv.Save(model);

            Assert.True(File.Exists(_testFile));

            var loaded = prv.Load<FcpModel>();
            Assert.NotNull(loaded);
            Assert.Equal("roundtrip", loaded.Name);
            Assert.Equal(42, loaded.Count);
        }
        finally
        {
            File.Delete(_testFile);
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsNew()
    {
        var prv = new JsonConfigProvider { FileName = _testFile };
        var model = prv.Load<FcpModel>();

        Assert.NotNull(model);
        Assert.Null(model.Name);
        Assert.Equal(0, model.Count);
    }

    [Fact]
    public void FileName_Property()
    {
        var prv = new JsonConfigProvider { FileName = _testFile };
        Assert.Equal(_testFile, prv.FileName);
    }

    [Fact]
    public void Save_CreatesFile()
    {
        var prv = new JsonConfigProvider { FileName = _testFile };
        try
        {
            prv.Save(new FcpModel { Name = "created" });
            Assert.True(File.Exists(_testFile));
        }
        finally
        {
            File.Delete(_testFile);
        }
    }

    private class FcpModel
    {
        public String? Name { get; set; }
        public Int32 Count { get; set; }
    }
}
