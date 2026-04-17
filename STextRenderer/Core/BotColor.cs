namespace STextRenderer.Core;

public readonly struct BotColor
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    public BotColor(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public static BotColor Black => new(0, 0, 0);
    public static BotColor White => new(255, 255, 255);
    public static BotColor Red => new(255, 0, 0);
    public static BotColor Green => new(0, 255, 0);
    public static BotColor Blue => new(0, 0, 255);
}
