using System;
using System.IO;

namespace BotMain.Message;

/// <summary>消息及消息数据构建工具</summary>
public static class MessageBuilder
{
    /// <summary>从文件路径读取图片并返回 base64:// 开头的字符串</summary>
    public static string BuildImageFile(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);
        return "base64://" + Convert.ToBase64String(bytes);
    }

    /// <summary>从文件流读取图片并返回 base64:// 开头的字符串</summary>
    public static string BuildImageFile(FileStream fs)
    {
        if (fs.CanSeek) fs.Seek(0, SeekOrigin.Begin);
        using var ms = new MemoryStream();
        fs.CopyTo(ms);
        return "base64://" + Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>从字节数组构建 base64:// 开头的字符串</summary>
    public static string BuildImageFile(byte[] bytes)
    {
        return "base64://" + Convert.ToBase64String(bytes);
    }
}
