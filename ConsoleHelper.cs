public static class ConsoleHelper
{  
    public const string ResetColor = "\x1b[0m";
    public const string Black = "\x1b[30m";
    public const string Red = "\x1b[31m";
    public const string Green = "\x1b[32m";
    public const string Yellow = "\x1b[33m";
    public const string Blue = "\x1b[34m";
    public const string Magenta = "\x1b[35m";
    public const string Cyan = "\x1b[36m";
    public const string White = "\x1b[37m";

    public static List<string> colors = ["\x1b[0m", "\x1b[30m", "\x1b[31m", "\x1b[32m", "\x1b[33",
    "\x1b[34m", "\x1b[35m", "\x1b[36m", "\x1b[37m"];

    public static void WriteLineWithColor(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    public static void WriteWithColor(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ResetColor();
    }   
}