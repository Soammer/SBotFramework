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

    #region ExportImage — PNG

    [TestMethod]
    public void ExportImage_Png_CreatesFile()
    {
        const string c_font = "Sarasa Fixed Slab SC";
        var path = Path.Combine(_tempDir, "output.png");
        string text = ".reg [名称] [种族] 注册，不超过14有效字符串（中文视为2）\n种族包括：1-人 2-猫粮";
        var (h, w) = ImageBuilder.MeasureImageSize(text, 28, FontStyle.Regular, 16, c_font);
        using var result = ImageBuilder.BuildImage(text, h, w, 28, FontStyle.Regular, c_font);
        ImageBuilder.ExportImage(result, path);
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
