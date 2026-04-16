using SkiaSharp;
using System.Text;

namespace STextRenderer.Core;

public static class ImageBuilder
{
    private const string c_DefaultFontFamily = "Microsoft YaHei UI";
    private const float c_LineHeightMultiplier = 1.4f;
    private const int c_Padding = 10;

    private static readonly SKColor s_defaultColor = SKColors.Black;

    #region BuildImage

    /// <summary>
    /// 将字符串渲染为指定尺寸的图片，使用默认字体与黑色。
    /// </summary>
    public static ImageData BuildImage(string text, int width, int height, int fontSize, FontStyle fontStyle)
        => BuildImageCore(text, width, height, fontSize, fontStyle, c_DefaultFontFamily, s_defaultColor);

    /// <summary>
    /// 将字符串渲染为指定尺寸的图片，指定字体族，使用默认黑色。
    /// </summary>
    public static ImageData BuildImage(string text, int width, int height, int fontSize, FontStyle fontStyle, string fontFamily)
        => BuildImageCore(text, width, height, fontSize, fontStyle, fontFamily, s_defaultColor);

    /// <summary>
    /// 将字符串渲染为指定尺寸的图片，指定字体颜色，使用默认字体。
    /// </summary>
    public static ImageData BuildImage(string text, int width, int height, int fontSize, FontStyle fontStyle, SKColor color)
    {
        return BuildImageCore(text, width, height, fontSize, fontStyle, c_DefaultFontFamily, color);
    }

    /// <summary>
    /// 将字符串渲染为指定尺寸的图片，指定字体族与字体颜色。
    /// </summary>
    public static ImageData BuildImage(string text, int width, int height, int fontSize, FontStyle fontStyle, string fontFamily, SKColor color)
        => BuildImageCore(text, width, height, fontSize, fontStyle, fontFamily, color);

    #endregion BuildImage

    #region ExportImage

    /// <summary>
    /// 将 ImageData 按路径后缀决定格式保存到本地文件。
    /// 支持 .png / .jpg / .jpeg / .webp / .bmp，其余后缀均输出 PNG。
    /// </summary>
    public static void ExportImage(ImageData imageData, string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var format = GetFormatFromExtension(Path.GetExtension(path));

        using var image = SKImage.FromBitmap(imageData.Bitmap);
        using var data = image.Encode(format, 100)
            ?? throw new NotSupportedException($"当前环境不支持将图片编码为格式 '{format}'。");
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }

    #endregion ExportImage

    #region MeasureImageSize

    /// <summary>
    /// 根据文本内容与渲染参数，计算恰好容纳该文本的最适合图片尺寸。
    /// 以显式换行符（\n）作为分段依据，不对每行文本做自动折叠，
    /// 宽度取各行中最长的自然宽度，高度按行数与行间距累加。
    /// </summary>
    /// <param name="text">待渲染的文本。</param>
    /// <param name="fontSize">字体大小（像素）。</param>
    /// <param name="fontStyle">字体样式。</param>
    /// <param name="margin">四周外边距（像素），最终尺寸在各方向各扩展此值。</param>
    /// <returns>最适合的图片宽高（均至少为 1）。</returns>
    public static (int Width, int Height) MeasureImageSize(string text, int fontSize, FontStyle fontStyle, int margin)
    {
        using var typeface = SKTypeface.FromFamilyName(c_DefaultFontFamily, ToSKFontStyle(fontStyle))
                             ?? SKTypeface.Default;

        using var paint = new SKPaint
        {
            IsAntialias = true,
            TextSize = fontSize,
            Typeface = typeface,
        };

        var lines = text.Split('\n');

        var maxLineWidth = 0f;
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            var w = paint.MeasureText(line);
            if (w > maxLineWidth)
            {
                maxLineWidth = w;
            }
        }

        // 高度 = 首行文字高度（fontSize）+ 后续每行的行间距
        var lineHeight = fontSize * c_LineHeightMultiplier;
        var textHeight = fontSize + (lines.Length - 1) * lineHeight;

        var width = (int)Math.Ceiling(maxLineWidth) + margin * 2;
        var height = (int)Math.Ceiling(textHeight) + margin * 2;

        return (Math.Max(width, 1), Math.Max(height, 1));
    }

    #endregion MeasureImageSize

    #region Private

    private static ImageData BuildImageCore(string text, int width, int height, int fontSize, FontStyle fontStyle, string fontFamily, SKColor color)
    {
        var bitmap = new SKBitmap(width, height);

        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        using var typeface = SKTypeface.FromFamilyName(fontFamily, ToSKFontStyle(fontStyle))
                             ?? SKTypeface.Default;

        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            TextSize = fontSize,
            Typeface = typeface,
        };

        DrawWrappedText(canvas, paint, text, width, height, fontSize);

        return new ImageData(bitmap);
    }

    private static void DrawWrappedText(SKCanvas canvas, SKPaint paint, string text, int imageWidth, int imageHeight, int fontSize)
    {
        var lineHeight = fontSize * c_LineHeightMultiplier;
        var maxWidth = imageWidth - c_Padding * 2;
        var lines = WrapText(text, paint, maxWidth);

        // y 为基线位置：首行基线距顶部 padding + fontSize
        var y = (float)(c_Padding + fontSize);

        foreach (var line in lines)
        {
            if (y + c_Padding > imageHeight)
            {
                break;
            }

            canvas.DrawText(line, c_Padding, y, paint);
            y += lineHeight;
        }
    }

    private static List<string> WrapText(string text, SKPaint paint, float maxWidth)
    {
        var lines = new List<string>();

        foreach (var paragraph in text.Split('\n'))
        {
            if (string.IsNullOrEmpty(paragraph))
            {
                lines.Add(string.Empty);
                continue;
            }

            WrapParagraph(paragraph, paint, maxWidth, lines);
        }

        return lines;
    }

    /// <summary>
    /// 逐字符测量并换行，兼容中文等非空格分词语言。
    /// </summary>
    private static void WrapParagraph(string paragraph, SKPaint paint, float maxWidth, List<string> result)
    {
        var current = new StringBuilder();

        foreach (var ch in paragraph)
        {
            current.Append(ch);

            if (paint.MeasureText(current.ToString()) <= maxWidth)
            {
                continue;
            }

            if (current.Length > 1)
            {
                // 当前字符放到下一行
                current.Remove(current.Length - 1, 1);
                result.Add(current.ToString());
                current.Clear();
                current.Append(ch);
            }
            else
            {
                // 单个字符已超宽，强制输出
                result.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }
    }

    private static SKFontStyle ToSKFontStyle(FontStyle style)
    {
        return style switch
        {
            FontStyle.Bold => SKFontStyle.Bold,
            FontStyle.Italic => SKFontStyle.Italic,
            FontStyle.BoldItalic => SKFontStyle.BoldItalic,
            _ => SKFontStyle.Normal,
        };
    }

    private static SKEncodedImageFormat GetFormatFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
            ".webp" => SKEncodedImageFormat.Webp,
            ".bmp" => SKEncodedImageFormat.Bmp,
            _ => SKEncodedImageFormat.Png,
        };
    }

    #endregion Private
}