using NewLife;
using NewLife.Compression;
using Xunit;

namespace XUnitTest.Compression;

/// <summary>Tar 解压路径越界防护</summary>
public class TarSlipTests
{
    private static String NewRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "TarSlip_" + Environment.ProcessId + "_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(root);

        return root;
    }

    private static String CreateTar(String root, String entryName)
    {
        var src = Path.Combine(root, "payload.txt");
        File.WriteAllText(src, "evil");

        var tarPath = Path.Combine(root, "evil.tar");
        var tar = new TarFile();
        tar.CreateEntryFromFile(src, entryName);
        using (var fs = File.Create(tarPath)) tar.Write(fs);

        return tarPath;
    }

    [Theory(DisplayName = "解压时条目逃出目标目录会被拒绝")]
    [InlineData("..")]
    [InlineData("../evil.txt")]
    [InlineData("../../evil.txt")]
    [InlineData("sub/../../evil.txt")]
    public void RejectOutsideEntry(String entryName)
    {
        var root = NewRoot();
        try
        {
            var tarPath = CreateTar(root, entryName);
            var dest = Path.Combine(root, "dest");

            Assert.Throws<IOException>(() => TarFile.ExtractToDirectory(tarPath, dest, true));
            Assert.False(File.Exists(Path.Combine(root, "evil.txt")));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact(DisplayName = "解压时绝对路径条目会被拒绝")]
    public void RejectAbsoluteEntry()
    {
        var root = NewRoot();
        try
        {
            var outside = Path.GetFullPath(Path.Combine(root, "..", "evil-abs-" + Guid.NewGuid().ToString("N")[..8] + ".txt"));
            var tarPath = CreateTar(root, outside);
            var dest = Path.Combine(root, "dest");

            Assert.Throws<IOException>(() => TarFile.ExtractToDirectory(tarPath, dest, true));
            Assert.False(File.Exists(outside));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact(DisplayName = "正常条目仍可解压")]
    public void NormalEntryExtracted()
    {
        var root = NewRoot();
        try
        {
            var tarPath = CreateTar(root, "sub/payload.txt");
            var dest = Path.Combine(root, "dest");

            TarFile.ExtractToDirectory(tarPath, dest, true);

            var file = Path.Combine(dest, "sub", "payload.txt");
            Assert.True(File.Exists(file));
            Assert.Equal("evil", File.ReadAllText(file));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
