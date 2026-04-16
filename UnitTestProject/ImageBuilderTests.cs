using STextRenderer.Core;

namespace UnitTestProject;

[TestClass]
public sealed class ImageBuilderTests
{
    private string _tempDir = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine("D:\\tmp", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        //if (Directory.Exists(_tempDir))
        //{
        //    Directory.Delete(_tempDir, recursive: true);
        //}
    }


    #region BuildImage — 文本换行
    [TestMethod]
    public void BuildImage_LongText_WrapsWithoutThrow()
    {
        var text = string.Concat(Enumerable.Repeat("这是一段用于测试自动换行的中文文本。", 20));
        using var result = ImageBuilder.BuildImage(text, 400, 600, 16, FontStyle.Regular);
        Assert.IsNotNull(result);
    }

    #endregion

    #region ExportImage — PNG

    [TestMethod]
    public void ExportImage_Png_CreatesFile()
    {
        var path = Path.Combine(_tempDir, "output.png");
        string text = """
            .reg
            -reg [名称] [种族]，名称有效字符串不超过14（中文视为2）
            种族包括1- 人 2-猫粮
            """;
        var (h, w) = ImageBuilder.MeasureImageSize(text, 14, FontStyle.Regular, 30);
        using var result = ImageBuilder.BuildImage(text, h, w, 14, FontStyle.Regular);
        ImageBuilder.ExportImage(result, path);
        Assert.IsTrue(File.Exists(path));
    }

    [TestMethod]
    public void ExportImage_Png_CreatesNestedDirectory()
    {
        var path = Path.Combine(_tempDir, "sub", "nested", "output.png");
        using var data = ImageBuilder.BuildImage("Dir test", 100, 50, 14, FontStyle.Regular);
        ImageBuilder.ExportImage(data, path);
        Assert.IsTrue(File.Exists(path));
    }

    #endregion

    #region Helper

    private static byte[] ReadHeader(string path, int count)
    {
        using var fs = File.OpenRead(path);
        var buffer = new byte[count];
        _ = fs.Read(buffer, 0, count);
        return buffer;
    }

    #endregion
}
